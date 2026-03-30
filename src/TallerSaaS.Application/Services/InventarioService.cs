using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Extensions;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Application.Services;

public class InventarioService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;

    public InventarioService(IApplicationDbContext db, ICurrentTenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
    }

    public async Task<PagedResult<InventarioDto>> GetAllPagedAsync(int pageNumber, int pageSize, string? buscar = null, string? categoria = null)
    {
        var query = _db.Inventario
            .AsNoTracking()
            .Include(p => p.Bodega)
            .Where(p => p.Activo);

        if (!string.IsNullOrEmpty(buscar))
            query = query.Where(p => p.Nombre.Contains(buscar) || (p.SKU != null && p.SKU.Contains(buscar)));
        if (!string.IsNullOrEmpty(categoria))
            query = query.Where(p => p.Categoria == categoria);

        var paged = await query.OrderBy(p => p.Nombre)
            .Select(p => new
            {
                p.Id, p.Nombre, p.SKU, p.Descripcion, p.Categoria, p.Stock, p.StockMinimo,
                p.PrecioCompra, p.PrecioVenta, p.Proveedor, p.BodegaId,
                BodegaNombre = p.Bodega != null ? p.Bodega.Nombre : null,
                p.TipoItem
            })
            .ToPagedListAsync(pageNumber, pageSize);

        return new PagedResult<InventarioDto>
        {
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            Data = paged.Data.Select(p => new InventarioDto
            {
                Id = p.Id, TenantId = _tenantService.TenantId ?? Guid.Empty, Nombre = p.Nombre, SKU = p.SKU, Descripcion = p.Descripcion,
                Categoria = p.Categoria, Stock = p.Stock, StockMinimo = p.StockMinimo,
                PrecioCompra = p.PrecioCompra, PrecioVenta = p.PrecioVenta, Proveedor = p.Proveedor,
                BodegaId = p.BodegaId,
                BodegaNombre = p.BodegaNombre,
                TipoItem = p.TipoItem == TipoItemProducto.Servicio ? "Servicio" : "Refaccion",
                NivelStock = p.Stock <= 0 ? "Agotado" : p.Stock <= p.StockMinimo ? "Bajo" : "OK",
                NivelStockClase = p.Stock <= 0 ? "danger" : p.Stock <= p.StockMinimo ? "warning" : "success"
            }).ToList()
        };
    }

    public async Task<List<InventarioDto>> GetBajoStockAsync() =>
        await _db.Inventario.Where(p => p.Stock <= p.StockMinimo && p.Activo)
            .Select(p => new InventarioDto
            {
                Id = p.Id, Nombre = p.Nombre, SKU = p.SKU, Stock = p.Stock, StockMinimo = p.StockMinimo,
                NivelStock = p.Stock <= 0 ? "Agotado" : "Bajo",
                NivelStockClase = p.Stock <= 0 ? "danger" : "warning"
            }).ToListAsync();

    public async Task<InventarioDto?> GetByIdAsync(Guid id)
    {
        var p = await _db.Inventario
            .AsNoTracking()
            .Include(x => x.Bodega)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return null;
        return new InventarioDto
        {
            Id = p.Id, TenantId = p.TenantId, Nombre = p.Nombre, SKU = p.SKU, Descripcion = p.Descripcion,
            Categoria = p.Categoria, Stock = p.Stock, StockMinimo = p.StockMinimo,
            PrecioCompra = p.PrecioCompra, PrecioVenta = p.PrecioVenta, Proveedor = p.Proveedor,
            BodegaId = p.BodegaId,
            BodegaNombre = p.Bodega?.Nombre,
            TipoItem = p.TipoItem == TipoItemProducto.Servicio ? "Servicio" : "Refaccion",
            NivelStock = p.NivelStock, NivelStockClase = p.NivelStockClase
        };
    }

    public async Task<ProductoInventario> CreateAsync(InventarioDto dto, Guid tenantId)
    {
        var tipoItem = dto.TipoItem == "Servicio" ? TipoItemProducto.Servicio : TipoItemProducto.Refaccion;
        var producto = new ProductoInventario
        {
            TenantId = tenantId, Nombre = dto.Nombre, SKU = dto.SKU, Descripcion = dto.Descripcion,
            Categoria = dto.Categoria, Stock = dto.Stock, StockMinimo = dto.StockMinimo,
            PrecioCompra = dto.PrecioCompra, PrecioVenta = dto.PrecioVenta, Proveedor = dto.Proveedor,
            BodegaId = dto.BodegaId,
            TipoItem = tipoItem
        };
        _db.Inventario.Add(producto);
        await _db.SaveChangesAsync();
        return producto;
    }

    public async Task UpdateAsync(InventarioDto dto)
    {
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var p = await _db.Inventario
            .FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception("Producto no encontrado");
        p.Nombre = dto.Nombre; p.SKU = dto.SKU; p.Descripcion = dto.Descripcion;
        p.Categoria = dto.Categoria; p.Stock = dto.Stock; p.StockMinimo = dto.StockMinimo;
        p.PrecioCompra = dto.PrecioCompra; p.PrecioVenta = dto.PrecioVenta; p.Proveedor = dto.Proveedor;
        p.BodegaId = dto.BodegaId;
        p.TipoItem = dto.TipoItem == "Servicio" ? TipoItemProducto.Servicio : TipoItemProducto.Refaccion;
        p.FechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Búsqueda rápida de productos para el selector dinámico en Órdenes.
    /// Devuelve hasta 20 coincidencias filtradas por nombre/SKU y opcionalmente por tipo.
    /// </summary>
    public async Task<List<ProductoBusquedaDto>> BuscarAsync(string query, string? tipo = null)
    {
        var q = _db.Inventario
            .AsNoTracking()
            .Include(p => p.Bodega)
            .Where(p => p.Activo && (p.Nombre.Contains(query) || (p.SKU != null && p.SKU.Contains(query))));

        if (tipo == "Servicio")
            q = q.Where(p => p.TipoItem == TipoItemProducto.Servicio);
        else if (tipo == "Refaccion")
            q = q.Where(p => p.TipoItem == TipoItemProducto.Refaccion);

        return await q.Take(20)
            .Select(p => new ProductoBusquedaDto
            {
                Id          = p.Id,
                Nombre      = p.Nombre,
                SKU         = p.SKU,
                Stock       = p.Stock,
                StockMinimo = p.StockMinimo,
                PrecioVenta = p.PrecioVenta,
                TipoItem    = p.TipoItem == TipoItemProducto.Servicio ? "Servicio" : "Refaccion",
                BodegaId    = p.BodegaId,
                BodegaNombre = p.Bodega != null ? p.Bodega.Nombre : null
            })
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task AjustarStockAsync(Guid id, int cantidad, string tipo, Guid tenantId, string? observaciones = null)
    {
        var p = await _db.Inventario
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new Exception("Producto no encontrado");

        var tipoMovimiento = tipo == "entrada" ? TipoMovimiento.AjusteEntrada : TipoMovimiento.AjusteSalida;
        p.Stock = tipo == "entrada" ? p.Stock + cantidad : p.Stock - cantidad;
        p.FechaActualizacion = DateTime.UtcNow;

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            TenantId    = tenantId,
            ProductoId  = p.Id,
            Tipo        = tipoMovimiento,
            Cantidad    = cantidad,
            Referencia  = "Ajuste manual",
            Observaciones = observaciones
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetCategoriasAsync() =>
        await _db.Inventario.Where(p => p.Categoria != null)
            .Select(p => p.Categoria!).Distinct().OrderBy(c => c).ToListAsync();

    /// <summary>
    /// Ejecuta la salida DEFINITIVA de stock de los repuestos consumidos en una orden.
    /// Llamado exclusivamente desde FacturaService al momento de facturar y bloquear.
    /// Registra un MovimientoInventario tipo Salida por cada ítem con producto.
    /// </summary>
    public async Task DescontarStockPorOrdenAsync(Domain.Entities.Orden orden)
    {
        var itemsConProducto = orden.Items
            .Where(i => i.ProductoInventarioId.HasValue)
            .ToList();

        foreach (var item in itemsConProducto)
        {
            var producto = await _db.Inventario
                .FirstOrDefaultAsync(x => x.Id == item.ProductoInventarioId!.Value && x.TenantId == orden.TenantId);
            if (producto == null) continue;

            var cantidadDescontar = (int)Math.Ceiling(item.Cantidad);
            producto.Stock = Math.Max(0, producto.Stock - cantidadDescontar);
            producto.FechaActualizacion = DateTime.UtcNow;

            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                TenantId   = orden.TenantId,
                ProductoId = producto.Id,
                Tipo       = TipoMovimiento.Salida,
                Cantidad   = cantidadDescontar,
                Referencia = orden.NumeroOrden,
                Observaciones = $"Consumo en orden #{orden.NumeroOrden} al facturar"
            });
        }
        // SaveChanges is called by FacturaService after all orders are processed
    }
}

