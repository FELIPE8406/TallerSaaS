using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Infrastructure.Data;
using TallerSaaS.Shared.Helpers;

namespace TallerSaaS.Infrastructure.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
                                  ICurrentTenantService tenantService,
                                  UserManager<ApplicationUser> userManager,
                                  ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var isSuperAdmin = context.User.IsInRole("SuperAdmin");

            var sessionTenant = context.Session.GetString("ImpersonatedTenantId");
            if (!string.IsNullOrEmpty(sessionTenant) && Guid.TryParse(sessionTenant, out var sessionId))
            {
                Tenant? sessionTenantEntity = null;
                try
                {
                    sessionTenantEntity = await db.Tenants.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == sessionId);
                }
                catch { }

                if (sessionTenantEntity is null)
                {
                    context.Session.Remove("ImpersonatedTenantId");
                    context.Session.Remove("ImpersonatedTenantNombre");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Tenant de impersonación no encontrado.");
                    return;
                }

                tenantService.SetTenant(sessionId, sessionTenantEntity.PlanSuscripcionId, sessionTenantEntity.Nombre);
            }
            else
            {
                var tenantClaim = context.User.FindFirst(TenantClaimTypes.TenantId)?.Value;
                var nombreClaim = context.User.FindFirst(TenantClaimTypes.TenantNombre)?.Value;
                var planClaim   = context.User.FindFirst("TenantPlanId")?.Value;

                if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var claimId))
                {
                    int? planId = int.TryParse(planClaim, out var pId) ? pId : null;

                    if (planId == null && !isSuperAdmin)
                    {
                        try
                        {
                            var tenant = await db.Tenants.AsNoTracking()
                                .FirstOrDefaultAsync(t => t.Id == claimId);
                            planId = tenant?.PlanSuscripcionId;
                        }
                        catch { }
                    }

                    tenantService.SetTenant(claimId, planId, nombreClaim);
                }
                else if (!isSuperAdmin)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Tenant no identificado.");
                    return;
                }
            }
        }

        await _next(context);
    }
}
