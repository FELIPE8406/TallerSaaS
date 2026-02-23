using System.ComponentModel.DataAnnotations;

namespace TallerSaaS.Web.Models;

/// <summary>
/// Strongly-typed ViewModel for the NuevoTenant form.
/// Captures taller data + first admin credentials in one POST.
/// </summary>
public class NuevoTenantViewModel
{
    // ── Datos del Taller ──────────────────────────────────────────────────
    [Required(ErrorMessage = "El nombre del taller es obligatorio.")]
    [StringLength(200)]
    [Display(Name = "Nombre del Taller")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "NIT / Cédula")]
    public string? RFC { get; set; }   // maps to Tenant.RFC

    [Required(ErrorMessage = "El email de contacto es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(150)]
    [Display(Name = "Email de Contacto del Taller")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(300)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Required(ErrorMessage = "Selecciona un plan.")]
    [Display(Name = "Plan de Suscripción")]
    public int PlanSuscripcionId { get; set; }

    // ── Credenciales del Administrador Inicial ────────────────────────────
    [Required(ErrorMessage = "El correo del administrador es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(150)]
    [Display(Name = "Email del Administrador")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña Inicial")]
    public string AdminPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare(nameof(AdminPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string AdminPasswordConfirm { get; set; } = string.Empty;
}
