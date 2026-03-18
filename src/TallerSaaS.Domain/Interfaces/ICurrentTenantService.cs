namespace TallerSaaS.Domain.Interfaces;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    int? PlanId { get; }
    void SetTenant(Guid tenantId, int? planId = null);
}
