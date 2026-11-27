namespace Application.Interfaces;

/// <summary>Respuesta de un envío. StatusCode null = no hubo respuesta (timeout o sin red).</summary>
public sealed record RespuestaEnvioGateway(int? StatusCode, string? MessageId, string? Error);

/// <summary>Sesión del bot de un teléfono. Estado ya normalizado a CteSesionWhatsapp.</summary>
public sealed record SesionWhatsapp(string Estado, string? Numero, string? Qr);

/// <summary>Wabix Gateway: el único que habla con WhatsApp.</summary>
public interface IWhatsappGateway
{
    /// <summary>False si falta BaseUrl o ApiKey: WhatsApp está deshabilitado en este entorno.</summary>
    bool Configurado { get; }

    /// <summary>No lanza por fallas del gateway: todo resultado vuelve en la respuesta.</summary>
    Task<RespuestaEnvioGateway> EnviarTextoAsync(int telefonoId, string destino, string texto, CancellationToken cancellationToken = default);

    /// <summary>Null si el gateway no responde.</summary>
    Task<SesionWhatsapp?> ObtenerSesionAsync(int telefonoId, CancellationToken cancellationToken = default);

    /// <summary>true/false según el gateway; null si no se pudo saber.</summary>
    Task<bool?> ExisteTelefonoAsync(int telefonoId, CancellationToken cancellationToken = default);
}
