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
        var query = _db.Vehiculos.AsNoTracking().AsQueryable();
        if (clienteId.HasValue)
            query = query.Where(v => v.ClienteId == clienteId.Value);
            
        var paged = await query.OrderByDescending(v => v.FechaRegistro)
            .Select(v => new VehiculoDto
            {
                Id          = v.Id,
                ClienteId   = v.ClienteId,
                ClienteNombre = v.Cliente != null ? v.Cliente.NombreCompleto : string.Empty,
                Marca       = v.Marca,
                Modelo      = v.Modelo,
                Anio        = v.Anio,
                Placa       = v.Placa,
                VIN         = v.VIN,
                Color       = v.Color,
                Kilometraje = v.Kilometraje
            })
            .ToPagedListAsync(pageNumber, pageSize);
        
        return paged;
    }

    public async Task<List<VehiculoDto>> GetAllAsync(Guid? clienteId = null, string? buscar = null)
    {
        var query = _db.Vehiculos.AsNoTracking().AsQueryable();
        if (clienteId.HasValue)
            query = query.Where(v => v.ClienteId == clienteId.Value);

        if (!string.IsNullOrEmpty(buscar))
        {
            buscar = buscar.Trim().ToLower();
            query = query.Where(v => v.Placa!.ToLower().Contains(buscar) || 
                                     v.Marca.ToLower().Contains(buscar) || 
                                     v.Modelo.ToLower().Contains(buscar));
        }

        // Limit results for lookups
        if (string.IsNullOrEmpty(buscar) && !clienteId.HasValue)
            query = query.Take(50);

        return await query.OrderByDescending(v => v.FechaRegistro)
            .Select(v => new VehiculoDto
            {
                Id          = v.Id,
                ClienteId   = v.ClienteId,
                ClienteNombre = v.Cliente != null ? v.Cliente.NombreCompleto : string.Empty,
                Marca       = v.Marca,
                Modelo      = v.Modelo,
                Anio        = v.Anio,
                Placa       = v.Placa,
                VIN         = v.VIN,
                Color       = v.Color,
                Kilometraje = v.Kilometraje
            })
            .ToListAsync();
    }

    /// <summary>
    /// Returns the top N vehicles ordered by registration date (most recent first).
    /// Pushes Take(count) to SQL — ideal for dropdown population
    /// without fetching the entire table into memory.
    /// </summary>
    public async Task<List<VehiculoDto>> GetTopAsync(int count = 20)
    {
        return await _db.Vehiculos.AsNoTracking()
            .OrderByDescending(v => v.FechaRegistro)
            .Take(count)
            .Select(v => new VehiculoDto
            {
                Id          = v.Id,
                ClienteId   = v.ClienteId,
                ClienteNombre = v.Cliente != null ? v.Cliente.NombreCompleto : string.Empty,
                Marca       = v.Marca,
                Modelo      = v.Modelo,
                Anio        = v.Anio,
                Placa       = v.Placa,
                VIN         = v.VIN,
                Color       = v.Color,
                Kilometraje = v.Kilometraje
            }).ToListAsync();
    }

    public async Task<VehiculoDto?> GetByIdAsync(Guid id)
    {
        var v = await _db.Vehiculos.AsNoTracking().Include(v => v.Cliente).FirstOrDefaultAsync(v => v.Id == id);
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
