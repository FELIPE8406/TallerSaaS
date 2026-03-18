namespace TallerSaaS.Application.DTOs;

/// <summary>
/// Centraliza la lógica de zona horaria de Colombia (UTC-5).
/// DRY: todo el módulo debe usar AhoraLocal() en lugar de DateTime.UtcNow
/// cuando necesite la hora del negocio (registros almacenados en hora local).
/// </summary>
public static class TimeZoneHelper
{
    /// <summary>Zona horaria de Colombia (América/Bogotá, UTC-5).</summary>
    private static readonly TimeZoneInfo ColombiaZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            // Windows: "SA Pacific Standard Time"  |  Linux/macOS: "America/Bogota"
            OperatingSystem.IsWindows()
                ? "SA Pacific Standard Time"
                : "America/Bogota");

    /// <summary>
    /// Devuelve la fecha y hora actual en la zona horaria de Colombia.
    /// Usar siempre en lugar de DateTime.UtcNow / DateTime.Now en el módulo de reportes.
    /// </summary>
    public static DateTime AhoraLocal() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ColombiaZone);

    /// <summary>
    /// Convierte una fecha/hora local de Colombia a UTC para almacenamiento estándar.
    /// Asume que 'localDateTime' es *unspecified* o *local* referenciado al taller en Colombia.
    /// </summary>
    public static DateTime ToUtcFromColombia(DateTime localDateTime)
    {
        // Asegurar formato local/unspecified para la conversión segura
        localDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, ColombiaZone);
    }

    /// <summary>
    /// Convierte una fecha/hora UTC (típicamente de Base de Datos) a hora de Colombia.
    /// </summary>
    public static DateTime ToColombiaFromUtc(DateTime utcDateTime)
    {
        utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ColombiaZone);
    }

    /// <summary>
    /// Normaliza la fecha "hasta" al último instante del día (23:59:59.999)
    /// para que la comparación &lt;= incluya todos los registros del día.
    /// </summary>
    public static DateTime FinDeDia(DateTime fecha) =>
        fecha.Date.AddDays(1).AddTicks(-1);

    /// <summary>
    /// Normaliza la fecha "desde" al primer instante del día (00:00:00.000).
    /// </summary>
    public static DateTime InicioDeDia(DateTime fecha) =>
        fecha.Date;
}
