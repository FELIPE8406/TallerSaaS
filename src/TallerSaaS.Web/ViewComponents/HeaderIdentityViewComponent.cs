using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TallerSaaS.Infrastructure.Data;

namespace TallerSaaS.Web.ViewComponents;

public class HeaderIdentityViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public HeaderIdentityViewComponent(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!UserClaimsPrincipal.Identity?.IsAuthenticated ?? false) return Content(string.Empty);

        if (UserClaimsPrincipal.IsInRole("SuperAdmin"))
        {
            return View("SuperAdmin");
        }

        // Check if impersonating (Support Mode)
        var impersonatedIdStr = HttpContext.Session.GetString("ImpersonatedTenantId");
        Guid? tenantId = null;
        
        if (!string.IsNullOrEmpty(impersonatedIdStr))
        {
            if (Guid.TryParse(impersonatedIdStr, out var sid)) tenantId = sid;
        }
        else
        {
            var claim = UserClaimsPrincipal.Claims.FirstOrDefault(c => c.Type == "TenantId")?.Value;
            if (Guid.TryParse(claim, out var tid)) tenantId = tid;
        }

        if (tenantId.HasValue)
        {
            var tenant = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant != null)
            {
                return View("Workshop", tenant);
            }
        }

        return View("Default");
    }
}
