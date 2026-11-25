namespace Shared.Settings;

/// <summary>Sección "WhatsappGateway" de appsettings. Sin BaseUrl o ApiKey, el envío queda deshabilitado.</summary>
public class WhatsappGatewaySettings
{
    public const string Seccion = "WhatsappGateway";

    public string BaseUrl { get; set; } = "";

    /// <summary>API key de la Empresa "Turnero" en el gateway.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Secreto que el gateway manda en X-Webhook-Secret al entregar respuestas. Es el mismo en todos
    /// los teléfonos del turnero. Vacío = el webhook rechaza todo (401).
    /// </summary>
    public string WebhookSecret { get; set; } = "";

    public int TimeoutSegundos { get; set; } = 15;
}
