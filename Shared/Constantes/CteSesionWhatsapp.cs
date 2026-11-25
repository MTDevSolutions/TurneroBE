namespace Shared.Constantes;

/// <summary>Estado de la sesión de WhatsApp del tenant, tal como lo ve el front.</summary>
public static class CteSesionWhatsapp
{
    /// <summary>El tenant no tiene teléfono vinculado o el gateway no está configurado.</summary>
    public const string SinConfigurar = "sin_configurar";
    public const string EsperandoQr = "esperando_qr";
    public const string Conectado = "conectado";
    /// <summary>El bot o el gateway no responden: no es un problema de la cuenta.</summary>
    public const string Desconectado = "desconectado";
    /// <summary>WhatsApp cerró la sesión: hay que volver a escanear el QR.</summary>
    public const string Deslogueado = "deslogueado";

    private static readonly HashSet<string> DelBot = [EsperandoQr, Conectado, Desconectado, Deslogueado];

    /// <summary>Lo que no reconocemos se muestra como desconectado, nunca como un estado inventado.</summary>
    public static string Normalizar(string? estado)
        => estado is not null && DelBot.Contains(estado) ? estado : Desconectado;

    public static bool MuestraQr(string estado) => estado is EsperandoQr or Deslogueado;
}
