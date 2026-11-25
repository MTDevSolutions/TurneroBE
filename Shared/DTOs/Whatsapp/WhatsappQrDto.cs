namespace Shared.DTOs.Whatsapp;

public class WhatsappQrDto
{
    public string EstadoSesion { get; set; } = null!;

    /// <summary>data-url PNG del QR; null si no hay QR que mostrar (conectado, sin configurar, caído).</summary>
    public string? Qr { get; set; }
}
