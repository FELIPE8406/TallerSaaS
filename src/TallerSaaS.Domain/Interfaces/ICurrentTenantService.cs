namespace TallerSaaS.Domain.Interfaces;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
}
