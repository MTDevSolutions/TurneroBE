namespace Shared.DTOs.Paginacion;

/// <summary>Página de resultados de un listado paginado.</summary>
public class PaginaDto<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
