using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Infrastructure.Data;

public class CurrentTenantService : ICurrentTenantService
{
    private Guid? _tenantId;
    private int? _planId;
    private string? _tenantNombre;

    public Guid? TenantId => _tenantId;
    public int? PlanId => _planId;
    public string? TenantNombre => _tenantNombre;

    public void SetTenant(Guid tenantId, int? planId = null, string? tenantNombre = null)
    {
        _tenantId = tenantId;
        _planId = planId;
        _tenantNombre = tenantNombre;
    }
}
