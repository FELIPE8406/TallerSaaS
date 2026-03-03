using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Shared.Helpers;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class FacturasController : Controller
{
    private readonly FacturaService _facturaService;
    private readonly OrdenService _ordenService;
    private readonly ReporteService _reporteService;
    private readonly ICurrentTenantService _tenantService;

    public FacturasController(
        FacturaService facturaService,
        OrdenService ordenService,
        ReporteService reporteService,
        ICurrentTenantService tenantService)
    {
        _facturaService  = facturaService;
        _ordenService    = ordenService;
        _reporteService  = reporteService;
        _tenantService   = tenantService;
    }

    // ── Lista de Facturas ─────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var facturas = await _facturaService.GetAllAsync();
        return View(facturas);
    }

    // ── Nueva Factura — selección de órdenes ──────────────────────────────────
    public async Task<IActionResult> Nueva()
    {
        // Only show orders in "Terminado" state that are not blocked yet
        var ordenesElegibles = await _ordenService.GetAllAsync(Domain.Enums.EstadoOrden.Terminado);
        var noFacturadas = ordenesElegibles.Where(o => !o.Bloqueada).ToList();
        return View(noFacturadas);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Nueva(List<Guid> ordenIds)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        if (!ordenIds.Any())
        {
            TempData["Error"] = "Debe seleccionar al menos una orden.";
            return RedirectToAction(nameof(Nueva));
        }

        try
        {
            var factura = await _facturaService.GenerarFacturaAsync(ordenIds, _tenantService.TenantId.Value);
            TempData["Exito"] = $"Factura {factura.NumeroFactura} generada exitosamente.";
            return RedirectToAction(nameof(Detalle), new { id = factura.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Nueva));
        }
    }

    // ── Detalle de Factura ────────────────────────────────────────────────────
    public async Task<IActionResult> Detalle(Guid id)
    {
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura == null) return NotFound();
        return View(factura);
    }

    // ── Descarga PDF Apple-Style ──────────────────────────────────────────────
    public async Task<IActionResult> DescargarPdf(Guid id)
    {
        var tenantNombre = User.FindFirst(TenantClaimTypes.TenantNombre)?.Value ?? "Taller";
        var pdf = await _reporteService.GenerarFacturaPdfPorFacturaAsync(id, tenantNombre);
        return File(pdf, "application/pdf", $"Factura-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    // ── Descarga Excel Pro ────────────────────────────────────────────────────
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DescargarExcel()
    {
        var excel = await _reporteService.ExportarFacturasExcelAsync();
        return File(excel,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Facturas-{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
