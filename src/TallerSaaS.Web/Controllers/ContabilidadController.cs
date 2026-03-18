using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize]
public class ContabilidadController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly ReporteService _reporteService;
    private readonly ICurrentTenantService _tenantService;

    public ContabilidadController(
        IApplicationDbContext db,
        ReporteService reporteService,
        ICurrentTenantService tenantService)
    {
        _db = db;
        _reporteService = reporteService;
        _tenantService = tenantService;
    }

    private async Task<bool> IsPremiumAsync()
    {
        var tenantId = _tenantService.TenantId;
        if (tenantId == null) return false;

        var tenant = await _db.Tenants
            .Include(t => t.PlanSuscripcion)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        // PlanId 3 = Empresarial
        return tenant?.PlanSuscripcionId == 3;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.IsPremium = await IsPremiumAsync();
        
        // Basic stats for the dashboard
        var cuentas = await _db.CuentasContables.CountAsync();
        var asientos = await _db.AsientosContables.CountAsync();
        
        ViewBag.TotalCuentas = cuentas;
        ViewBag.TotalAsientos = asientos;

        // Clients list for Certificates modal
        ViewBag.Terceros = await _db.Clientes
            .OrderBy(c => c.NombreCompleto)
            .Select(c => new { c.Id, c.NombreCompleto })
            .ToListAsync();
        
        return View();
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> LibroAuxiliarExcel(Guid? terceroId, DateTime? desde, DateTime? hasta)
    {
        if (!await IsPremiumAsync()) return Forbid();

        try
        {
            var excel = await _reporteService.ExportarLibroAuxiliarExcelAsync(terceroId, desde, hasta);
            Response.Headers["Content-Disposition"] = "inline";
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Libro Auxiliar: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> BalancePruebaExcel(DateTime? desde, DateTime? hasta)
    {
        if (!await IsPremiumAsync()) return Forbid();

        try
        {
            var excel = await _reporteService.ExportarBalancePruebaExcelAsync(desde, hasta);
            Response.Headers["Content-Disposition"] = "inline";
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Balance de Prueba: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CertificadoRetencionesPdf(Guid terceroId, int anio)
    {
        if (!await IsPremiumAsync()) return Forbid();

        try
        {
            var pdf = await _reporteService.GenerarCertificadoRetencionesPdfAsync(terceroId, anio);
            Response.Headers["Content-Disposition"] = "inline";
            return File(pdf, "application/pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Certificado: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}
