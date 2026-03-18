namespace TallerSaaS.Application.DTOs;

public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    /// <summary>
    /// Calcula el número total de páginas basadas en TotalCount y PageSize.
    /// Retorna al menos 1 página si no hay registros, para mantener coherencia UI.
    /// </summary>
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
