using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class TrazabilidadController : Controller
{
    private readonly TrazabilidadService _trazabilidadService;
    private readonly VehiculoService _vehiculoService;
    private readonly ICurrentTenantService _tenantService;

    public TrazabilidadController(
        TrazabilidadService trazabilidadService,
        VehiculoService vehiculoService,
        ICurrentTenantService tenantService)
    {
        _trazabilidadService = trazabilidadService;
        _vehiculoService     = vehiculoService;
        _tenantService       = tenantService;
    }

    public async Task<IActionResult> Timeline(Guid vehiculoId)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();

        // Verify ownership before delegating to service
        var vehiculo = await _vehiculoService.GetByIdAsync(vehiculoId);
        if (vehiculo == null) return NotFound();
        if (vehiculo.TenantId != _tenantService.TenantId.Value) return Forbid();

        var timeline = await _trazabilidadService.GetTimelineByVehiculoAsync(vehiculoId);
        if (timeline == null) return NotFound();
        return View(timeline);
    }
}
