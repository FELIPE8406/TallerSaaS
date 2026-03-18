namespace TallerSaaS.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string? NIT { get; set; }
    public string? Ciudad { get; set; }
    public string? RFC { get; set; }
    public string? Logo { get; set; }  // filename stored in wwwroot/logos/
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Email { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    public int? PlanSuscripcionId { get; set; }
    public PlanSuscripcion? PlanSuscripcion { get; set; }
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
