using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Infrastructure.Data;

public class CurrentTenantService : ICurrentTenantService
{
    private Guid? _tenantId;

    public Guid? TenantId => _tenantId;

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}
