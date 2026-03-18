using System.ComponentModel.DataAnnotations;

namespace TallerSaaS.Web.Models;

public class PlanViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0, 10000000, ErrorMessage = "Precio inválido")]
    public decimal Precio { get; set; }

    [Required(ErrorMessage = "El límite de mecánicos es obligatorio")]
    public int LimiteUsuarios { get; set; } = 5;

    public string? Descripcion { get; set; }

    public string? Beneficios { get; set; }

    public string? ColorHex { get; set; } = "#0066CC";
}
