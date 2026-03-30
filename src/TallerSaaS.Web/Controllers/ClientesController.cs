using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class ClientesController : Controller
{
    private readonly ClienteService _clienteService;
    private readonly ICurrentTenantService _tenantService;

    public ClientesController(ClienteService clienteSvc, ICurrentTenantService tenantService)
    {
        _clienteService = clienteSvc;
        _tenantService = tenantService;
    }

    public async Task<IActionResult> Index(string? buscar, int pagina = 1)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var clientes = await _clienteService.GetAllAsync(buscar);
        ViewBag.Buscar = buscar;
        ViewBag.Pagina = pagina;
        ViewBag.Total = clientes.Count;
        var paginado = TallerSaaS.Shared.Helpers.PaginacionHelper.Paginar(clientes, pagina, 10);
        return View(paginado);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarJson(string q)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(new List<object>());

        var clientes = await _clienteService.GetAllAsync(q);
        return Json(clientes.Select(c => new {
            id = c.Id,
            text = $"{c.NombreCompleto} {(c.Cedula != null ? "— " + c.Cedula : "")}"
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllJson()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var clientes = await _clienteService.GetAllAsync("");
        return Json(clientes.Take(20).Select(c => new {
            id = c.Id,
            text = $"{c.NombreCompleto} {(c.Cedula != null ? "— " + c.Cedula : "")}"
        }));
    }

    public IActionResult Crear()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        return View(new ClienteDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ClienteDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        if (!_tenantService.TenantId.HasValue) return Forbid();
        await _clienteService.CreateAsync(dto, _tenantService.TenantId.Value);
        TempData["Exito"] = "Cliente creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var cliente = await _clienteService.GetByIdAsync(id);
        if (cliente == null) return NotFound();
        if (cliente.TenantId != _tenantService.TenantId.Value) return Forbid();
        return View(cliente);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(ClienteDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var existing = await _clienteService.GetByIdAsync(dto.Id);
        if (existing == null) return NotFound();
        if (existing.TenantId != _tenantService.TenantId.Value) return Forbid();
        await _clienteService.UpdateAsync(dto);
        TempData["Exito"] = "Cliente actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var cliente = await _clienteService.GetByIdAsync(id);
        if (cliente == null) return NotFound();
        if (cliente.TenantId != _tenantService.TenantId.Value) return Forbid();
        await _clienteService.DeleteAsync(id);
        TempData["Exito"] = "Cliente desactivado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var cliente = await _clienteService.GetByIdAsync(id);
        if (cliente == null) return NotFound();
        if (cliente.TenantId != _tenantService.TenantId.Value) return Forbid();
        await _clienteService.ToggleStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
