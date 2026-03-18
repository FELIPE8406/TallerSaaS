namespace TallerSaaS.Application.DTOs;

/// <summary>
/// Filtro de periodos de tiempo para exportaciones y reportes.
/// Soporta periodos predefinidos (Trimestral, Semestral, Anual) y rangos personalizados.
/// Toda la lógica de fecha usa <see cref="TimeZoneHelper"/> (UTC-5 Colombia) como única fuente de verdad (DRY).
/// </summary>
public class ReporteFilter
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public string Periodo { get; set; } = "personalizado";

    // ── Factories con zona horaria correcta (UTC-5 Colombia) ─────────────────

    public static ReporteFilter Trimestral()
    {
        var ahora = TimeZoneHelper.AhoraLocal();
        return new ReporteFilter
        {
            Periodo = "trimestral",
            Desde   = TimeZoneHelper.InicioDeDia(ahora.AddMonths(-3)),
            Hasta   = TimeZoneHelper.FinDeDia(ahora)
        };
    }

    public static ReporteFilter Semestral()
    {
        var ahora = TimeZoneHelper.AhoraLocal();
        return new ReporteFilter
        {
            Periodo = "semestral",
            Desde   = TimeZoneHelper.InicioDeDia(ahora.AddMonths(-6)),
            Hasta   = TimeZoneHelper.FinDeDia(ahora)
        };
    }

    public static ReporteFilter Anual()
    {
        var ahora = TimeZoneHelper.AhoraLocal();
        return new ReporteFilter
        {
            Periodo = "anual",
            Desde   = new DateTime(ahora.Year, 1, 1, 0, 0, 0),
            Hasta   = TimeZoneHelper.FinDeDia(ahora)
        };
    }

    public static ReporteFilter Personalizado(DateTime desde, DateTime hasta)
    {
        // Normalizamos extremos e invertimos si están al revés
        var d = TimeZoneHelper.InicioDeDia(desde);
        var h = TimeZoneHelper.FinDeDia(hasta);
        if (d > h) (d, h) = (h, TimeZoneHelper.FinDeDia(desde));
        return new ReporteFilter { Periodo = "personalizado", Desde = d, Hasta = h };
    }

    /// <summary>
    /// Construye un filtro desde un string de periodo y fechas opcionales.
    /// Si el periodo es personalizado y las fechas llegan nulas, aplica Trimestral como fallback seguro.
    /// </summary>
    public static ReporteFilter FromPeriodo(string? periodo, DateTime? desde = null, DateTime? hasta = null) =>
        periodo?.ToLower() switch
        {
            "trimestral" => Trimestral(),
            "semestral"  => Semestral(),
            "anual"      => Anual(),
            _            => desde.HasValue && hasta.HasValue
                                ? Personalizado(desde.Value, hasta.Value)
                                : Trimestral()   // fallback seguro: nunca rango vacío
        };
}
