using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Infrastructure.Data;

public class CurrentTenantService : ICurrentTenantService
{
    private Guid? _tenantId;
    private int? _planId;

    public Guid? TenantId => _tenantId;
    public int? PlanId => _planId;

    public void SetTenant(Guid tenantId, int? planId = null)
    {
        _tenantId = tenantId;
        _planId = planId;
    }
}
