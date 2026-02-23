using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Shared.Helpers;

namespace TallerSaaS.Infrastructure.Middleware;

/// <summary>
/// Reads the TenantId claim from the authenticated user and populates
/// the scoped ICurrentTenantService so that DbContext Global Query Filters
/// can isolate data automatically for every request.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst(TenantClaimTypes.TenantId)?.Value;

            if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
            {
                tenantService.SetTenant(tenantId);
            }
        }

        await _next(context);
    }
}
