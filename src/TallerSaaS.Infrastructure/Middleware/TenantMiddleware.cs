using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Infrastructure.Data;
using TallerSaaS.Shared.Helpers;

namespace TallerSaaS.Infrastructure.Middleware;

/// <summary>
/// Resolves the active TenantId on every request using this precedence:
///   1. Session  — SuperAdmin "Acceso de Soporte" override
///   2. Claim    — Standard TenantId claim injected by TenantClaimsFactory
///   3. DB       — Fallback for users whose claim cookie predates the factory
///                 (existing accounts before factory registration).
/// </summary>
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
            // ── 1. SuperAdmin Acceso de Soporte (session override) ──────────────
            var sessionTenant = context.Session.GetString("ImpersonatedTenantId");
            if (!string.IsNullOrEmpty(sessionTenant) && Guid.TryParse(sessionTenant, out var sessionId))
            {
                // Load plan for session-based impersonation
                var tenant = await db.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == sessionId);

                tenantService.SetTenant(sessionId, tenant?.PlanSuscripcionId);
            }
            else
            {
                // ── 2. Standard TenantId claim ──
                var tenantClaim = context.User.FindFirst(TenantClaimTypes.TenantId)?.Value;
                var planClaim = context.User.FindFirst("TenantPlanId")?.Value;

                if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var claimId))
                {
                    int? planId = int.TryParse(planClaim, out var pId) ? pId : null;
                    
                    // Fallback for PlanId if not in claims (old cookie)
                    if (planId == null && !context.User.IsInRole("SuperAdmin"))
                    {
                        var tenant = await db.Tenants.AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == claimId);
                        planId = tenant?.PlanSuscripcionId;
                    }

                    tenantService.SetTenant(claimId, planId);
                }
                else
                {
                    // ── 3. DB fallback ──
                    if (!context.User.IsInRole("SuperAdmin"))
                    {
                        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            var user = await userManager.FindByIdAsync(userId);
                            if (user?.TenantId.HasValue == true)
                            {
                                var tenant = await db.Tenants.AsNoTracking()
                                    .FirstOrDefaultAsync(t => t.Id == user.TenantId.Value);
                                tenantService.SetTenant(user.TenantId.Value, tenant?.PlanSuscripcionId);
                            }
                        }
                    }
                }
            }
        }

        await _next(context);
    }
}
