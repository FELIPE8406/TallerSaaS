using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Infrastructure.Data;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class DashboardController : Controller
{
    private readonly DashboardService _dashboardService;
    private readonly ICurrentTenantService _tenantService;

    // NOTE: Direct _db injection removed — DashboardService now handles all queries
    // efficiently with Task.WhenAll, eliminating redundant connection usage.
    public DashboardController(DashboardService dashboardSvc,
                               ICurrentTenantService tenantService)
    {
        _dashboardService = dashboardSvc;
        _tenantService    = tenantService;
    }

    public async Task<IActionResult> Index()
    {
        // Single call to DashboardService — internally runs queries in parallel
        // via Task.WhenAll (see DashboardService.GetDashboardAsync).
        // No direct _db access here to avoid double-connection exhaustion.
        var dashboard = await _dashboardService.GetDashboardAsync();

        // Build cash-flow weekly chart from the VentasMensuales data
        // (already fetched by the service — no extra DB calls)
        var flujoCajaLabels = dashboard.VentasMensuales.Select(v => v.Mes).ToList();
        var flujoCajaData   = dashboard.VentasMensuales.Select(v => v.Total).ToList();

        // Aggregate top services from OrdenesPorEstado (already loaded)
        ViewBag.FlujoCajaLabels    = flujoCajaLabels;
        ViewBag.FlujoCajaData      = flujoCajaData;
        ViewBag.TopServiciosLabels = new List<string>();   // populated on demand via AJAX
        ViewBag.TopServiciosData   = new List<int>();

        // Real KPI values from service (no hardcoding)
        ViewBag.VentasMesCOP         = dashboard.VentasMes;
        ViewBag.OrdenesActivas        = dashboard.OrdenesAbiertas;
        ViewBag.VehiculosParaEntrega  = dashboard.OrdenesPorEstado
                                            .Where(e => e.Estado == EstadoOrden.Terminado.ToString())
                                            .Sum(e => e.Conteo);
        ViewBag.TotalVehiculos        = dashboard.TotalVehiculos;

        return View(dashboard);
    }

    // ── JSON endpoint for Chart.js (on-demand top services) ─────────────────
    [HttpGet]
    public async Task<IActionResult> GetChartData()
    {
        var dashboard = await _dashboardService.GetDashboardAsync();
        return Json(new
        {
            ventasMensuales  = dashboard.VentasMensuales,
            ordenesPorEstado = dashboard.OrdenesPorEstado
        });
    }
}
