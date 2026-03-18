using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        var user = await _userManager.FindByEmailAsync(email);
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
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.EsSuperAdmin
                ? RedirectToAction("Index", "SuperAdmin")
                : RedirectToAction("Index", "Dashboard");
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

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
