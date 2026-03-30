using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Infrastructure.Data;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentTenantService _tenantService;
    private readonly IApplicationDbContext _db;

    public UsuariosController(UserManager<ApplicationUser> userManager, ICurrentTenantService tenantService, IApplicationDbContext db)
    {
        _userManager = userManager;
        _tenantService = tenantService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId = _tenantService.TenantId;
        var users = await _userManager.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();
            
        return View(users);
    }

    [HttpGet]
    public IActionResult Crear() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string nombre, string email, string password, string role)
    {
        var rolesPermitidos = new[] { "Admin", "Mecanico" };
        if (!rolesPermitidos.Contains(role))
        {
            TempData["Error"] = "Rol no permitido.";
            return RedirectToAction(nameof(Index));
        }

        var tenantId = _tenantService.TenantId;
        if (tenantId == null)
        {
            TempData["Error"] = "No se pudo identificar el taller.";
            return RedirectToAction(nameof(Index));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NombreCompleto = nombre,
            TenantId = tenantId,
            EmailConfirmed = true,
            Activo = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
            
            // If it's a mechanic, add default availability (Mon-Fri 8:00 - 18:00)
            if (role == "Mecanico")
            {
                for (int i = 1; i <= 5; i++) // Mon-Fri
                {
                    _db.MechanicAvailabilities.Add(new MechanicAvailability
                    {
                        MechanicId = user.Id,
                        DayOfWeek = i,
                        StartTime = new TimeSpan(8, 0, 0),
                        EndTime = new TimeSpan(18, 0, 0),
                        IsActive = true
                    });
                }
                await _db.SaveChangesAsync();
            }

            TempData["Exito"] = $"Usuario {nombre} creado correctamente como {role}.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = string.Join("<br/>", result.Errors.Select(e => e.Description));
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null && user.TenantId == _tenantService.TenantId)
        {
            // Instead of hard delete, we often deactivate
            user.Activo = false;
            await _userManager.UpdateAsync(user);
            TempData["Exito"] = "Usuario desactivado correctamente.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Disponibilidad(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.TenantId != _tenantService.TenantId) return NotFound();

        var availability = await _db.MechanicAvailabilities
            .Where(a => a.MechanicId == id)
            .OrderBy(a => a.DayOfWeek)
            .ToListAsync();

        ViewBag.User = user;
        return View(availability);
    }

    [HttpPost]
    public async Task<IActionResult> Disponibilidad(string mechanicId, List<MechanicAvailability> items)
    {
        var user = await _userManager.FindByIdAsync(mechanicId);
        if (user == null || user.TenantId != _tenantService.TenantId) return NotFound();

        // Remove old and add new (simpler than update logic)
        var old = await _db.MechanicAvailabilities.Where(a => a.MechanicId == mechanicId).ToListAsync();
        _db.MechanicAvailabilities.RemoveRange(old);

        foreach (var item in items)
        {
            if (item.IsActive)
            {
                item.MechanicId = mechanicId;
                item.TenantId = (Guid)user.TenantId!; // Fix: Cast Guid? to Guid safely
                _db.MechanicAvailabilities.Add(item);
            }
        }

        await _db.SaveChangesAsync();
        TempData["Exito"] = "Jornada laboral actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
