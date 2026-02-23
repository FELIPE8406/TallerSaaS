using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Infrastructure.Data;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico")]
public class DashboardController : Controller
{
    private readonly DashboardService _dashboardService;
    private readonly ICurrentTenantService _tenantService;
    private readonly ApplicationDbContext _db;

    public DashboardController(DashboardService dashboardSvc,
                               ICurrentTenantService tenantService,
                               ApplicationDbContext db)
    {
        _dashboardService = dashboardSvc;
        _tenantService    = tenantService;
        _db               = db;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId  = _tenantService.TenantId;
        var ahora     = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

        // ── KPI 1: Ventas del mes en COP (órdenes pagadas del mes corriente) ────
        var ventasMesCOP = await _db.Ordenes
            .Where(o => (!tenantId.HasValue || o.TenantId == tenantId) &&
                        o.Pagada &&
                        o.FechaEntrada >= inicioMes)
            .SumAsync(o => (decimal?)o.Total) ?? 0m;

        // ── KPI 2: Órdenes Activas (no entregadas) ────────────────────────────
        var ordenesActivas = await _db.Ordenes
            .CountAsync(o => (!tenantId.HasValue || o.TenantId == tenantId) &&
                             o.Estado != EstadoOrden.Entregado);

        // ── KPI 3: Vehículos con orden en estado "Terminado" (para entrega) ────
        var vehiculosParaEntrega = await _db.Ordenes
            .Where(o => (!tenantId.HasValue || o.TenantId == tenantId) &&
                        o.Estado == EstadoOrden.Terminado)
            .Select(o => o.VehiculoId)
            .Distinct()
            .CountAsync();

        // ── KPI 4: Total vehículos registrados ──────────────────────────────
        var totalVehiculos = await _db.Vehiculos
            .CountAsync(v => !tenantId.HasValue || v.TenantId == tenantId);

        // ── Chart: Flujo de Caja Semanal (últimas 8 semanas, COP) ────────────
        var hace8Semanas = ahora.AddDays(-56);
        var ordenesRecientes = await _db.Ordenes
            .Where(o => (!tenantId.HasValue || o.TenantId == tenantId) &&
                        o.Pagada &&
                        o.FechaEntrada >= hace8Semanas)
            .Select(o => new { o.FechaEntrada, o.Total })
            .ToListAsync();

        var flujoCajaLabels = new List<string>();
        var flujoCajaData   = new List<decimal>();

        for (int i = 7; i >= 0; i--)
        {
            var inicioSemana = ahora.AddDays(-i * 7).Date;
            var finSemana    = inicioSemana.AddDays(7);
            flujoCajaLabels.Add($"Sem {ahora.AddDays(-i * 7):dd/MM}");
            flujoCajaData.Add(ordenesRecientes
                .Where(o => o.FechaEntrada >= inicioSemana && o.FechaEntrada < finSemana)
                .Sum(o => o.Total));
        }

        // ── Chart: Servicios más Solicitados (top 6 items) ───────────────────
        var topServicios = await _db.ItemsOrden
            .Join(_db.Ordenes,
                  item  => item.OrdenId,
                  orden => orden.Id,
                  (item, orden) => new { item.Descripcion, orden.TenantId })
            .Where(x => !tenantId.HasValue || x.TenantId == tenantId)
            .GroupBy(x => x.Descripcion)
            .Select(g => new { Nombre = g.Key, Conteo = g.Count() })
            .OrderByDescending(x => x.Conteo)
            .Take(6)
            .ToListAsync();

        // ── Build full dashboard DTO ─────────────────────────────────────────
        var dashboard = await _dashboardService.GetDashboardAsync();

        // Enrich ViewBag with real KPI values
        ViewBag.VentasMesCOP          = ventasMesCOP;
        ViewBag.OrdenesActivas        = ordenesActivas;
        ViewBag.VehiculosParaEntrega  = vehiculosParaEntrega;
        ViewBag.TotalVehiculos        = totalVehiculos;
        ViewBag.FlujoCajaLabels       = flujoCajaLabels;
        ViewBag.FlujoCajaData         = flujoCajaData;
        ViewBag.TopServiciosLabels    = topServicios.Select(s => s.Nombre).ToList();
        ViewBag.TopServiciosData      = topServicios.Select(s => s.Conteo).ToList();

        return View(dashboard);
    }

    // ── JSON endpoint for Chart.js (used by existing partials if any) ────────
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
