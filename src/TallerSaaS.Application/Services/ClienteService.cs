using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Application.Services;

public class ClienteService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;

    public ClienteService(IApplicationDbContext db, ICurrentTenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
    }

    public async Task<List<ClienteDto>> GetAllAsync(string? buscar = null)
    {
        var query = _db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(buscar))
        {
            buscar = buscar.Trim().ToLower();
            query = query.Where(c => c.NombreCompleto.ToLower().Contains(buscar) ||
                                     (c.Cedula != null && c.Cedula.Contains(buscar)) ||
                                     (c.Telefono != null && c.Telefono.Contains(buscar)));
        }

        // Limit large results—if no search, only top 50
        if (string.IsNullOrEmpty(buscar))
            query = query.Take(50);

        return await query.Select(c => new ClienteDto
        {
            Id = c.Id, TenantId = c.TenantId, NombreCompleto = c.NombreCompleto, Email = c.Email,
            Telefono = c.Telefono, Direccion = c.Direccion, Cedula = c.Cedula,
            FechaRegistro = c.FechaRegistro, Activo = c.Activo,
            TotalVehiculos = c.Vehiculos.Count
        }).OrderBy(c => c.NombreCompleto).ToListAsync();
    }

    /// <summary>
    /// Búsqueda optimizada para autocompletar (endpoint JSON).
    /// Evita devolver miles de clientes al navegador.
    /// </summary>
    public async Task<List<ClienteDto>> BuscarTopAsync(string? buscar, int take = 20)
    {
        var query = _db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(buscar))
        {
            buscar = buscar.Trim().ToLower();
            query = query.Where(c => c.NombreCompleto.ToLower().Contains(buscar) ||
                                     (c.Cedula != null && c.Cedula.Contains(buscar)) ||
                                     (c.Telefono != null && c.Telefono.Contains(buscar)));
        }

        return await query
            .Select(c => new ClienteDto
            {
                Id = c.Id,
                TenantId = c.TenantId,
                NombreCompleto = c.NombreCompleto,
                Email = c.Email,
                Telefono = c.Telefono,
                Direccion = c.Direccion,
                Cedula = c.Cedula,
                FechaRegistro = c.FechaRegistro,
                Activo = c.Activo,
                TotalVehiculos = c.Vehiculos.Count
            })
            .OrderBy(c => c.NombreCompleto)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>
    /// Returns the top N active clients ordered by name.
    /// Pushes Take(count) to SQL — ideal for dropdown population
    /// without fetching the entire table into memory.
    /// </summary>
    public async Task<List<ClienteDto>> GetTopAsync(int count = 20)
    {
        return await _db.Clientes.AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.NombreCompleto)
            .Take(count)
            .Select(c => new ClienteDto
            {
                Id = c.Id, NombreCompleto = c.NombreCompleto, Email = c.Email,
                Telefono = c.Telefono, Direccion = c.Direccion, Cedula = c.Cedula,
                FechaRegistro = c.FechaRegistro, Activo = c.Activo
            }).ToListAsync();
    }

    public async Task<ClienteDto?> GetByIdAsync(Guid id) =>
        await _db.Clientes.AsNoTracking().Where(c => c.Id == id).Select(c => new ClienteDto
        {
            Id = c.Id, TenantId = c.TenantId, NombreCompleto = c.NombreCompleto, Email = c.Email,
            Telefono = c.Telefono, Direccion = c.Direccion, Cedula = c.Cedula,
            FechaRegistro = c.FechaRegistro, Activo = c.Activo,
            TotalVehiculos = c.Vehiculos.Count
        }).FirstOrDefaultAsync();

    public async Task<Cliente> CreateAsync(ClienteDto dto, Guid tenantId)
    {
        var cliente = new Cliente
        {
            TenantId = tenantId, NombreCompleto = dto.NombreCompleto,
            Email = dto.Email, Telefono = dto.Telefono,
            Direccion = dto.Direccion, Cedula = dto.Cedula
        };
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();
        return cliente;
    }

    public async Task UpdateAsync(ClienteDto dto)
    {
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception("Cliente no encontrado");
        cliente.NombreCompleto = dto.NombreCompleto;
        cliente.Email = dto.Email;
        cliente.Telefono = dto.Telefono;
        cliente.Direccion = dto.Direccion;
        cliente.Cedula = dto.Cedula;
        cliente.Activo = dto.Activo;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception("Cliente no encontrado");
        cliente.Activo = false;
        await _db.SaveChangesAsync();
    }

    public async Task ToggleStatusAsync(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) throw new UnauthorizedAccessException("Tenant no identificado.");
        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenantService.TenantId.Value)
            ?? throw new Exception("Cliente no encontrado");
        cliente.Activo = !cliente.Activo;
        await _db.SaveChangesAsync();
    }
}
