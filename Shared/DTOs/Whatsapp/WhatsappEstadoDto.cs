namespace Shared.DTOs.Whatsapp;

public class WhatsappEstadoDto
{
    /// <summary>sin_configurar | esperando_qr | conectado | desconectado | deslogueado</summary>
    public string EstadoSesion { get; set; } = null!;
    public string? Numero { get; set; }
    public bool AvisosActivos { get; set; }
}
