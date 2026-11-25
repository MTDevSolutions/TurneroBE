namespace Shared.DTOs.Paginacion;

/// <summary>Parámetros de paginación aceptados por query string en los listados.</summary>
public class PaginacionQuery
{
    private const int PageSizeMin = 1;
    private const int PageSizeMax = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Normaliza valores fuera de rango (Page &lt; 1, PageSize fuera de [1,100]).</summary>
    public (int Page, int PageSize) Normalizado()
    {
        var page = Page < 1 ? 1 : Page;
        var pageSize = Math.Clamp(PageSize, PageSizeMin, PageSizeMax);
        return (page, pageSize);
    }
}
