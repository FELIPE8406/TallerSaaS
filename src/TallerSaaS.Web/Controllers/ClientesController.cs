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
        var clientes = await _clienteService.GetAllAsync(buscar);
        ViewBag.Buscar = buscar;
        ViewBag.Pagina = pagina;
        ViewBag.Total = clientes.Count;
        var paginado = TallerSaaS.Shared.Helpers.PaginacionHelper.Paginar(clientes, pagina, 10);
        return View(paginado);
    }

    public IActionResult Crear() => View(new ClienteDto());

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
        var cliente = await _clienteService.GetByIdAsync(id);
        if (cliente == null) return NotFound();
        return View(cliente);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(ClienteDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _clienteService.UpdateAsync(dto);
        TempData["Exito"] = "Cliente actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        await _clienteService.DeleteAsync(id);
        TempData["Exito"] = "Cliente desactivado.";
        return RedirectToAction(nameof(Index));
    }
}
