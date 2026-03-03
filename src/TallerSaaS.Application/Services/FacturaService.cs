using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Application.Services;

/// <summary>
/// Gestiona la generación de Facturas con bloqueo de órdenes y descuento definitivo de stock.
/// </summary>
public class FacturaService
{
    private readonly IApplicationDbContext _db;
    private readonly TrazabilidadService _trazabilidad;
    private readonly InventarioService _inventario;

    public FacturaService(
        IApplicationDbContext db,
        TrazabilidadService trazabilidad,
        InventarioService inventario)
    {
        _db           = db;
        _trazabilidad = trazabilidad;
        _inventario   = inventario;
    }

    // ── Generación de Factura ─────────────────────────────────────────────────
    public async Task<Factura> GenerarFacturaAsync(List<Guid> ordenIds, Guid tenantId)
    {
        if (!ordenIds.Any())
            throw new InvalidOperationException("Debe seleccionar al menos una orden para facturar.");

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
        var anio = DateTime.UtcNow.Year;
        var count = await _db.Facturas.CountAsync(f => f.TenantId == tenantId) + 1;
        var numeroFactura = $"A-{anio}-{count:D4}";

        // 4. Crear la Factura
        var factura = new Factura
        {
            TenantId      = tenantId,
            NumeroFactura = numeroFactura,
            FechaEmision  = DateTime.UtcNow,
            CodigoQR      = $"TALLERSAAS|{numeroFactura}|{tenantId}|{DateTime.UtcNow:yyyyMMddHHmm}",
            Observaciones = null
        };

        // 5. Calcular totales consolidados y vincular órdenes
        foreach (var orden in ordenes)
        {
            factura.Subtotal  += orden.Subtotal;
            factura.Descuento += orden.Descuento;
            factura.IVA       += orden.IVA;
            factura.Total     += orden.Total;
            factura.Ordenes.Add(orden);

            // 6. Bloquear la orden (Seguridad)
            orden.Estado    = EstadoOrden.Facturada;
            orden.Bloqueada = true;
            orden.Pagada    = true;

            // 7. Descontar stock definitivo de cada ítem con producto de inventario
            await _inventario.DescontarStockPorOrdenAsync(orden);

            // 8. Registrar evento de trazabilidad
            await _trazabilidad.RegistrarEventoAsync(
                vehiculoId:  orden.VehiculoId,
                tipo:        TipoEvento.FacturaGenerada,
                descripcion: $"Factura #{numeroFactura} generada para Orden #{orden.NumeroOrden}",
                referenciaId: factura.Id,
                tenantId:    tenantId);
        }

        _db.Facturas.Add(factura);
        await _db.SaveChangesAsync();
        return factura;
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
        NumeroFactura      = f.NumeroFactura,
        FechaEmision       = f.FechaEmision,
        Subtotal           = f.Subtotal,
        Descuento          = f.Descuento,
        IVA                = f.IVA,
        Total              = f.Total,
        CodigoQR           = f.CodigoQR,
        FirmadaDigitalmente = f.FirmadaDigitalmente,
        Observaciones      = f.Observaciones,
        Ordenes            = f.Ordenes.Select(o => new OrdenDto
        {
            Id                   = o.Id,
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
