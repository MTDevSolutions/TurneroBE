using Domain.Entities;

namespace Application.Helpers;

/// <summary>Valores con los que se completan las variables de un mensaje de WhatsApp.</summary>
public sealed record VariablesMensaje(
    string Paciente, string Apellido, string Profesional, string Fecha, string Hora, string Lugar)
{
    /// <summary>Datos inventados para la vista previa del editor.</summary>
    public static readonly VariablesMensaje Ejemplo =
        new("Ana", "Ruiz", "tu profesional", "jueves 3/12", "15:00", "Consultorio Palermo (Gorriti 1234)");

    public static VariablesMensaje De(Turno turno, string profesional) => new(
        turno.Paciente.Nombre,
        turno.Paciente.Apellido,
        profesional,
        HoraArgentina.Fecha(turno.Inicio),
        HoraArgentina.Hora(turno.Inicio),
        LugarDe(turno));

    // Sucursal puede no venir cargada: "en el consultorio" sigue leyéndose bien.
    private static string LugarDe(Turno turno)
    {
        var sucursal = turno.Sucursal;
        if (sucursal is null)
            return "el consultorio";
        return string.IsNullOrWhiteSpace(sucursal.Direccion)
            ? sucursal.Nombre
            : $"{sucursal.Nombre} ({sucursal.Direccion})";
    }
}
