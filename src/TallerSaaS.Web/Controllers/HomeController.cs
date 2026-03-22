using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Infrastructure.Data;
using TallerSaaS.Web.Models;

namespace TallerSaaS.Web.Controllers;

/// <summary>
/// Home / root route controller.
/// The [Authorize] attribute ensures that every route here requires authentication.
/// Unauthenticated requests are automatically redirected to /Account/Login by the
/// cookie auth middleware (configured in Program.cs).
/// </summary>
[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // "/" — redirect authenticated users straight to their dashboard
    public async Task<IActionResult> Index([FromServices] UserManager<ApplicationUser> userManager)
    {
        if (User.IsInRole("SuperAdmin"))
            return RedirectToAction("Index", "SuperAdmin");

        var user = await userManager.Users.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
        if (user != null && (user.TenantId == null || user.Tenant?.PlanSuscripcionId == null))
        {
            return RedirectToAction("Index", "Subscription");
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
