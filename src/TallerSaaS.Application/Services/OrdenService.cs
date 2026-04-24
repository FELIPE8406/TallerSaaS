using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Extensions;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Application.Services;

public class OrdenService
{
    private readonly IApplicationDbContext _db;
    private readonly TrazabilidadService _trazabilidad;
    private readonly ICurrentTenantService _tenantService;

    public OrdenService(IApplicationDbContext db, TrazabilidadService trazabilidad, ICurrentTenantService tenantService)
    {
        _db = db;
        _trazabilidad = trazabilidad;
        _tenantService = tenantService;
    }

    private static OrdenDto MapToDto(Orden o) => new()
    {
        Id = o.Id, TenantId = o.TenantId, NumeroOrden = o.NumeroOrden,
        VehiculoId = o.VehiculoId,
        VehiculoDescripcion = o.Vehiculo != null ? $"{o.Vehiculo.Anio} {o.Vehiculo.Marca} {o.Vehiculo.Modelo}" : "",
        ClienteNombre = o.Vehiculo?.Cliente?.NombreCompleto ?? "",
        ClienteTelefono = o.Vehiculo?.Cliente?.Telefono ?? "",
        Estado = (int)o.Estado, EstadoTexto = o.EstadoTexto, EstadoClase = o.EstadoClase,
        FechaEntrada = o.FechaEntrada, FechaSalida = o.FechaSalida,
        DiagnosticoInicial = o.DiagnosticoInicial, TrabajoRealizado = o.TrabajoRealizado,
        Observaciones = o.Observaciones, Subtotal = o.Subtotal, Descuento = o.Descuento,
        IVA = o.IVA, Total = o.Total, Pagada = o.Pagada,
        AplicarRetencion = o.AplicarRetencion,
        PorcentajeRetencion = o.PorcentajeRetencion,
        MontoRetencion = o.MontoRetencion,
        Items = o.Items.Select(i => new ItemOrdenDto
        {
            Id = i.Id, Descripcion = i.Descripcion, Tipo = i.Tipo,
            Cantidad = i.Cantidad, PrecioUnitario = i.PrecioUnitario,
            ProductoInventarioId = i.ProductoInventarioId
        }).ToList(),
        Bloqueada = o.Bloqueada,
        FacturaId = o.FacturaId,
        AppointmentId = o.AppointmentId
    };

    public async Task<PagedResult<OrdenDto>> GetAllPagedAsync(int pageNumber, int pageSize, EstadoOrden? estado = null)
    {
        var query = _db.Ordenes.AsNoTracking()
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            // .Include(o => o.Items) // REMOVED: redundant for list view, items aren't shown in grid
            .AsQueryable();

        if (estado.HasValue)
            query = query.Where(o => o.Estado == estado.Value);

        var orderedQuery = query.OrderByDescending(o => o.FechaEntrada);
        var paged = await orderedQuery.ToPagedListAsync(pageNumber, pageSize);

        return new PagedResult<OrdenDto>
        {
            Data = paged.Data.Select(MapToDto).ToList(),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
    }

    /// <summary>Non-paginated overload kept for internal use (e.g. FacturasController).</summary>
    public async Task<List<OrdenDto>> GetAllAsync(EstadoOrden? estado = null)
    {
        var query = _db.Ordenes.AsNoTracking()
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .AsQueryable();

        if (estado.HasValue)
            query = query.Where(o => o.Estado == estado.Value);

        var ordenes = await query.OrderByDescending(o => o.FechaEntrada).Take(100).ToListAsync();
        return ordenes.Select(MapToDto).ToList();
    }

    public async Task<OrdenDto?> GetByIdAsync(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return null;
        var orden = await _db.Ordenes
            .AsNoTracking()
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Include(o => o.Items).ThenInclude(i => i.ProductoInventario)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _tenantService.TenantId.Value);
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
            // ⚠️ BUG FIX: persistir el descuento ingresado en el formulario
            Descuento = dto.Descuento,
            Estado = dto.Estado is 1 or 2 ? (EstadoOrden)dto.Estado : EstadoOrden.Recibido
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
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var orden = await _db.Ordenes
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception("Orden no encontrada");

        // Protección de datos: una orden bloqueada (facturada) no puede mutar vía workflow.
        if (orden.Bloqueada)
            throw new InvalidOperationException("ORDEN_YA_BLOQUEADA");

        if (!EsTransicionValida(orden.Estado, nuevoEstado))
            throw new InvalidOperationException($"TRANSICION_INVALIDA: {orden.Estado} -> {nuevoEstado}");

        // Entregado requiere que la orden esté pagada y vinculada a una factura.
        if (nuevoEstado == EstadoOrden.Entregado)
        {
            if (!orden.Pagada || !orden.FacturaId.HasValue)
                throw new InvalidOperationException("REQUIERE_FACTURA");
        }

        orden.Estado = nuevoEstado;

        if (nuevoEstado == EstadoOrden.Entregado || nuevoEstado == EstadoOrden.EntregadoYFacturado)
            orden.FechaSalida = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private static bool EsTransicionValida(EstadoOrden actual, EstadoOrden nuevo)
    {
        return actual switch
        {
            EstadoOrden.Recibido            => nuevo == EstadoOrden.EnReparacion,
            EstadoOrden.EnReparacion        => nuevo == EstadoOrden.Terminado,
            EstadoOrden.Terminado          => nuevo == EstadoOrden.Entregado,
            EstadoOrden.Entregado          => nuevo == EstadoOrden.EntregadoYFacturado,
            EstadoOrden.Facturada          => nuevo == EstadoOrden.EntregadoYFacturado,
            _                               => false
        };
    }

    public async Task AddItemAsync(Guid ordenId, ItemOrdenDto dto)
    {
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var ordenCheck = await _db.Ordenes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ordenId && o.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception($"Orden {ordenId} no encontrada.");

        if (ordenCheck.Bloqueada)
            throw new InvalidOperationException(
                $"La orden {ordenCheck.NumeroOrden} está facturada y bloqueada. No se pueden agregar ítems.");

        // ── Phase 1: Validar inventario si es Refacción ───────────────────────────
        // Services are non-inventoriable: enforce quantity = 1
        if (dto.Tipo.Equals("Servicio", StringComparison.OrdinalIgnoreCase))
        {
            dto.Cantidad = 1;

            // ── Regla anti-duplicado: no se puede agregar el mismo servicio dos veces ──
            var itemsActuales = await _db.ItemsOrden
                .AsNoTracking()
                .Where(i => i.OrdenId == ordenId &&
                            i.Tipo == "Servicio" &&
                            i.Descripcion.ToLower() == dto.Descripcion.ToLower())
                .AnyAsync();

            if (itemsActuales)
                throw new InvalidOperationException(
                    $"El servicio '{dto.Descripcion}' ya existe en esta orden. " +
                    "No se permite duplicar un mismo servicio en la misma orden activa.");
        }

        // Todas las variantes del tipo Refacción deben estar vinculadas a inventario
        bool esRefaccion = dto.Tipo.Equals("Refaccion",  StringComparison.OrdinalIgnoreCase)
                        || dto.Tipo.Equals("Refacción", StringComparison.OrdinalIgnoreCase);

        if (esRefaccion)
        {
            // ⚠️ Si no tiene ProductoInventarioId el cliente no seleccionó un producto: bloquear.
            if (!dto.ProductoInventarioId.HasValue)
                throw new InvalidOperationException(
                    "Las refacciones deben estar vinculadas a un producto del inventario. " +
                    "Seleccione el producto correspondiente o agréguelo primero al módulo de Inventario.");

            var producto = await _db.Inventario.FindAsync(dto.ProductoInventarioId.Value)
                ?? throw new InvalidOperationException(
                    "El producto seleccionado no existe en el inventario. " +
                    "Verifíquelo en el módulo de Inventario antes de continuar.");

            var cantidadNecesaria = (int)Math.Ceiling(dto.Cantidad);
            if (producto.Stock < cantidadNecesaria)
                throw new InvalidOperationException(
                    $"Stock insuficiente para '{producto.Nombre}'. " +
                    $"Disponible: {producto.Stock} unidad(es). Requerido: {cantidadNecesaria}. " +
                    $"Realice un Ajuste de Entrada desde el módulo de Inventario antes de continuar.");

            // Descuento preventivo de stock al agregar el ítem
            producto.Stock -= cantidadNecesaria;
            producto.FechaActualizacion = DateTime.UtcNow;

            _db.MovimientosInventario.Add(new Domain.Entities.MovimientoInventario
            {
                TenantId   = ordenCheck.TenantId,
                ProductoId = producto.Id,
                Tipo       = Domain.Entities.TipoMovimiento.Salida,
                Cantidad   = cantidadNecesaria,
                Referencia = ordenCheck.NumeroOrden,
                Observaciones = $"Salida preventiva al agregar ítem a orden #{ordenCheck.NumeroOrden}"
            });
        }

        // ── Phase 2: INSERT the item only (simple operation, no concurrency risk) ──
        var item = new ItemOrden
        {
            TenantId       = ordenCheck.TenantId, 
            OrdenId        = ordenId,
            Descripcion    = dto.Descripcion,
            Tipo           = dto.Tipo,
            Cantidad       = dto.Cantidad,
            PrecioUnitario = dto.PrecioUnitario,
            ProductoInventarioId = dto.ProductoInventarioId
        };
        _db.ItemsOrden.Add(item);
        await _db.SaveChangesAsync();

        var orden = await _db.Ordenes
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == ordenId && o.TenantId == _tenantService.TenantId!.Value)
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
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var orden = await _db.Ordenes
            .Include(o => o.Items).ThenInclude(i => i.ProductoInventario)
            .FirstOrDefaultAsync(o => o.Id == ordenId && o.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception("Orden no encontrada");

        if (orden.Bloqueada)
            throw new InvalidOperationException(
                $"La orden {orden.NumeroOrden} está facturada y bloqueada. No se pueden eliminar ítems.");

        var item = orden.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new Exception("Ítem no encontrado");

        // ── Restock automático (sincronización bidireccional) ──────────────────
        // Si el ítem es una refacción vinculada al inventario, devolver el stock
        // que fue descontado preventivamente en AddItemAsync.
        bool esRefaccion = item.Tipo.Equals("Refaccion",  StringComparison.OrdinalIgnoreCase)
                        || item.Tipo.Equals("Refacción", StringComparison.OrdinalIgnoreCase);

        if (esRefaccion && item.ProductoInventarioId.HasValue)
        {
            var producto = await _db.Inventario.FindAsync(item.ProductoInventarioId.Value);
            if (producto != null)
            {
                var cantidadDevolver = (int)Math.Ceiling(item.Cantidad);
                producto.Stock += cantidadDevolver;
                producto.FechaActualizacion = DateTime.UtcNow;

                _db.MovimientosInventario.Add(new Domain.Entities.MovimientoInventario
                {
                    TenantId      = orden.TenantId,
                    ProductoId    = producto.Id,
                    Tipo          = Domain.Entities.TipoMovimiento.AjusteEntrada,
                    Cantidad      = cantidadDevolver,
                    Referencia    = orden.NumeroOrden,
                    Observaciones = $"Restock automático: ítem eliminado de orden #{orden.NumeroOrden}"
                });
            }
        }

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

    public async Task UpdateRetentionAsync(Guid ordenId, bool aplicar, decimal porcentaje)
    {
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var orden = await _db.Ordenes
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == ordenId && o.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception("Orden no encontrada");

        if (orden.Bloqueada)
            throw new InvalidOperationException("No se puede modificar una orden bloqueada.");

        orden.AplicarRetencion = aplicar;
        orden.PorcentajeRetencion = porcentaje;
        RecalcularTotales(orden);

        await _db.SaveChangesAsync();
    }

    private static void RecalcularTotales(Orden orden)
    {
        // 1. Subtotal base (Suma de todos los ítems)
        orden.Subtotal = orden.Items.Sum(i => i.Cantidad * i.PrecioUnitario);
        
        // 2. Base gravable (Subtotal - Descuento)
        var baseGravable = orden.Subtotal - orden.Descuento;
        
        // 3. IVA (19% en Colombia)
        orden.IVA = Math.Round(baseGravable * 0.19m, 2);
        
        // 4. Retención en la Fuente (SÓLO si está activa y se calcula sobre la base gravable)
        if (orden.AplicarRetencion)
        {
            orden.MontoRetencion = Math.Round(baseGravable * (orden.PorcentajeRetencion / 100m), 2);
        }
        else
        {
            orden.MontoRetencion = 0;
        }

        // 5. Total Final = Base + IVA - Retención
        orden.Total = Math.Round(baseGravable + orden.IVA - orden.MontoRetencion, 2);
    }
}
