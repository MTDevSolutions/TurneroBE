namespace Shared.DTOs.Whatsapp;

/// <summary>
/// Lo que entrega Wabix Gateway por webhook (MensajePendienteDto del gateway). Los adjuntos no se
/// usan: el turnero solo entiende texto.
/// </summary>
public class WebhookEntranteDto
{
    /// <summary>Id del mensaje en el gateway: se usa para no procesar dos veces un reintento.</summary>
    public long Id { get; set; }

    /// <summary>Número del remitente, solo dígitos (549...).</summary>
    public string? Numero { get; set; } = "";

    public string? Texto { get; set; } = "";

    public DateTime RecibidoEn { get; set; }

    /// <summary>Etiqueta del teléfono en el gateway: el id de la cuenta del turnero.</summary>
    public string? Etiqueta { get; set; }
}
