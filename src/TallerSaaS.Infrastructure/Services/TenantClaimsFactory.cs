using Microsoft.AspNetCore.Identity;
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

    public TenantClaimsFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
        _userManager = userManager;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        // Let the base factory add the standard claims (nameidentifier, username, roles, etc.)
        var identity = await base.GenerateClaimsAsync(user);

        // Inject TenantId claim directly from the user entity (no DB round-trip needed)
        if (user.TenantId.HasValue)
        {
            identity.AddClaim(new Claim(TenantClaimTypes.TenantId, user.TenantId.Value.ToString()));
        }

        // Optional: mark SuperAdmins so views can distinguish them
        if (user.EsSuperAdmin)
        {
            identity.AddClaim(new Claim("EsSuperAdmin", "true"));
        }

        return identity;
    }
}
