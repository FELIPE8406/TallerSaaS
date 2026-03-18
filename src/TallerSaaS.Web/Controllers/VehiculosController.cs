using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class VehiculosController : Controller
{
    private readonly VehiculoService _vehiculoService;
    private readonly ClienteService  _clienteService;
    private readonly ICurrentTenantService _tenantService;

    public VehiculosController(VehiculoService vehiculoSvc,
                               ClienteService  clienteSvc,
                               ICurrentTenantService tenantService)
    {
        _vehiculoService = vehiculoSvc;
        _clienteService  = clienteSvc;
        _tenantService   = tenantService;
    }

    // ── Index ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewBag.Clientes = await _clienteService.GetAllAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(int page = 1, int size = 10, Guid? clienteId = null)
    {
        var result = await _vehiculoService.GetAllPagedAsync(page, size, clienteId);
        return Json(result);
    }

    // ── Crear ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Crear(Guid? clienteId)
    {
        await PopularClientes();
        return View(new VehiculoDto { ClienteId = clienteId ?? Guid.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(VehiculoDto dto)
    {
        if (!ModelState.IsValid) { await PopularClientes(); return View(dto); }
        if (!_tenantService.TenantId.HasValue) return Forbid();
        await _vehiculoService.CreateAsync(dto, _tenantService.TenantId.Value);
        TempData["Exito"] = $"Vehículo {dto.Marca} {dto.Modelo} registrado.";
        return RedirectToAction(nameof(Index));
    }

    // ── Editar ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Editar(Guid id)
    {
        var vehiculo = await _vehiculoService.GetByIdAsync(id);
        if (vehiculo == null) return NotFound();
        await PopularClientes();
        return View(vehiculo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(VehiculoDto dto)
    {
        if (!ModelState.IsValid) { await PopularClientes(); return View(dto); }
        await _vehiculoService.UpdateAsync(dto);
        TempData["Exito"] = "Vehículo actualizado.";
        return RedirectToAction(nameof(Index));
    }

    // ── Eliminar ──────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        try
        {
            await _vehiculoService.DeleteAsync(id);
            TempData["Exito"] = "Vehículo eliminado.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // ── Private ───────────────────────────────────────────────────────────────
    private async Task PopularClientes()
    {
        ViewBag.Clientes = await _clienteService.GetAllAsync();
    }
}
