using Shared.DTOs.Paginacion;

namespace Tests.DTOs;

public class PaginacionTests
{
    [Theory]
    [InlineData(0, 20, 1, 20)]        // page < 1 se lleva a 1
    [InlineData(-5, 20, 1, 20)]
    [InlineData(3, 0, 3, 1)]          // pageSize se clampea a [1,100]
    [InlineData(3, 500, 3, 100)]
    [InlineData(2, 50, 2, 50)]        // dentro de rango, se respeta
    public void Normalizado_clampea_page_y_pageSize(int page, int pageSize, int esperadoPage, int esperadoPageSize)
    {
        var (p, ps) = new PaginacionQuery { Page = page, PageSize = pageSize }.Normalizado();

        Assert.Equal(esperadoPage, p);
        Assert.Equal(esperadoPageSize, ps);
    }
}
