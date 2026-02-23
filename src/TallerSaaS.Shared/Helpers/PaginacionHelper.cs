namespace TallerSaaS.Shared.Helpers;

public class PaginacionResultado<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalRegistros { get; set; }
    public int Pagina { get; set; }
    public int RegistrosPorPagina { get; set; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / RegistrosPorPagina);
    public bool TienePaginaAnterior => Pagina > 1;
    public bool TienePaginaSiguiente => Pagina < TotalPaginas;
}

public static class PaginacionHelper
{
    public static PaginacionResultado<T> Paginar<T>(IEnumerable<T> fuente, int pagina, int registrosPorPagina = 10)
    {
        var lista = fuente.ToList();
        var resultado = new PaginacionResultado<T>
        {
            TotalRegistros = lista.Count,
            Pagina = pagina,
            RegistrosPorPagina = registrosPorPagina,
            Items = lista.Skip((pagina - 1) * registrosPorPagina).Take(registrosPorPagina).ToList()
        };
        return resultado;
    }
}
