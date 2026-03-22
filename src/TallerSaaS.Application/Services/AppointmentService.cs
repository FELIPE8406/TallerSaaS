using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;
    private readonly OrdenService _ordenService;

    public AppointmentService(
        IApplicationDbContext db, 
        ICurrentTenantService tenantService,
        OrdenService ordenService)
    {
        _db = db;
        _tenantService = tenantService;
        _ordenService = ordenService;
    }

    public async Task<List<AppointmentDto>> GetAppointmentsAsync(DateTime start, DateTime end)
    {
        var tenantId = _tenantService.TenantId ?? throw new Exception("Tenant no identificado");
        
        // FullCalendar u otros clientes pueden enviar fechas extremas al retroceder mucho,
        // esto causa excepciones en TimeZoneHelper.ToUtcFromColombia.
        if (start < new DateTime(2000, 1, 1)) start = new DateTime(2000, 1, 1);
        if (end > new DateTime(2100, 1, 1)) end = new DateTime(2100, 1, 1);

        var utcStart = TimeZoneHelper.ToUtcFromColombia(start);
        var utcEnd = TimeZoneHelper.ToUtcFromColombia(end);

        var data = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.StartDateTime <= utcEnd && a.EndDateTime >= utcStart)
            .OrderBy(a => a.StartDateTime)
            .Select(a => new
            {
                a.Id,
                a.ClienteId,
                ClienteNombre = a.Cliente != null ? a.Cliente.NombreCompleto : "N/A",
                a.VehiculoId,
                VehiculoAnio = a.Vehiculo != null ? a.Vehiculo.Anio : 0,
                VehiculoMarca = a.Vehiculo != null ? a.Vehiculo.Marca : "",
                VehiculoModelo = a.Vehiculo != null ? a.Vehiculo.Modelo : "",
                VehiculoPlaca = a.Vehiculo != null ? (a.Vehiculo.Placa ?? "ND") : "ND",
                a.MechanicId,
                a.StartDateTime,
                a.EndDateTime,
                a.EstimatedDuration,
                a.ServiceType,
                Status = (int)a.Status,
                a.WhatsappReminderSent
            })
            .ToListAsync();
        
        // Final mapping for non-translatable fields
        return data.Select(a => new AppointmentDto {
            Id = a.Id,
            ClienteId = a.ClienteId,
            ClienteNombre = a.ClienteNombre,
            VehiculoId = a.VehiculoId,
            VehiculoDescripcion = $"{a.VehiculoAnio} {a.VehiculoMarca} {a.VehiculoModelo} ({a.VehiculoPlaca})",
            MechanicId = a.MechanicId,
            StartDateTime = TimeZoneHelper.ToColombiaFromUtc(a.StartDateTime),
            EndDateTime = TimeZoneHelper.ToColombiaFromUtc(a.EndDateTime),
            EstimatedDuration = a.EstimatedDuration,
            ServiceType = a.ServiceType,
            Status = a.Status,
            StatusTexto = ((AppointmentStatus)a.Status) switch 
            {
                AppointmentStatus.Confirmed => "Confirmada",
                AppointmentStatus.CheckedIn => "En Taller",
                AppointmentStatus.Completed => "Completada/Facturada",
                AppointmentStatus.Cancelled => "Cancelada",
                _ => a.Status.ToString()
            },
            WhatsappReminderSent = a.WhatsappReminderSent
        }).ToList();
    }

    public async Task<AppointmentDto> GetAppointmentByIdAsync(Guid id)
    {
        var a = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Cliente)
            .Include(a => a.Vehiculo)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new Exception("Cita no encontrada");

        return MapToDto(a);
    }

    public async Task<Guid> CreateAppointmentAsync(AppointmentDto dto)
    {
        var tenantId = _tenantService.TenantId ?? throw new Exception("Tenant no identificado");

        // UI sends times in Colombian Local Time. We validate overlapping in local time.
        // Wait! We should store it in UTC but validate overlaps natively or in UTC.
        // HasOverlapAsync expects UTC to match the DB cleanly.
        var utcStart = TimeZoneHelper.ToUtcFromColombia(dto.StartDateTime);
        var utcEnd = TimeZoneHelper.ToUtcFromColombia(dto.EndDateTime);

        // 1. Conflict Validation (Overlap)
        if (await HasOverlapAsync(tenantId, dto.MechanicId, utcStart, utcEnd))
            throw new Exception("El mecánico ya tiene una cita programada en este horario.");

        // 2. Availability Validation (Evaluates local bounds over UTC ranges but DB rules run mapping naturally)
        if (!await IsMechanicAvailableAsync(dto.MechanicId, dto.StartDateTime, dto.EndDateTime))
            throw new Exception("El horario seleccionado está fuera de la jornada laboral del mecánico.");

        var appointment = new Appointment
        {
            TenantId = tenantId,
            ClienteId = dto.ClienteId,
            VehiculoId = dto.VehiculoId,
            MechanicId = dto.MechanicId,
            // Guardar estrictamente en UTC
            StartDateTime = utcStart,
            EndDateTime = utcEnd,
            EstimatedDuration = dto.EstimatedDuration,
            ServiceType = dto.ServiceType,
            Status = AppointmentStatus.Confirmed,
            FechaRegistro = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        return appointment.Id;
    }

    public async Task UpdateAppointmentAsync(AppointmentDto dto)
    {
        var tenantId = _tenantService.TenantId ?? throw new Exception("Tenant no identificado");
        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId) 
            ?? throw new Exception("Cita no encontrada");

        var utcStart = TimeZoneHelper.ToUtcFromColombia(dto.StartDateTime);
        var utcEnd = TimeZoneHelper.ToUtcFromColombia(dto.EndDateTime);

        // Check for conflicts if time or mechanic changed
        if (a.StartDateTime != utcStart || a.EndDateTime != utcEnd || a.MechanicId != dto.MechanicId)
        {
            if (await HasOverlapAsync(tenantId, dto.MechanicId, utcStart, utcEnd, dto.Id))
                throw new Exception("El mecánico ya tiene una cita programada en este horario.");
                
            if (!await IsMechanicAvailableAsync(dto.MechanicId, dto.StartDateTime, dto.EndDateTime))
                throw new Exception("El mecánico no está disponible en ese horario.");
        }

        a.MechanicId = dto.MechanicId;
        a.StartDateTime = utcStart;
        a.EndDateTime = utcEnd;
        a.EstimatedDuration = dto.EstimatedDuration;
        a.ServiceType = dto.ServiceType;
        a.Status = (AppointmentStatus)dto.Status;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAppointmentAsync(Guid id)
    {
        var a = await _db.Appointments.FindAsync(id) ?? throw new Exception("Cita no encontrada");
        _db.Appointments.Remove(a);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAppointmentStatusAsync(Guid id, int status)
    {
        var a = await _db.Appointments.FindAsync(id) ?? throw new Exception("Cita no encontrada");
        var newStatus = (AppointmentStatus)status;

        // State Machine Validation
        if (newStatus == AppointmentStatus.Completed && a.Status != AppointmentStatus.CheckedIn)
        {
            throw new Exception("No se puede marcar como completada una cita que no ha sido registrada (Checked-in).");
        }

        if (a.Status == AppointmentStatus.Cancelled && newStatus != AppointmentStatus.Cancelled)
        {
            throw new Exception("No se puede modificar el estado de una cita cancelada.");
        }

        a.Status = newStatus;
        await _db.SaveChangesAsync();
    }

    public async Task<Guid> ConvertToServiceOrderAsync(Guid appointmentId)
    {
        var a = await _db.Appointments
            .Include(a => a.Cliente)
            .Include(a => a.Vehiculo)
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new Exception("Cita no encontrada");

        if (a.Status == AppointmentStatus.Completed)
            throw new Exception("Esta cita ya fue procesada.");

        var tenantId = _tenantService.TenantId ?? throw new Exception("Tenant no identificado");

        // Create the Service Order via OrdenService
        var ordenDto = new OrdenDto
        {
            VehiculoId = a.VehiculoId,
            DiagnosticoInicial = $"Cita de {a.ServiceType}. Programada para {a.StartDateTime:dd/MM/yyyy HH:mm}",
            Estado = 1 // Recibido
        };

        var orden = await _ordenService.CreateAsync(ordenDto, tenantId);
        
        // Traceability Link
        var dbOrden = await _db.Ordenes.FindAsync(orden.Id);
        if (dbOrden != null)
        {
            dbOrden.AppointmentId = a.Id;
        }

        // Mark appointment as checked-in/completed
        a.Status = AppointmentStatus.Completed;
        await _db.SaveChangesAsync();

        return orden.Id;
    }

    public async Task<List<MechanicAvailabilityDto>> GetMechanicAvailabilityAsync(string mechanicId)
    {
        var availabilities = await _db.MechanicAvailabilities
            .AsNoTracking()
            .Where(m => m.MechanicId == mechanicId)
            .OrderBy(m => m.DayOfWeek)
            .ToListAsync();

        return availabilities.Select(m => new MechanicAvailabilityDto
        {
            Id = m.Id,
            MechanicId = m.MechanicId,
            DayOfWeek = m.DayOfWeek,
            StartTime = m.StartTime.ToString(@"hh\:mm"),
            EndTime = m.EndTime.ToString(@"hh\:mm"),
            IsActive = m.IsActive
        }).ToList();
    }

    public async Task UpdateMechanicAvailabilityAsync(string mechanicId, List<MechanicAvailabilityDto> dtos)
    {
        var tenantId = _tenantService.TenantId ?? throw new Exception("Tenant no identificado");

        // Remove old availability for this mechanic ONLY for the current tenant
        var old = await _db.MechanicAvailabilities
            .Where(m => m.MechanicId == mechanicId && m.TenantId == tenantId)
            .ToListAsync();
        _db.MechanicAvailabilities.RemoveRange(old);

        // Add new rules
        foreach (var dto in dtos)
        {
            _db.MechanicAvailabilities.Add(new MechanicAvailability
            {
                TenantId = tenantId,
                MechanicId = mechanicId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = TimeSpan.Parse(dto.StartTime),
                EndTime = TimeSpan.Parse(dto.EndTime),
                IsActive = dto.IsActive
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsMechanicAvailableAsync(string mechanicId, DateTime start, DateTime end)
    {
        if (end <= start) return false;

        // Fix N+1 Query: Cargar todas las reglas del mecánico una sola vez
        // en lugar de hacerlo iteración por iteración en el bucle
        var allRules = await _db.MechanicAvailabilities
            .AsNoTracking()
            .Where(m => m.MechanicId == mechanicId)
            .ToListAsync();
            
        var activeRules = allRules.Where(m => m.IsActive).ToList();
        bool hasAnyRules = allRules.Any();

        var current = start;
        while (current < end)
        {
            var dayStart = current;
            var dayEnd = current.Date.AddDays(1);
            if (dayEnd > end) dayEnd = end;

            int dayOfWeek = (int)dayStart.DayOfWeek;
            var startT = dayStart.TimeOfDay;
            var endT = dayEnd.TimeOfDay;
            
            // Special case: if segment ends exactly at midnight of next day, 
            // the TimeOfDay is 00:00:00, but for comparison we need 24:00:00
            if (dayEnd.TimeOfDay == TimeSpan.Zero && dayEnd.Date > dayStart.Date)
            {
                endT = TimeSpan.FromHours(24);
            }

            var rules = activeRules.Where(m => m.DayOfWeek == dayOfWeek).ToList();

            if (!rules.Any()) 
            {
                // Fallback: Monday to Friday, 08:00 to 18:00
                // ONLY if the mechanic has NO custom rules at all
                if (!hasAnyRules)
                {
                    bool isWorkDay = dayOfWeek >= 1 && dayOfWeek <= 5;
                    bool withinHours = startT >= new TimeSpan(8, 0, 0) && endT <= new TimeSpan(18, 0, 0);
                    if (!(isWorkDay && withinHours)) return false;
                }
                else return false; // Has rules for other days, but not this one
            }
            else
            {
                // Must fit within at least one active shift segment
                if (!rules.Any(r => startT >= r.StartTime && endT <= r.EndTime))
                    return false;
            }

            current = dayEnd; // Move to next day segment
        }

        return true;
    }

    public async Task SendWhatsappReminderAsync(Guid appointmentId)
    {
        var a = await _db.Appointments
            .Include(a => a.Cliente)
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new Exception("Cita no encontrada");

        // Mock Logic for WhatsApp Notification
        // In a real scenario, this would call Twilio or a similar API.
        var message = $"Hola {a.Cliente?.NombreCompleto}, tu cita en {a.Tenant?.Nombre} está agendada para el {a.StartDateTime:dd/MM/yyyy} a las {a.StartDateTime:HH:mm}.";
        
        System.Diagnostics.Debug.WriteLine($"[WHATSAPP MOCK] Sent to {a.Cliente?.Telefono}: {message}");
        
        a.WhatsappReminderSent = true;
        await _db.SaveChangesAsync();
    }

    private async Task<bool> HasOverlapAsync(Guid tenantId, string mechanicId, DateTime utcStart, DateTime utcEnd, Guid? excludeId = null)
    {
        // Mandatory Double-Booking Logic checking overlapping boundaries:
        // New_Start < Existing_End AND New_End > Existing_Start
        return await _db.Appointments.AnyAsync(a => 
            a.TenantId == tenantId &&
            a.MechanicId == mechanicId &&
            a.Id != excludeId &&
            a.Status != AppointmentStatus.Cancelled &&
            utcStart < a.EndDateTime && utcEnd > a.StartDateTime);
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id,
        ClienteId = a.ClienteId,
        ClienteNombre = a.Cliente?.NombreCompleto ?? "N/A",
        VehiculoId = a.VehiculoId,
        VehiculoDescripcion = a.Vehiculo != null ? $"{a.Vehiculo.Anio} {a.Vehiculo.Marca} {a.Vehiculo.Modelo} ({a.Vehiculo.Placa ?? "ND"})" : "N/A",
        MechanicId = a.MechanicId,
        // UI expects Local Time, convert DB UTC to Colombia Time
        StartDateTime = TimeZoneHelper.ToColombiaFromUtc(a.StartDateTime),
        EndDateTime = TimeZoneHelper.ToColombiaFromUtc(a.EndDateTime),
        EstimatedDuration = a.EstimatedDuration,
        ServiceType = a.ServiceType,
        Status = (int)a.Status,
        StatusTexto = a.Status switch 
        {
            AppointmentStatus.Confirmed => "Confirmada",
            AppointmentStatus.CheckedIn => "En Taller",
            AppointmentStatus.Completed => "Completada/Facturada",
            AppointmentStatus.Cancelled => "Cancelada",
            _ => a.Status.ToString()
        },
        WhatsappReminderSent = a.WhatsappReminderSent
    };
}
