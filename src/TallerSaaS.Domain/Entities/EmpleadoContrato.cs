namespace TallerSaaS.Domain.Entities;

public class EmpleadoContrato
{
    public Guid Id { get; set; }
    
    // Foreign Key to IdentityUser (ApplicationUser string Id)
    public string UserId { get; set; } = null!;

    public Guid TenantId { get; set; }

    public decimal SalarioBase { get; set; }
    
    public decimal PorcentajeComision { get; set; }

    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;

    // e.g. "Mecanico", "Admin"
    public string TipoEmpleado { get; set; } = "Mecanico";

    public string? URLContratoPDF { get; set; }
}
