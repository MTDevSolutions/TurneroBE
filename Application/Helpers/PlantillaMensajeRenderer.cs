using System.Text.RegularExpressions;
using Shared.Constantes;

namespace Application.Helpers;

/// <summary>Arma el texto de un mensaje a partir de su plantilla y valida las variables (como en CellFix).</summary>
public static partial class PlantillaMensajeRenderer
{
    public sealed record Variable(string Nombre, string Descripcion);

    public static readonly IReadOnlyList<Variable> Variables =
    [
        new("paciente", "Nombre del paciente"),
        new("apellido", "Apellido del paciente"),
        new("profesional", "Tu nombre, como figura en la cuenta"),
        new("fecha", "Día del turno (ej. jueves 3/12)"),
        new("hora", "Hora del turno (ej. 15:00)"),
        new("lugar", "Consultorio y dirección"),
    ];

    private static readonly HashSet<string> Nombres = Variables.Select(v => v.Nombre).ToHashSet();

    // Detección: cualquier cosa entre llaves, así "{Paciente}" o "{nombre}" se marcan como
    // desconocidas en vez de llegarle al paciente tal cual.
    [GeneratedRegex(@"\{([^{}\r\n]{1,40})\}")]
    private static partial Regex MarcadorCualquiera();

    // Reemplazo: solo nombres exactos; un marcador desconocido se deja como está.
    [GeneratedRegex(@"\{([a-z_]+)\}")]
    private static partial Regex Marcador();

    public static List<string> VariablesDesconocidas(string texto)
        => MarcadorCualquiera().Matches(texto)
            .Select(m => m.Groups[1].Value)
            .Where(v => !Nombres.Contains(v))
            .Distinct()
            .ToList();

    public static string Renderizar(string texto, VariablesMensaje v)
        => Marcador().Replace(texto, m => Valor(m.Groups[1].Value, v) ?? m.Value);

    /// <summary>Texto final: la plantilla (propia o de fábrica) completada, más el pie fijo si lo tiene.</summary>
    public static string Armar(CtePlantillasMensaje.Definicion definicion, string? textoPropio, VariablesMensaje v)
    {
        var cuerpo = Renderizar(textoPropio ?? definicion.TextoPorDefecto, v);
        return definicion.Pie is null ? cuerpo : $"{cuerpo}\n\n{definicion.Pie}";
    }

    private static string? Valor(string nombre, VariablesMensaje v) => nombre switch
    {
        "paciente" => v.Paciente,
        "apellido" => v.Apellido,
        "profesional" => v.Profesional,
        "fecha" => v.Fecha,
        "hora" => v.Hora,
        "lugar" => v.Lugar,
        _ => null,
    };
}
