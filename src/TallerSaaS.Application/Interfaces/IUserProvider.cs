namespace TallerSaaS.Application.Interfaces;

public interface IUserProvider
{
    Task<string?> GetUserNameAsync(string userId);
    Task<IEnumerable<(string Id, string Name)>> GetMechanicsAsync(Guid tenantId);
    Task<IEnumerable<(string Id, string Name, string Role)>> GetStaffAsync(Guid tenantId);
}
