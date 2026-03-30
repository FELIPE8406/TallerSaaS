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

    public IActionResult Index()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> BuscarJson(string q, Guid? clienteId = null)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var vehiculos = await _vehiculoService.GetAllAsync(clienteId, q);
        return Json(vehiculos.Select(v => new {
            id = v.Id,
            text = $"{v.Marca} {v.Modelo} ({v.Placa ?? "SIN PLACA"})"
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(int page = 1, int size = 20, Guid? clienteId = null)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var result = await _vehiculoService.GetAllPagedAsync(page, size, clienteId);
        return Json(result);
    }

    public async Task<IActionResult> Crear(Guid? clienteId)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
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

    public async Task<IActionResult> Editar(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var vehiculo = await _vehiculoService.GetByIdAsync(id);
        if (vehiculo == null) return NotFound();
        if (vehiculo.TenantId != _tenantService.TenantId.Value) return Forbid();
        await PopularClientes();
        return View(vehiculo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(VehiculoDto dto)
    {
        if (!ModelState.IsValid) { await PopularClientes(); return View(dto); }
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var existing = await _vehiculoService.GetByIdAsync(dto.Id);
        if (existing == null) return NotFound();
        if (existing.TenantId != _tenantService.TenantId.Value) return Forbid();
        await _vehiculoService.UpdateAsync(dto);
        TempData["Exito"] = "Vehículo actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var vehiculo = await _vehiculoService.GetByIdAsync(id);
        if (vehiculo == null) return NotFound();
        if (vehiculo.TenantId != _tenantService.TenantId.Value) return Forbid();
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

    private async Task PopularClientes()
    {
        ViewBag.Clientes = await _clienteService.GetTopAsync(20);
    }
}
