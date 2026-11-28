namespace Shared.DTOs.Whatsapp;

/// <summary>
/// Lo que necesita el cartel de "se desconectó tu WhatsApp". Sale de la base (lo último que avisó
/// el gateway), no de consultar al bot: el front lo pide seguido desde cualquier pantalla.
/// </summary>
public class WhatsappAlertaDto
{
    /// <summary>True si la cuenta tiene WhatsApp vinculado y la sesión no está conectada.</summary>
    public bool Mostrar { get; set; }

    /// <summary>esperando_qr | desconectado | deslogueado (null si no hay que mostrar nada).</summary>
    public string? Estado { get; set; }

    /// <summary>Desde cuándo (UTC).</summary>
    public DateTime? Desde { get; set; }
}
