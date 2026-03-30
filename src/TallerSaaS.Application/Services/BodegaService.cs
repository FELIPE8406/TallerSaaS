using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Extensions;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Application.Services;

/// <summary>
/// CRUD y operaciones de traslado para bodegas/almacenes multi-tenant.
/// </summary>
public class BodegaService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;

    public BodegaService(IApplicationDbContext db, ICurrentTenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
    }

    // ── Consultas ──────────────────────────────────────────────────────────────
    public async Task<List<BodegaDto>> GetAllAsync()
    {
        return await _db.Bodegas
            .Where(b => b.Activo)
            .Select(b => new BodegaDto
            {
                Id             = b.Id,
                TenantId       = b.TenantId,
                Nombre         = b.Nombre,
                Descripcion    = b.Descripcion,
                Ubicacion      = b.Ubicacion,
                Activo         = b.Activo,
                TotalProductos = b.Productos.Count
            })
            .OrderBy(b => b.Nombre)
            .ToListAsync();
    }

    public async Task<BodegaDto?> GetByIdAsync(Guid id)
    {
        var b = await _db.Bodegas.Include(x => x.Productos).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return null;
        return new BodegaDto
        {
            Id             = b.Id,
            TenantId       = b.TenantId,
            Nombre         = b.Nombre,
            Descripcion    = b.Descripcion,
            Ubicacion      = b.Ubicacion,
            Activo         = b.Activo,
            TotalProductos = b.Productos.Count
        };
    }

    public async Task<PagedResult<MovimientoInventarioDto>> GetMovimientosPagedAsync(int pageNumber, int pageSize, Guid? bodegaId = null)
    {
        var query = _db.MovimientosInventario
            .Include(m => m.Producto)
            .Include(m => m.BodegaOrigen)
            .Include(m => m.BodegaDestino)
            .AsQueryable();

        if (bodegaId.HasValue)
            query = query.Where(m => m.BodegaOrigenId == bodegaId || m.BodegaDestinoId == bodegaId);

        var orderedQuery = query.OrderByDescending(m => m.Fecha);
        var paged = await orderedQuery.ToPagedListAsync(pageNumber, pageSize);

        return new PagedResult<MovimientoInventarioDto>
        {
            Data = paged.Data.Select(m => new MovimientoInventarioDto
            {
                Id             = m.Id,
                ProductoNombre = m.Producto != null ? m.Producto.Nombre : "—",
                BodegaOrigen   = m.BodegaOrigen != null ? m.BodegaOrigen.Nombre : null,
                BodegaDestino  = m.BodegaDestino != null ? m.BodegaDestino.Nombre : null,
                Tipo           = m.Tipo,
                Cantidad       = m.Cantidad,
                Referencia     = m.Referencia,
                Observaciones  = m.Observaciones,
                Fecha          = m.Fecha
            }).ToList(),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize   = paged.PageSize
        };
    }

    // ── Crear / Editar Bodega ──────────────────────────────────────────────────
    public async Task<Bodega> CreateAsync(BodegaDto dto, Guid tenantId)
    {
        var bodega = new Bodega
        {
            TenantId    = tenantId,
            Nombre      = dto.Nombre,
            Descripcion = dto.Descripcion,
            Ubicacion   = dto.Ubicacion,
            Activo      = true
        };
        _db.Bodegas.Add(bodega);
        await _db.SaveChangesAsync();
        return bodega;
    }

    public async Task UpdateAsync(BodegaDto dto)
    {
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var b = await _db.Bodegas
            .FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception("Bodega no encontrada");
        b.Nombre      = dto.Nombre;
        b.Descripcion = dto.Descripcion;
        b.Ubicacion   = dto.Ubicacion;
        b.Activo      = dto.Activo;
        await _db.SaveChangesAsync();
    }

    // ── Traslado entre Bodegas ─────────────────────────────────────────────────
    /// <summary>
    /// Traslada unidades de un producto de una bodega origen a una bodega destino.
    /// Registra dos movimientos: Salida de origen y Entrada a destino.
    /// </summary>
    public async Task TrasladarAsync(Guid productoId, Guid bodegaOrigenId, Guid bodegaDestinoId,
        int cantidad, Guid tenantId, string? observaciones = null)
    {
        var producto = await _db.Inventario
            .FirstOrDefaultAsync(x => x.Id == productoId && x.TenantId == tenantId)
            ?? throw new Exception("Producto no encontrado.");

        if (producto.Stock < cantidad)
            throw new InvalidOperationException(
                $"Stock insuficiente en bodega origen. Disponible: {producto.Stock}, Solicitado: {cantidad}.");

        // Actualizar stock (el producto tiene asignación de bodega principal)
        producto.Stock -= cantidad;
        producto.FechaActualizacion = DateTime.UtcNow;

        // Mover asignación de bodega si el traslado es total
        if (producto.BodegaId == bodegaOrigenId)
            producto.BodegaId = bodegaDestinoId;

        // Registrar movimiento único de Traslado
        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            TenantId        = tenantId,
            ProductoId      = productoId,
            BodegaOrigenId  = bodegaOrigenId,
            BodegaDestinoId = bodegaDestinoId,
            Tipo            = TipoMovimiento.Traslado,
            Cantidad        = cantidad,
            Observaciones   = observaciones
        });

        await _db.SaveChangesAsync();
    }

    // ── Consulta de stock para el panel dinámico de Traslado ──────────────────
    /// <summary>
    /// Devuelve información compacta de stock para el panel de resumen dinámico del formulario de Traslado.
    /// </summary>
    public async Task<object?> GetProductoStockInfoAsync(Guid productoId)
    {
        var p = await _db.Inventario
            .Include(x => x.Bodega)
            .FirstOrDefaultAsync(x => x.Id == productoId && x.Activo);

        if (p == null) return null;

        return new
        {
            id           = p.Id,
            nombre       = p.Nombre,
            sku          = p.SKU,
            stock        = p.Stock,
            stockMinimo  = p.StockMinimo,
            bodegaNombre = p.Bodega != null ? p.Bodega.Nombre : "Sin bodega asignada"
        };
    }
}
