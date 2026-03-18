namespace TallerSaaS.Domain.Entities;

public class Pago
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Estado { get; set; } = "Pendiente"; // Pendiente, Completado, Fallido
    public string? Referencia { get; set; }
    public string? Concepto { get; set; }
    public int? PlanSuscripcionId { get; set; }
    public PlanSuscripcion? PlanSuscripcion { get; set; }
}
