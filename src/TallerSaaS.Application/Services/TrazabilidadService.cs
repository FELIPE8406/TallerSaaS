using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Application.Services;

/// <summary>
/// Servicio de Trazabilidad: registra y consulta el historial de eventos por vehículo.
/// </summary>
public class TrazabilidadService
{
    private readonly IApplicationDbContext _db;

    public TrazabilidadService(IApplicationDbContext db) => _db = db;

    // ── Registro ──────────────────────────────────────────────────────────────
    public async Task RegistrarEventoAsync(
        Guid vehiculoId,
        TipoEvento tipo,
        string descripcion,
        Guid referenciaId,
        Guid tenantId)
    {
        var evento = new EventoTrazabilidad
        {
            VehiculoId    = vehiculoId,
            TenantId      = tenantId,
            Tipo          = tipo,
            Descripcion   = descripcion,
            ReferenciaId  = referenciaId,
            FechaEvento   = DateTime.UtcNow
        };
        _db.EventosTrazabilidad.Add(evento);
        await _db.SaveChangesAsync();
    }

    // ── Consulta ──────────────────────────────────────────────────────────────
    public async Task<TimelineVehiculoDto?> GetTimelineByVehiculoAsync(Guid vehiculoId)
    {
        var vehiculo = await _db.Vehiculos
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == vehiculoId);

        if (vehiculo == null) return null;

        var eventos = await _db.EventosTrazabilidad
            .Where(e => e.VehiculoId == vehiculoId)
            .OrderBy(e => e.FechaEvento)
            .ToListAsync();

        return new TimelineVehiculoDto
        {
            VehiculoId          = vehiculo.Id,
            VehiculoDescripcion = vehiculo.Descripcion,
            ClienteNombre       = vehiculo.Cliente?.NombreCompleto,
            Eventos             = eventos.Select(e => new EventoTrazabilidadDto
            {
                Id           = e.Id,
                VehiculoId   = e.VehiculoId,
                Tipo         = (int)e.Tipo,
                TipoIcono    = e.TipoIcono,
                TipoClase    = e.TipoClase,
                Descripcion  = e.Descripcion,
                ReferenciaId = e.ReferenciaId,
                FechaEvento  = e.FechaEvento
            }).ToList()
        };
    }
}
