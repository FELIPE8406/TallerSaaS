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
    public async Task<IActionResult> Nueva(List<Guid> ordenIds, string tipoFacturacion = "NoElectronica")
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        if (!ordenIds.Any())
        {
            TempData["Error"] = "Debe seleccionar al menos una orden.";
            return RedirectToAction(nameof(Nueva));
        }

        // Convertir el valor del formulario al enum de dominio
        var tipo = tipoFacturacion == "Electronica"
            ? Domain.Enums.TipoFacturacion.Electronica
            : Domain.Enums.TipoFacturacion.NoElectronica;

        try
        {
            var factura = await _facturaService.GenerarFacturaAsync(
                ordenIds, _tenantService.TenantId.Value, tipo);

            if (tipo == Domain.Enums.TipoFacturacion.Electronica)
            {
                // Fase 1 (transicional): la factura electrónica queda registrada con
                // EstadoEnvio = PendienteEnvio. El ciclo de entrega YA se cierra aquí
                // (orden.Pagada = true, Estado = EntregadoYFacturado).
                // El botón "Enviar a DIAN" en el Detalle es el placeholder para Fase 2.
                TempData["Exito"] = $"Factura {factura.NumeroFactura} registrada como " +
                                    "<strong>Factura Electrónica — Pendiente de Envío a la DIAN</strong>. " +
                                    "La orden fue marcada como entregada. Use el botón \"Enviar a DIAN\" cuando la integración esté activa.";
                return RedirectToAction(nameof(Detalle), new { id = factura.Id });
            }

            // Factura interna: flujo normal.
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

    // ── Placeholder: Enviar a DIAN (Fase 1 → Fase 2) ────────────────────────
    /// <summary>
    /// Fase 1 (transicional): no realiza ninguna llamada a la API DIAN.
    /// Registra la intención en TempData y redirige al Dashboard sin errores.
    /// En Fase 2, este método invocará el servicio de integración DIAN y
    /// actualizará factura.EstadoEnvio = Enviada.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarADian(Guid id)
    {
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura == null) return NotFound();

        // TODO Fase 2: invocar IDianService.EnviarAsync(id) y persistir EstadoEnvio = Enviada
        TempData["Info"] = $"Factura <strong>{factura.NumeroFactura}</strong>: la integración con la DIAN " +
                           "está <strong>en construcción</strong>. Cuando esté activa, este botón " +
                           "enviará el documento electrónico a la DIAN automáticamente.";
        return RedirectToAction("Index", "Dashboard");
    }
}
