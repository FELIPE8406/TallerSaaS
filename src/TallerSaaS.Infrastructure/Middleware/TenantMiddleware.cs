using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
                                  UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // ── 1. SuperAdmin Acceso de Soporte (session override) ──────────────
            var sessionTenant = context.Session.GetString("ImpersonatedTenantId");
            if (!string.IsNullOrEmpty(sessionTenant) && Guid.TryParse(sessionTenant, out var sessionId))
            {
                tenantService.SetTenant(sessionId);
            }
            else
            {
                // ── 2. Standard TenantId claim (injected by TenantClaimsFactory) ──
                var tenantClaim = context.User.FindFirst(TenantClaimTypes.TenantId)?.Value;
                if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var claimId))
                {
                    tenantService.SetTenant(claimId);
                }
                else
                {
                    // ── 3. DB fallback — for users whose cookie predates the factory ──
                    // Only invoke if the user is NOT a SuperAdmin (they have no TenantId)
                    if (!context.User.IsInRole("SuperAdmin"))
                    {
                        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            var user = await userManager.FindByIdAsync(userId);
                            if (user?.TenantId.HasValue == true)
                            {
                                tenantService.SetTenant(user.TenantId.Value);
                            }
                        }
                    }
                }
            }
        }

        await _next(context);
    }
}
