using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TallerSaaS.Infrastructure.Data;
using TallerSaaS.Shared.Helpers;
using System.Security.Claims;

namespace TallerSaaS.Infrastructure.Services;

/// <summary>
/// Extends the default ClaimsPrincipalFactory to automatically inject
/// TenantId and TenantNombre claims from ApplicationUser into the cookie
/// at sign-in time — no manual AddClaimsAsync is needed anywhere.
/// </summary>
public class TenantClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public TenantClaimsFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        ApplicationDbContext db)
        : base(userManager, roleManager, optionsAccessor)
    {
        _userManager = userManager;
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        // Let the base factory add the standard claims (nameidentifier, username, roles, etc.)
        var identity = await base.GenerateClaimsAsync(user);

        // Inject TenantId claim directly from the user entity
        if (user.TenantId.HasValue)
        {
            identity.AddClaim(new Claim(TenantClaimTypes.TenantId, user.TenantId.Value.ToString()));
            
            // NEW: Add PlanId to claims to allow UI gating without DB hits on layout
            var tenant = await _db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == user.TenantId.Value);
            if (tenant != null)
            {
                identity.AddClaim(new Claim("TenantPlanId", tenant.PlanSuscripcionId.ToString() ?? ""));
            }
        }

        // Optional: mark SuperAdmins so views can distinguish them
        if (user.EsSuperAdmin)
        {
            identity.AddClaim(new Claim("EsSuperAdmin", "true"));
        }

        return identity;
    }
}
