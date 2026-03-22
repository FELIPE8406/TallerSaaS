using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Infrastructure.Data;

namespace TallerSaaS.Infrastructure.Services;

public class UserProvider : IUserProvider
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserProvider(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.NombreCompleto;
    }

    public async Task<IEnumerable<(string Id, string Name)>> GetMechanicsAsync(Guid tenantId)
    {
        // First get all active users for the tenant
        var users = await _userManager.Users
            .Where(u => u.TenantId == tenantId && u.Activo)
            .ToListAsync();
        
        var mechanics = new List<(string Id, string Name)>();
        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, "Mecanico"))
            {
                mechanics.Add((user.Id, user.NombreCompleto ?? user.UserName ?? "Sin Nombre"));
            }
        }
        return mechanics;
    }

    public async Task<IEnumerable<(string Id, string Name, string Role)>> GetStaffAsync(Guid tenantId)
    {
        var users = await _userManager.Users
            .Where(u => u.TenantId == tenantId && u.Activo)
            .ToListAsync();

        var staff = new List<(string Id, string Name, string Role)>();
        foreach (var user in users)
        {
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Usuario";
            staff.Add((user.Id, user.NombreCompleto ?? user.UserName ?? "Sin Nombre", role));
        }
        return staff;
    }
}
