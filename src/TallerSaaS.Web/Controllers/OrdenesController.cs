using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico")]
public class OrdenesController : Controller
{
    private readonly OrdenService _ordenService;
    private readonly ClienteService _clienteService;
    private readonly VehiculoService _vehiculoService;
    private readonly ICurrentTenantService _tenantService;

    public OrdenesController(OrdenService ordenSvc,
                             ClienteService clienteSvc,
                             VehiculoService vehiculoSvc,
                             ICurrentTenantService tenantService)
    {
        _ordenService    = ordenSvc;
        _clienteService  = clienteSvc;
        _vehiculoService = vehiculoSvc;
        _tenantService   = tenantService;
    }

    public async Task<IActionResult> Index(int? estado)
    {
        var ordenes = await _ordenService.GetAllAsync(estado.HasValue ? (EstadoOrden)estado.Value : null);
        ViewBag.EstadoFiltro = estado;
        return View(ordenes);
    }

    public async Task<IActionResult> Detalle(Guid id)
    {
        var orden = await _ordenService.GetByIdAsync(id);
        if (orden == null) return NotFound();
        return View(orden);
    }

    public async Task<IActionResult> Crear()
    {
        ViewBag.Clientes  = await _clienteService.GetAllAsync();
        ViewBag.Vehiculos = await _vehiculoService.GetAllAsync();
        return View(new OrdenDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(OrdenDto dto)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var orden = await _ordenService.CreateAsync(dto, _tenantService.TenantId.Value);
        TempData["Exito"] = $"Orden {orden.NumeroOrden} creada.";
        return RedirectToAction(nameof(Detalle), new { id = orden.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(Guid id, int estado)
    {
        await _ordenService.CambiarEstadoAsync(id, (EstadoOrden)estado);
        TempData["Exito"] = "Estado actualizado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarItem(Guid ordenId, ItemOrdenDto dto)
    {
        await _ordenService.AddItemAsync(ordenId, dto);
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarItem(Guid ordenId, Guid itemId)
    {
        await _ordenService.RemoveItemAsync(ordenId, itemId);
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    public async Task<IActionResult> DescargarPdf(Guid id)
    {
        // Will be handled by ReportesController
        return RedirectToAction("FacturaPdf", "Reportes", new { ordenId = id });
    }
}
