using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Application.Services;

public class OrdenService
{
    private readonly IApplicationDbContext _db;
    private readonly TrazabilidadService _trazabilidad;

    public OrdenService(IApplicationDbContext db, TrazabilidadService trazabilidad)
    {
        _db = db;
        _trazabilidad = trazabilidad;
    }

    private static OrdenDto MapToDto(Orden o) => new()
    {
        Id = o.Id, NumeroOrden = o.NumeroOrden,
        VehiculoId = o.VehiculoId,
        VehiculoDescripcion = o.Vehiculo != null ? $"{o.Vehiculo.Anio} {o.Vehiculo.Marca} {o.Vehiculo.Modelo}" : "",
        ClienteNombre = o.Vehiculo?.Cliente?.NombreCompleto ?? "",
        ClienteTelefono = o.Vehiculo?.Cliente?.Telefono ?? "",
        Estado = (int)o.Estado, EstadoTexto = o.EstadoTexto, EstadoClase = o.EstadoClase,
        FechaEntrada = o.FechaEntrada, FechaSalida = o.FechaSalida,
        DiagnosticoInicial = o.DiagnosticoInicial, TrabajoRealizado = o.TrabajoRealizado,
        Observaciones = o.Observaciones, Subtotal = o.Subtotal, Descuento = o.Descuento,
        IVA = o.IVA, Total = o.Total, Pagada = o.Pagada,
        Items = o.Items.Select(i => new ItemOrdenDto
        {
            Id = i.Id, Descripcion = i.Descripcion, Tipo = i.Tipo,
            Cantidad = i.Cantidad, PrecioUnitario = i.PrecioUnitario,
            ProductoInventarioId = i.ProductoInventarioId
        }).ToList(),
        Bloqueada = o.Bloqueada,
        FacturaId = o.FacturaId
    };

    public async Task<List<OrdenDto>> GetAllAsync(EstadoOrden? estado = null)
    {
        var query = _db.Ordenes
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Include(o => o.Items)
            .AsQueryable();

        if (estado.HasValue)
            query = query.Where(o => o.Estado == estado.Value);

        var ordenes = await query.OrderByDescending(o => o.FechaEntrada).ToListAsync();
        return ordenes.Select(MapToDto).ToList();
    }

    public async Task<OrdenDto?> GetByIdAsync(Guid id)
    {
        var orden = await _db.Ordenes
            .IgnoreQueryFilters()   // bypass TenantId filter — auth already validated by controller
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Include(o => o.Items).ThenInclude(i => i.ProductoInventario)
            .FirstOrDefaultAsync(o => o.Id == id);
        return orden == null ? null : MapToDto(orden);
    }

    public async Task<Orden> CreateAsync(OrdenDto dto, Guid tenantId)
    {
        var count = await _db.Ordenes.CountAsync();
        var orden = new Orden
        {
            TenantId = tenantId, VehiculoId = dto.VehiculoId,
            NumeroOrden = $"ORD-{DateTime.Now:yyyyMM}-{count + 1:D4}",
            DiagnosticoInicial = dto.DiagnosticoInicial, Observaciones = dto.Observaciones,
            Estado = EstadoOrden.Recibido
        };
        _db.Ordenes.Add(orden);
        await _db.SaveChangesAsync();

        // Registrar evento de trazabilidad
        var vehiculo = await _db.Vehiculos.FindAsync(dto.VehiculoId);
        if (vehiculo != null)
            await _trazabilidad.RegistrarEventoAsync(
                dto.VehiculoId, TipoEvento.OrdenCreada,
                $"Orden #{orden.NumeroOrden} creada",
                orden.Id, tenantId);

        return orden;
    }

    public async Task CambiarEstadoAsync(Guid id, EstadoOrden nuevoEstado)
    {
        var orden = await _db.Ordenes.FindAsync(id) ?? throw new Exception("Orden no encontrada");
        orden.Estado = nuevoEstado;
        if (nuevoEstado == EstadoOrden.Entregado)
            orden.FechaSalida = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task AddItemAsync(Guid ordenId, ItemOrdenDto dto)
    {
        // ── Phase 0: validate order exists and is not locked ─────────────────
        var ordenCheck = await _db.Ordenes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ordenId)
            ?? throw new Exception($"Orden {ordenId} no encontrada.");

        if (ordenCheck.Bloqueada)
            throw new InvalidOperationException(
                $"La orden {ordenCheck.NumeroOrden} está facturada y bloqueada. No se pueden agregar ítems.");

        // ── Phase 1: INSERT the item only (simple operation, no concurrency risk) ──
        var item = new ItemOrden
        {
            OrdenId        = ordenId,
            Descripcion    = dto.Descripcion,
            Tipo           = dto.Tipo,
            Cantidad       = dto.Cantidad,
            PrecioUnitario = dto.PrecioUnitario,
            ProductoInventarioId = dto.ProductoInventarioId
        };
        _db.ItemsOrden.Add(item);
        await _db.SaveChangesAsync();

        // ── Phase 2: reload Orden fresh, recalculate totals, save ─────────────────
        // Load with tracking (no AsNoTracking) so EF can UPDATE scalar columns.
        var orden = await _db.Ordenes
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == ordenId)
            ?? throw new Exception("Orden no encontrada al recalcular totales.");

        RecalcularTotales(orden);   // Subtotal, IVA 19% Colombia, Total

        // Explicitly mark ALL scalar properties as Modified to ensure EF generates
        // the full UPDATE even if it doesn't detect property-level changes.
        _db.Entry(orden).State = EntityState.Modified;
        // Navigation properties must NOT be Modified — only scalars
        _db.Entry(orden).Reference(o => o.Vehiculo).IsModified = false;
        _db.Entry(orden).Reference(o => o.Factura).IsModified   = false;
        _db.Entry(orden).Collection(o => o.Items).IsModified     = false;

        await _db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(Guid ordenId, Guid itemId)
    {
        var orden = await _db.Ordenes
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == ordenId)
            ?? throw new Exception("Orden no encontrada");

        if (orden.Bloqueada)
            throw new InvalidOperationException(
                $"La orden {orden.NumeroOrden} está facturada y bloqueada. No se pueden eliminar ítems.");

        var item = orden.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new Exception("Ítem no encontrado");

        orden.Items.Remove(item);
        RecalcularTotales(orden);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await _db.Entry(orden).ReloadAsync();
            RecalcularTotales(orden);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>Crea una orden adicional para el mismo vehículo tras haber sido facturado. </summary>
    public async Task<Orden> CrearOrdenAdicionalAsync(OrdenDto dto, Guid tenantId)
    {
        var count = await _db.Ordenes.CountAsync();
        var orden = new Orden
        {
            TenantId = tenantId, VehiculoId = dto.VehiculoId,
            NumeroOrden = $"ORD-{DateTime.Now:yyyyMM}-{count + 1:D4}",
            DiagnosticoInicial = dto.DiagnosticoInicial, Observaciones = dto.Observaciones,
            Estado = EstadoOrden.Recibido
        };
        _db.Ordenes.Add(orden);
        await _db.SaveChangesAsync();

        await _trazabilidad.RegistrarEventoAsync(
            dto.VehiculoId, TipoEvento.OrdenAdicionalCreada,
            $"Orden Adicional #{orden.NumeroOrden} creada",
            orden.Id, tenantId);

        return orden;
    }

    private static void RecalcularTotales(Orden orden)
    {
        orden.Subtotal = orden.Items.Sum(i => i.Cantidad * i.PrecioUnitario);
        var subtotalConDescuento = orden.Subtotal - orden.Descuento;
        orden.IVA = Math.Round(subtotalConDescuento * 0.19m, 2);  // IVA Colombia = 19%
        orden.Total = Math.Round(subtotalConDescuento + orden.IVA, 2);
    }
}
