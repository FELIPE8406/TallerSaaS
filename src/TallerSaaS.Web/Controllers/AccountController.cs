using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TallerSaaS.Infrastructure.Data;

namespace TallerSaaS.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signInManager = signIn;
        _userManager   = users;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password,
                                           bool rememberMe = false, string? returnUrl = null)
    {
        // Usamos IgnoreQueryFilters para evitar problemas de timeout/filtros durante el login 
        // y cargamos explícitamente el Tenant para verificar el plan.
        var user = await _userManager.Users
            .Include(u => u.Tenant)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !user.Activo)
        {
            TempData["Error"] = "Credenciales inválidas o cuenta inactiva.";
            return View();
        }

        // TenantClaimsFactory (registered in Program.cs) automatically injects
        // the TenantId claim from user.TenantId on every successful sign-in.
        // No manual AddClaimsAsync is required here.
        var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe,
                                                              lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (user.EsSuperAdmin) return RedirectToAction("Index", "SuperAdmin");

            // Si no tiene plan activo, enviarlo a elegir uno
            if (user.TenantId == null || user.Tenant?.PlanSuscripcionId == null)
            {
                return RedirectToAction("Index", "Subscription");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        TempData["Error"] = result.IsLockedOut
            ? "Cuenta bloqueada temporalmente."
            : "Credenciales incorrectas.";
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Also clear impersonation session on logout
        HttpContext.Session.Remove("ImpersonatedTenantId");
        HttpContext.Session.Remove("ImpersonatedTenantNombre");
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");
        if (remoteError != null)
        {
            TempData["Error"] = $"Error del proveedor externo: {remoteError}";
            return RedirectToAction("Login", new { returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["Error"] = "Error al obtener información del login externo.";
            return RedirectToAction("Login", new { returnUrl });
        }

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            var user = await _userManager.Users
                .Include(u => u.Tenant)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == info.Principal.FindFirstValue(ClaimTypes.Email));
            
            if (user != null && !user.EsSuperAdmin && (user.TenantId == null || user.Tenant?.PlanSuscripcionId == null))
            {
                return RedirectToAction("Index", "Subscription");
            }
            return LocalRedirect(returnUrl);
        }

        // Si el usuario no tiene cuenta, podrías crear una automáticamente o pedir registro.
        // Aquí intentamos buscar por email para vincular cuentas existentes.
        var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
        if (email != null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
        }

        TempData["Error"] = "Usuario no registrado en el sistema.";
        return RedirectToAction("Login", new { returnUrl });
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
