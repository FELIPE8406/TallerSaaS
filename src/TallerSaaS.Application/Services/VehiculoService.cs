using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Extensions;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;

namespace TallerSaaS.Application.Services;

public class VehiculoService
{
    private readonly IApplicationDbContext _db;
    public VehiculoService(IApplicationDbContext db) => _db = db;

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static VehiculoDto Map(Vehiculo v) => new()
    {
        Id          = v.Id,
        ClienteId   = v.ClienteId,
        ClienteNombre = v.Cliente?.NombreCompleto ?? string.Empty,
        Marca       = v.Marca,
        Modelo      = v.Modelo,
        Anio        = v.Anio,
        Placa       = v.Placa,
        VIN         = v.VIN,
        Color       = v.Color,
        Kilometraje = v.Kilometraje
    };

    // ── Queries ───────────────────────────────────────────────────────────────
    public async Task<PagedResult<VehiculoDto>> GetAllPagedAsync(int pageNumber, int pageSize, Guid? clienteId = null)
    {
        var query = _db.Vehiculos.Include(v => v.Cliente).AsQueryable();
        if (clienteId.HasValue)
            query = query.Where(v => v.ClienteId == clienteId.Value);
            
        var orderedQuery = query.OrderBy(v => v.Marca).ThenBy(v => v.Modelo);
        var paged = await orderedQuery.ToPagedListAsync(pageNumber, pageSize);
        
        return new PagedResult<VehiculoDto>
        {
            Data = paged.Data.Select(Map).ToList(),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
    }

    public async Task<List<VehiculoDto>> GetAllAsync(Guid? clienteId = null)
    {
        var query = _db.Vehiculos.Include(v => v.Cliente).AsQueryable();
        if (clienteId.HasValue)
            query = query.Where(v => v.ClienteId == clienteId.Value);

        var list = await query.OrderBy(v => v.Marca).ThenBy(v => v.Modelo).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<VehiculoDto?> GetByIdAsync(Guid id)
    {
        var v = await _db.Vehiculos.Include(v => v.Cliente).FirstOrDefaultAsync(v => v.Id == id);
        return v == null ? null : Map(v);
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    public async Task<Vehiculo> CreateAsync(VehiculoDto dto, Guid tenantId)
    {
        var vehiculo = new Vehiculo
        {
            TenantId   = tenantId,
            ClienteId  = dto.ClienteId,
            Marca      = dto.Marca,
            Modelo     = dto.Modelo,
            Anio       = dto.Anio,
            Placa      = dto.Placa?.ToUpper().Trim(),
            VIN        = dto.VIN,
            Color      = dto.Color,
            Kilometraje = dto.Kilometraje
        };
        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync();
        return vehiculo;
    }

    public async Task UpdateAsync(VehiculoDto dto)
    {
        var v = await _db.Vehiculos.FindAsync(dto.Id)
                ?? throw new Exception("Vehículo no encontrado");
        v.Marca      = dto.Marca;
        v.Modelo     = dto.Modelo;
        v.Anio       = dto.Anio;
        v.Placa      = dto.Placa?.ToUpper().Trim();
        v.VIN        = dto.VIN;
        v.Color      = dto.Color;
        v.Kilometraje = dto.Kilometraje;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var v = await _db.Vehiculos.Include(x => x.Ordenes)
                    .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Vehículo no encontrado");
        if (v.Ordenes.Any())
            throw new InvalidOperationException("No se puede eliminar: tiene órdenes asociadas.");
        _db.Vehiculos.Remove(v);
        await _db.SaveChangesAsync();
    }
}
