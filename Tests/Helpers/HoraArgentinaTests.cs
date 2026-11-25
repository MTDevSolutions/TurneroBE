using Application.Helpers;

namespace Tests.Helpers;

public class HoraArgentinaTests
{
    [Fact]
    public void Muestra_dia_fecha_y_hora_en_hora_argentina()
        // 2/12/2026 18:00 UTC = miércoles 15:00 en Argentina (UTC-3).
        => Assert.Equal("miércoles 2/12 a las 15:00",
            HoraArgentina.FechaYHora(new DateTime(2026, 12, 2, 18, 0, 0, DateTimeKind.Utc)));

    [Fact]
    public void Cruza_la_medianoche_hacia_el_dia_anterior()
        => Assert.Equal("martes 1/12 a las 22:30",
            HoraArgentina.FechaYHora(new DateTime(2026, 12, 2, 1, 30, 0, DateTimeKind.Utc)));
}
