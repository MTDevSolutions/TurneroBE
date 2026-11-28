namespace Shared.DTOs.Whatsapp;

/// <summary>
/// Aviso de Wabix Gateway cuando cambia la sesión de WhatsApp de un teléfono (AvisoSesionDto del
/// gateway): se cayó, WhatsApp la cerró o volvió.
/// </summary>
public class WebhookSesionDto
{
    /// <summary>Crece con cada aviso: uno menor o igual al último aplicado se ignora.</summary>
    public long Id { get; set; }

    public int TelefonoId { get; set; }

    public string? Numero { get; set; }

    /// <summary>Etiqueta del teléfono en el gateway: el id de la cuenta del turnero.</summary>
    public string? Etiqueta { get; set; }

    public string? EstadoAnterior { get; set; }

    /// <summary>conectado | esperando_qr | desconectado | deslogueado.</summary>
    public string? Estado { get; set; }

    public DateTime Desde { get; set; }
}
