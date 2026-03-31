using Microsoft.EntityFrameworkCore;
using System.Data;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Extensions;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Application.Services;

/// <summary>
/// Gestiona la generación de Facturas con bloqueo de órdenes y descuento definitivo de stock.
/// </summary>
public class FacturaService
{    private readonly IApplicationDbContext _db;
    private readonly TrazabilidadService _trazabilidad;
    private readonly InventarioService _inventario;
    private readonly IAccountingService _accounting;

    public FacturaService(
        IApplicationDbContext db,
        TrazabilidadService trazabilidad,
        InventarioService inventario,
        IAccountingService accounting)
    {
        _db           = db;
        _trazabilidad = trazabilidad;
        _inventario   = inventario;
        _accounting   = accounting;
    }

    // ── Generación de Factura ─────────────────────────────────────────────────
    /// <param name="tipoFacturacion">
    /// Tipo de documento a emitir: NoElectronica (cierra saldo inmediato) o
    /// Electronica (registra intención, queda como PendienteEnvio hasta integración DIAN).
    /// </param>
    public async Task<Factura> GenerarFacturaAsync(
        List<Guid> ordenIds,
        Guid tenantId,
        TipoFacturacion tipoFacturacion = TipoFacturacion.NoElectronica)
    {
        if (!ordenIds.Any())
            throw new InvalidOperationException("Debe seleccionar al menos una orden para facturar.");

        // Blindaje transaccional + aislamiento para evitar doble facturación concurrente.
        // Serializable reduce inconsistencias por lecturas concurrentes (especialmente al marcar Bloqueada).
        await using var tx = await _db.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // 1. Cargar todas las órdenes con sus ítems
            var ordenes = await _db.Ordenes
                .Include(o => o.Items).ThenInclude(i => i.ProductoInventario)
                .Include(o => o.Vehiculo)
                .Where(o => ordenIds.Contains(o.Id) && o.TenantId == tenantId)
                .ToListAsync();

            if (ordenes.Count != ordenIds.Count)
                throw new InvalidOperationException("Una o más órdenes no fueron encontradas para este tenant.");

            // 2. Validar que ninguna esté ya bloqueada/facturada
            var bloqueadas = ordenes.Where(o => o.Bloqueada).Select(o => o.NumeroOrden).ToList();
            if (bloqueadas.Any())
                throw new InvalidOperationException(
                    $"Las siguientes órdenes ya están facturadas: {string.Join(", ", bloqueadas)}");

            // 3. Generar número de factura consecutivo: A-YYYY-NNNN
            var anio  = DateTime.UtcNow.Year;
            var count = await _db.Facturas.CountAsync(f => f.TenantId == tenantId) + 1;
            var numeroFactura = $"A-{anio}-{count:D4}";

            // 4. Crear la Factura con tipo y estado de envío
            var factura = new Factura
            {
                TenantId        = tenantId,
                NumeroFactura   = numeroFactura,
                FechaEmision    = DateTime.UtcNow,
                Observaciones   = null,
                TipoFacturacion = tipoFacturacion,
                EstadoEnvio     = tipoFacturacion == TipoFacturacion.Electronica
                                  ? EstadoEnvioFactura.PendienteEnvio
                                  : EstadoEnvioFactura.NoAplica
            };

            // 5. Calcular totales consolidados y vincular órdenes
            foreach (var orden in ordenes)
            {
                factura.Subtotal  += orden.Subtotal;
                factura.Descuento += orden.Descuento;
                factura.Ordenes.Add(orden);

                // 6. Descontar (idempotente) antes de bloquear la orden
                await _inventario.DescontarStockPorOrdenAsync(orden);

                // 7. Bloquear la orden y cerrar el ciclo de cobro (ambos tipos de factura)
                orden.Bloqueada    = true;
                orden.Pagada       = true;                   // cierra el saldo en ambos flujos
                orden.FechaSalida  = DateTime.UtcNow;
                orden.Estado       = EstadoOrden.EntregadoYFacturado;

                // 8. Contabilidad: Costo de Ventas (Evento 2)
                await _accounting.RegistrarSalidaInventarioAsync(orden);

                // 9. Trazabilidad
                await _trazabilidad.RegistrarEventoAsync(
                    vehiculoId:   orden.VehiculoId,
                    tipo:         TipoEvento.FacturaGenerada,
                    descripcion:  $"Factura #{numeroFactura} generada para Orden #{orden.NumeroOrden}" +
                                  (tipoFacturacion == TipoFacturacion.Electronica
                                   ? " [Electrónica – Pendiente DIAN]" : ""),
                    referenciaId: factura.Id,
                    tenantId:     tenantId);
            }

            // 10. Recalcular IVA y Total (fórmula Colombia): (Subtotal − Descuento) × 1.19
            var subtotalNeto = factura.Subtotal - factura.Descuento;
            factura.IVA   = Math.Round(subtotalNeto * 0.19m, 2);
            factura.Total = Math.Round(subtotalNeto + factura.IVA, 2);

            _db.Facturas.Add(factura);

            // 11. Contabilidad: Factura (Evento 1)
            await _accounting.RegistrarFacturaAsync(factura);

            // 12. Contabilidad: Recaudo (Evento 3) - Asumimos recaudo inmediato en este flujo
            var pagoSimulado = new Pago
            {
                TenantId = tenantId,
                Monto = factura.Total,
                Fecha = DateTime.UtcNow,
                Referencia = factura.NumeroFactura
            };
            await _accounting.RegistrarPagoAsync(pagoSimulado, factura);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return factura;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Consultas ─────────────────────────────────────────────────────────────
    public async Task<List<FacturaDto>> GetAllAsync()
    {
        var facturas = await _db.Facturas
            .Include(f => f.Ordenes).ThenInclude(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Include(f => f.Ordenes).ThenInclude(o => o.Items)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();

        return facturas.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns a paginated page of facturas (newest first).
    /// Does NOT modify or replace existing methods.
    /// </summary>
    public async Task<PagedResult<FacturaDto>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var paged = await _db.Facturas.AsNoTracking()
            .OrderByDescending(f => f.FechaEmision)
            .Select(f => new
            {
                f.Id, f.NumeroFactura, f.FechaEmision, f.Subtotal, f.Descuento, f.IVA, f.Total, f.Observaciones,
                f.TipoFacturacion, f.EstadoEnvio,
                Ordenes = f.Ordenes.Select(o => new 
                {
                    o.Id, o.NumeroOrden, o.Total, 
                    VehiculoDesc = o.Vehiculo != null ? o.Vehiculo.Marca + " " + o.Vehiculo.Modelo : ""
                })
            })
            .ToPagedListAsync(pageNumber, pageSize);

        return new PagedResult<FacturaDto>
        {
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize   = paged.PageSize,
            Data       = paged.Data.Select(f => new FacturaDto
            {
                Id = f.Id, NumeroFactura = f.NumeroFactura, FechaEmision = f.FechaEmision,
                Subtotal = f.Subtotal, Descuento = f.Descuento, IVA = f.IVA, Total = f.Total,
                Observaciones = f.Observaciones,
                TipoFacturacion = f.TipoFacturacion.ToString(),
                EstadoEnvio = f.EstadoEnvio.ToString(),
                Ordenes = f.Ordenes.Select(o => new OrdenDto
                {
                    Id = o.Id, NumeroOrden = o.NumeroOrden, Total = o.Total, VehiculoDescripcion = o.VehiculoDesc
                }).ToList()
            }).ToList()
        };
    }

    public async Task<FacturaDto?> GetByIdAsync(Guid id)
    {
        var f = await _db.Facturas
            .Include(f => f.Ordenes).ThenInclude(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Include(f => f.Ordenes).ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(f => f.Id == id);
        return f == null ? null : MapToDto(f);
    }

    private static FacturaDto MapToDto(Factura f) => new()
    {
        Id                 = f.Id,
        TenantId           = f.TenantId,
        NumeroFactura      = f.NumeroFactura,
        FechaEmision       = f.FechaEmision,
        Subtotal           = f.Subtotal,
        Descuento          = f.Descuento,
        IVA                = f.IVA,
        Total              = f.Total,
        Observaciones      = f.Observaciones,
        TipoFacturacion    = f.TipoFacturacion.ToString(),
        EstadoEnvio        = f.EstadoEnvio.ToString(),
        Ordenes            = f.Ordenes.Select(o => new OrdenDto
        {
            Id                   = o.Id,
            TenantId             = o.TenantId,
            NumeroOrden          = o.NumeroOrden,
            VehiculoId           = o.VehiculoId,
            VehiculoDescripcion  = o.Vehiculo != null ? o.Vehiculo.Descripcion : "",
            ClienteNombre        = o.Vehiculo?.Cliente?.NombreCompleto ?? "",
            Estado               = (int)o.Estado,
            EstadoTexto          = o.EstadoTexto,
            EstadoClase          = o.EstadoClase,
            FechaEntrada         = o.FechaEntrada,
            Subtotal             = o.Subtotal,
            Descuento            = o.Descuento,
            IVA                  = o.IVA,
            Total                = o.Total,
            Pagada               = o.Pagada,
            Bloqueada            = o.Bloqueada,
            FacturaId            = o.FacturaId,
            Items                = o.Items.Select(i => new ItemOrdenDto
            {
                Id                   = i.Id,
                Descripcion          = i.Descripcion,
                Tipo                 = i.Tipo,
                Cantidad             = i.Cantidad,
                PrecioUnitario       = i.PrecioUnitario,
                ProductoInventarioId = i.ProductoInventarioId
            }).ToList()
        }).ToList()
    };
}
