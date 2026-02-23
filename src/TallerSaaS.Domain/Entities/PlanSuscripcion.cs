namespace TallerSaaS.Domain.Entities;

public class PlanSuscripcion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int LimiteUsuarios { get; set; } = 5;
    public decimal Precio { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}
