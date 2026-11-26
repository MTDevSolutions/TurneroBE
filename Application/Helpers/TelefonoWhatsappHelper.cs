namespace Application.Helpers;

/// <summary>
/// Lleva un teléfono (texto libre, de un paciente o del profesional) al formato que entiende WhatsApp para Argentina:
/// 549 + código de área + número, sólo dígitos. Lo que no se puede resolver sin adivinar
/// (un 0 o un 15 de por medio, falta de código de área) devuelve null: se rechaza en vez de
/// mandar mensajes a un número equivocado.
/// </summary>
public static class TelefonoWhatsappHelper
{
    public static string? Normalizar(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return null;

        var d = new string(telefono.Where(char.IsAsciiDigit).ToArray());

        if (d.Length == 13 && d.StartsWith("549") && EsNacional(d[3..]))
            return d;
        if (d.Length == 12 && d.StartsWith("54") && EsNacional(d[2..]))
            return "549" + d[2..];
        if (d.Length == 10 && EsNacional(d))
            return "549" + d;

        return null;
    }


    // Código de área + número, 10 dígitos. Los códigos de área argentinos son 11 o empiezan con
    // 2 o 3; cualquier otro comienzo es un 0 o un 15 mal puesto.
    private static bool EsNacional(string n)
        => n.Length == 10 && (n.StartsWith("11") || n[0] == '2' || n[0] == '3');
}
