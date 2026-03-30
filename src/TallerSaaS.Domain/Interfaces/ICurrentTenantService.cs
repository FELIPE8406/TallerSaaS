namespace TallerSaaS.Domain.Interfaces;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    int? PlanId { get; }
    string? TenantNombre { get; }
    void SetTenant(Guid tenantId, int? planId = null, string? tenantNombre = null);
}
