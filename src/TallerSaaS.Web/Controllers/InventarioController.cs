using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class InventarioController : Controller
{
    private readonly InventarioService _inventarioService;
    private readonly ICurrentTenantService _tenantService;

    public InventarioController(InventarioService inventarioSvc, ICurrentTenantService tenantService)
    {
        _inventarioService = inventarioSvc;
        _tenantService = tenantService;
    }

    public async Task<IActionResult> Index(string? buscar, string? categoria)
    {
        var productos = await _inventarioService.GetAllAsync(buscar, categoria);
        var categorias = await _inventarioService.GetCategoriasAsync();
        ViewBag.Buscar = buscar;
        ViewBag.CategoriaFiltro = categoria;
        ViewBag.Categorias = categorias;
        ViewBag.AlertasBajoStock = productos.Count(p => p.NivelStock != "OK");
        return View(productos);
    }

    public IActionResult Crear() => View(new InventarioDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(InventarioDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        if (!_tenantService.TenantId.HasValue) return Forbid();
        await _inventarioService.CreateAsync(dto, _tenantService.TenantId.Value);
        TempData["Exito"] = "Producto agregado al inventario.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(Guid id)
    {
        var producto = await _inventarioService.GetByIdAsync(id);
        if (producto == null) return NotFound();
        return View(producto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(InventarioDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _inventarioService.UpdateAsync(dto);
        TempData["Exito"] = "Producto actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AjustarStock(Guid id, int cantidad, string tipo)
    {
        await _inventarioService.AjustarStockAsync(id, cantidad, tipo);
        TempData["Exito"] = $"Stock {(tipo == "entrada" ? "aumentado" : "reducido")} correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
