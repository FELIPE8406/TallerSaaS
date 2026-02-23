namespace TallerSaaS.Domain.Entities;

public class Vehiculo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string? Placa { get; set; }
    public string? VIN { get; set; }
    public string? Color { get; set; }
    public string? Kilometraje { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public ICollection<Orden> Ordenes { get; set; } = new List<Orden>();

    public string Descripcion => $"{Anio} {Marca} {Modelo}";
}
