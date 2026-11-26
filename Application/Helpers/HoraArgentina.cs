namespace Application.Helpers;

/// <summary>
/// Los turnos se guardan en UTC; los mensajes al paciente van en hora argentina. Argentina tiene una
/// sola zona y no usa horario de verano, pero se pide al sistema por si eso cambia; si el contenedor
/// no tiene la base de zonas, se cae a UTC-3 fijo.
/// </summary>
public static class HoraArgentina
{
    private static readonly TimeZoneInfo Zona = Resolver();

    private static readonly string[] Dias = ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

    public static DateTime ALocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    /// <summary>"lunes 1/12 a las 15:30"</summary>
    public static string FechaYHora(DateTime utc)
    {
        var local = ALocal(utc);
        return $"{Dias[(int)local.DayOfWeek]} {local.Day}/{local.Month} a las {local:HH:mm}";
    }

    private static TimeZoneInfo Resolver()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("ART", TimeSpan.FromHours(-3), "Argentina", "Argentina");
        }
    }
}
