using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public IActionResult Index()
    {
        if (User.IsInRole("SuperAdmin"))
            return RedirectToAction("Index", "SuperAdmin");

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
