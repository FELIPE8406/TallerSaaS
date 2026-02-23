using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;

namespace TallerSaaS.Application.Services;

public class InventarioService
{
    private readonly IApplicationDbContext _db;

    public InventarioService(IApplicationDbContext db) => _db = db;

    public async Task<List<InventarioDto>> GetAllAsync(string? buscar = null, string? categoria = null)
    {
        var query = _db.Inventario.AsQueryable();
        if (!string.IsNullOrEmpty(buscar))
            query = query.Where(p => p.Nombre.Contains(buscar) || (p.SKU != null && p.SKU.Contains(buscar)));
        if (!string.IsNullOrEmpty(categoria))
            query = query.Where(p => p.Categoria == categoria);

        return await query.Select(p => new InventarioDto
        {
            Id = p.Id, Nombre = p.Nombre, SKU = p.SKU, Descripcion = p.Descripcion,
            Categoria = p.Categoria, Stock = p.Stock, StockMinimo = p.StockMinimo,
            PrecioCompra = p.PrecioCompra, PrecioVenta = p.PrecioVenta, Proveedor = p.Proveedor,
            NivelStock = p.Stock <= 0 ? "Agotado" : p.Stock <= p.StockMinimo ? "Bajo" : "OK",
            NivelStockClase = p.Stock <= 0 ? "danger" : p.Stock <= p.StockMinimo ? "warning" : "success"
        }).OrderBy(p => p.Nombre).ToListAsync();
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
        var p = await _db.Inventario.FindAsync(id);
        if (p == null) return null;
        return new InventarioDto
        {
            Id = p.Id, Nombre = p.Nombre, SKU = p.SKU, Descripcion = p.Descripcion,
            Categoria = p.Categoria, Stock = p.Stock, StockMinimo = p.StockMinimo,
            PrecioCompra = p.PrecioCompra, PrecioVenta = p.PrecioVenta, Proveedor = p.Proveedor,
            NivelStock = p.NivelStock, NivelStockClase = p.NivelStockClase
        };
    }

    public async Task<ProductoInventario> CreateAsync(InventarioDto dto, Guid tenantId)
    {
        var producto = new ProductoInventario
        {
            TenantId = tenantId, Nombre = dto.Nombre, SKU = dto.SKU, Descripcion = dto.Descripcion,
            Categoria = dto.Categoria, Stock = dto.Stock, StockMinimo = dto.StockMinimo,
            PrecioCompra = dto.PrecioCompra, PrecioVenta = dto.PrecioVenta, Proveedor = dto.Proveedor
        };
        _db.Inventario.Add(producto);
        await _db.SaveChangesAsync();
        return producto;
    }

    public async Task UpdateAsync(InventarioDto dto)
    {
        var p = await _db.Inventario.FindAsync(dto.Id) ?? throw new Exception("Producto no encontrado");
        p.Nombre = dto.Nombre; p.SKU = dto.SKU; p.Descripcion = dto.Descripcion;
        p.Categoria = dto.Categoria; p.Stock = dto.Stock; p.StockMinimo = dto.StockMinimo;
        p.PrecioCompra = dto.PrecioCompra; p.PrecioVenta = dto.PrecioVenta; p.Proveedor = dto.Proveedor;
        p.FechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task AjustarStockAsync(Guid id, int cantidad, string tipo)  // tipo: "entrada" | "salida"
    {
        var p = await _db.Inventario.FindAsync(id) ?? throw new Exception("Producto no encontrado");
        p.Stock = tipo == "entrada" ? p.Stock + cantidad : p.Stock - cantidad;
        p.FechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetCategoriasAsync() =>
        await _db.Inventario.Where(p => p.Categoria != null)
            .Select(p => p.Categoria!).Distinct().OrderBy(c => c).ToListAsync();
}
