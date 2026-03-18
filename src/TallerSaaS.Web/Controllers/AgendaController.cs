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
        var tenantId = _tenantService.TenantId;
        
        // Get all users of the current tenant (Admins can also be mechanics in small workshops)
        var users = await _userManager.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();
            
        var userIds = users.Select(u => u.Id).ToList();
        
        // Load active mechanic availabilities
        var availabilities = await _db.MechanicAvailabilities
            .Where(a => a.TenantId == tenantId && userIds.Contains(a.MechanicId) && a.IsActive)
            .ToListAsync();

        var mechanicsList = users.Select(m => new 
        { 
            Id = m.Id, 
            NombreCompleto = m.NombreCompleto,
            // FullCalendar format for businessHours
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
        
        // Clients and Vehicles for the modal
        ViewBag.Clientes = await _db.Clientes.OrderBy(c => c.NombreCompleto)
            .Select(c => new { c.Id, c.NombreCompleto }).ToListAsync();
            
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments(DateTime start, DateTime end)
    {
        try
        {
            var appointments = await _appointmentService.GetAppointmentsAsync(start, end);
            
            // Format for FullCalendar
            var events = appointments.Select(a => new
            {
                id = a.Id,
                resourceId = a.MechanicId,
                title = $"{a.ClienteNombre} - {a.VehiculoDescripcion}",
                start = a.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"), // Safe: Already translated back to Local by Service
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
            // Retornar un error en formato JSON para evitar que la vista se quede colgada
            return StatusCode(500, new { success = false, message = "Error interno al cargar la agenda.", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveAppointment([FromBody] AppointmentDto? dto)
    {
        try
        {
            if (dto == null)
            {
                return Json(new { success = false, message = "Datos de cita inválidos." });
            }

            if (dto.Id == Guid.Empty)
            {
                await _appointmentService.CreateAppointmentAsync(dto);
            }
            else
            {
                await _appointmentService.UpdateAppointmentAsync(dto);
            }
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
        try
        {
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
        try
        {
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
        try
        {
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
        var vehiculos = await _db.Vehiculos
            .Where(v => v.ClienteId == clienteId)
            .Select(v => new { v.Id, Descripcion = $"{v.Anio} {v.Marca} {v.Modelo} ({v.Placa})" })
            .ToListAsync();
        return Json(vehiculos);
    }

    private string GetColorByMechanic(string mechanicId)
    {
        if (string.IsNullOrEmpty(mechanicId)) return "#6c757d"; // Default Gray
        
        // Generate a deterministic color based on the mechanicId hash
        var colors = new[] 
        { 
            "#0d6efd", // Blue
            "#198754", // Green
            "#dc3545", // Red
            "#fd7e14", // Orange
            "#6f42c1", // Purple
            "#20c997", // Teal
            "#d63384", // Pink
            "#0dcaf0"  // Cyan
        };
        
        // Use absolute value of hash code to index into the color array
        int index = Math.Abs(mechanicId.GetHashCode()) % colors.Length;
        return colors[index];
    }
}
