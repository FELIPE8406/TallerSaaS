using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.Services;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class TrazabilidadController : Controller
{
    private readonly TrazabilidadService _trazabilidadService;
    private readonly VehiculoService _vehiculoService;

    public TrazabilidadController(
        TrazabilidadService trazabilidadService,
        VehiculoService vehiculoService)
    {
        _trazabilidadService = trazabilidadService;
        _vehiculoService     = vehiculoService;
    }

    public async Task<IActionResult> Timeline(Guid vehiculoId)
    {
        var timeline = await _trazabilidadService.GetTimelineByVehiculoAsync(vehiculoId);
        if (timeline == null) return NotFound();
        return View(timeline);
    }
}
