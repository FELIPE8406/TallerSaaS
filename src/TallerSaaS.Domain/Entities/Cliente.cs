namespace TallerSaaS.Domain.Entities;

public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Cedula { get; set; }  // CC / NIT colombiano
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}
