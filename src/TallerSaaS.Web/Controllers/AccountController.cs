using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Infrastructure.Data;
using TallerSaaS.Shared.Helpers;
using System.Security.Claims;

namespace TallerSaaS.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signInManager = signIn;
        _userManager = users;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.Activo)
        {
            TempData["Error"] = "Credenciales inválidas o cuenta inactiva.";
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            // Add tenant claim to the session
            if (user.TenantId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new(TenantClaimTypes.TenantId, user.TenantId.Value.ToString())
                };
                await _userManager.AddClaimsAsync(user, claims.Where(c =>
                    !_userManager.GetClaimsAsync(user).Result.Any(x => x.Type == c.Type)));
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.EsSuperAdmin)
                return RedirectToAction("Index", "SuperAdmin");

            return RedirectToAction("Index", "Dashboard");
        }

        TempData["Error"] = result.IsLockedOut ? "Cuenta bloqueada temporalmente." : "Credenciales incorrectas.";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
