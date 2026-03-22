using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Infrastructure.Data;
using System.Security.Claims;

namespace TallerSaaS.Web.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly ApplicationDbContext _db;

    public SubscriptionController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Subscription
    public async Task<IActionResult> Index()
    {
        // Si el usuario es SuperAdmin, no necesita plan (o ya lo gestiona en su panel)
        if (User.IsInRole("SuperAdmin"))
            return RedirectToAction("Index", "SuperAdmin");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Id == userId);

        // Si ya tiene plan activo, redirigir al Dashboard
        if (user?.Tenant?.PlanSuscripcionId != null)
            return RedirectToAction("Index", "Dashboard");

        var planes = await _db.PlanesSuscripcion
            .Where(p => p.Activo)
            .OrderBy(p => p.Precio)
            .ToListAsync();

        return View(planes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPlan(int planId)
    {
        var plan = await _db.PlanesSuscripcion.FindAsync(planId);
        if (plan == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return Unauthorized();

        // Si no tiene taller (Tenant), lo creamos por defecto o pedimos datos.
        // Por ahora, creamos uno básico para permitir la transición.
        if (user.Tenant == null)
        {
            var newTenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Nombre = $"Taller de {user.NombreCompleto}",
                PlanSuscripcionId = planId,
                FechaAlta = DateTime.UtcNow,
                Activo = true
            };
            _db.Tenants.Add(newTenant);
            user.TenantId = newTenant.Id;
        }
        else
        {
            user.Tenant.PlanSuscripcionId = planId;
        }

        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Has seleccionado el plan {plan.Nombre}. ¡Bienvenido a GEARDASH!";
        return RedirectToAction("Index", "Dashboard");
    }
}
