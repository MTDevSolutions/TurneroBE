using System.Globalization;
using System.Text;

namespace Application.Helpers;

public enum IntencionRespuesta { Confirmar, Cancelar, NoEntendida }

/// <summary>
/// Entiende el "Sí" o "No" del paciente. Mira solo el comienzo del mensaje, sin acentos ni signos:
/// "Sí!", "si, ahí estaré", "SII", "no puedo", "👍". Ante la duda no toca el turno: cancelar por
/// error es peor que no confirmar.
/// </summary>
public static class InterpreteRespuesta
{
    private static readonly HashSet<string> Si =
    [
        "si", "sii", "siii", "sip", "s", "confirmo", "confirmado", "confirmar", "ok", "oka", "okey", "dale",
        "perfecto", "listo", "claro", "obvio", "1",
    ];

    private static readonly HashSet<string> No =
    [
        "no", "noo", "nop", "n", "cancelo", "cancelar", "cancela", "cancelado", "2",
    ];

    private static readonly string[] EmojisSi = ["👍", "✅", "👌", "🙌"];
    private static readonly string[] EmojisNo = ["👎", "❌"];

    public static IntencionRespuesta Interpretar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return IntencionRespuesta.NoEntendida;

        var limpio = texto.Trim();
        if (EmojisSi.Any(e => limpio.StartsWith(e, StringComparison.Ordinal)))
            return IntencionRespuesta.Confirmar;
        if (EmojisNo.Any(e => limpio.StartsWith(e, StringComparison.Ordinal)))
            return IntencionRespuesta.Cancelar;

        var palabras = Normalizar(limpio).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (palabras.Length == 0)
            return IntencionRespuesta.NoEntendida;

        var primera = palabras[0];
        // "no se", "no sé si voy": duda, no una cancelación.
        if (primera == "no" && palabras.Length > 1 && palabras[1] is "se" or "sabe" or "estoy")
            return IntencionRespuesta.NoEntendida;

        if (No.Contains(primera))
            return IntencionRespuesta.Cancelar;
        if (Si.Contains(primera))
            return IntencionRespuesta.Confirmar;

        return IntencionRespuesta.NoEntendida;
    }

    // Minúsculas, sin acentos, y todo lo que no es letra o dígito pasa a ser espacio.
    private static string Normalizar(string texto)
    {
        var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return sb.ToString();
    }
}
