using Microsoft.AspNetCore.Identity;

namespace TallerSaaS.Infrastructure.Data;

public class ApplicationUser : IdentityUser
{
    public Guid? TenantId { get; set; }
    public TallerSaaS.Domain.Entities.Tenant? Tenant { get; set; }
    public string? NombreCompleto { get; set; }
    public bool EsSuperAdmin { get; set; } = false;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;

    public TallerSaaS.Domain.Entities.EmpleadoContrato? EmpleadoContrato { get; set; }
}
