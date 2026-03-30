using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Infrastructure.Data;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize]
public class AgendaController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;

    public AgendaController(
        IAppointmentService appointmentService,
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext db,
        ICurrentTenantService tenantService)
    {
        _appointmentService = appointmentService;
        _userManager = userManager;
        _db = db;
        _tenantService = tenantService;
    }

    public async Task<IActionResult> Index()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var tenantId = _tenantService.TenantId;

        var users = await _userManager.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        var availabilities = await _db.MechanicAvailabilities
            .Where(a => a.TenantId == tenantId && userIds.Contains(a.MechanicId) && a.IsActive)
            .ToListAsync();

        var mechanicsList = users.Select(m => new
        {
            Id = m.Id,
            NombreCompleto = m.NombreCompleto,
            BusinessHours = availabilities
                .Where(a => a.MechanicId == m.Id)
                .Select(a => new
                {
                    daysOfWeek = new[] { a.DayOfWeek },
                    startTime = a.StartTime.ToString(@"hh\:mm"),
                    endTime = a.EndTime.ToString(@"hh\:mm")
                })
                .ToList()
        }).ToList();

        ViewBag.Mechanics = mechanicsList;

        ViewBag.Clientes = await _db.Clientes.OrderBy(c => c.NombreCompleto)
            .Select(c => new { c.Id, c.NombreCompleto }).ToListAsync();

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments(DateTime start, DateTime end)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        try
        {
            var appointments = await _appointmentService.GetAppointmentsAsync(start, end);

            var events = appointments.Select(a => new
            {
                id = a.Id,
                resourceId = a.MechanicId,
                title = $"{a.ClienteNombre} - {a.VehiculoDescripcion}",
                start = a.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = a.EndDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                extendedProps = new
                {
                    serviceType = a.ServiceType,
                    status = a.Status,
                    statusTexto = a.StatusTexto,
                    clienteId = a.ClienteId,
                    vehiculoId = a.VehiculoId,
                    clienteNombre = a.ClienteNombre,
                    vehiculoDescripcion = a.VehiculoDescripcion
                },
                backgroundColor = GetColorByMechanic(a.MechanicId),
                borderColor = GetColorByMechanic(a.MechanicId)
            });

            return Json(events);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error interno al cargar la agenda.", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveAppointment([FromBody] AppointmentDto? dto)
    {
        if (!_tenantService.TenantId.HasValue) return Json(new { success = false, message = "Tenant no identificado." });
        try
        {
            if (dto == null)
                return Json(new { success = false, message = "Datos de cita inválidos." });

            if (dto.Id != Guid.Empty)
            {
                var existing = await _appointmentService.GetAppointmentByIdAsync(dto.Id);
                if (existing == null) return Json(new { success = false, message = "Cita no encontrada." });
                if (existing.TenantId != _tenantService.TenantId.Value) return Json(new { success = false, message = "Acceso no autorizado." });
            }

            if (dto.Id == Guid.Empty)
                await _appointmentService.CreateAppointmentAsync(dto);
            else
                await _appointmentService.UpdateAppointmentAsync(dto);

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, int status)
    {
        if (!_tenantService.TenantId.HasValue) return Json(new { success = false, message = "Tenant no identificado." });
        try
        {
            var existing = await _appointmentService.GetAppointmentByIdAsync(id);
            if (existing == null) return Json(new { success = false, message = "Cita no encontrada." });
            if (existing.TenantId != _tenantService.TenantId.Value) return Json(new { success = false, message = "Acceso no autorizado." });

            await _appointmentService.UpdateAppointmentStatusAsync(id, status);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ConvertToOrder(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Json(new { success = false, message = "Tenant no identificado." });
        try
        {
            var existing = await _appointmentService.GetAppointmentByIdAsync(id);
            if (existing == null) return Json(new { success = false, message = "Cita no encontrada." });
            if (existing.TenantId != _tenantService.TenantId.Value) return Json(new { success = false, message = "Acceso no autorizado." });

            var orderId = await _appointmentService.ConvertToServiceOrderAsync(id);
            return Json(new { success = true, orderId });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SendReminder(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Json(new { success = false, message = "Tenant no identificado." });
        try
        {
            var existing = await _appointmentService.GetAppointmentByIdAsync(id);
            if (existing == null) return Json(new { success = false, message = "Cita no encontrada." });
            if (existing.TenantId != _tenantService.TenantId.Value) return Json(new { success = false, message = "Acceso no autorizado." });

            await _appointmentService.SendWhatsappReminderAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetVehiculos(Guid clienteId)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var vehiculos = await _db.Vehiculos
            .AsNoTracking()
            .Where(v => v.ClienteId == clienteId)
            .Select(v => new { v.Id, Descripcion = $"{v.Anio} {v.Marca} {v.Modelo} ({v.Placa})" })
            .ToListAsync();
        return Json(vehiculos);
    }

    private string GetColorByMechanic(string mechanicId)
    {
        if (string.IsNullOrEmpty(mechanicId)) return "#6c757d";
        var colors = new[]
        {
            "#0d6efd",
            "#198754",
            "#dc3545",
            "#fd7e14",
            "#6f42c1",
            "#20c997",
            "#d63384",
            "#0dcaf0"
        };
        int index = Math.Abs(mechanicId.GetHashCode()) % colors.Length;
        return colors[index];
    }
}
