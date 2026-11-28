using Shared.DTOs.Whatsapp;

namespace Application.Interfaces;

public interface IWhatsappConexionService
{
    Task<WhatsappEstadoDto> EstadoAsync(CancellationToken cancellationToken = default);
    Task<WhatsappQrDto> QrAsync(CancellationToken cancellationToken = default);

    /// <summary>Si hay que mostrar el cartel de WhatsApp desconectado. Lee la base, no al bot.</summary>
    Task<WhatsappAlertaDto> AlertaAsync(CancellationToken cancellationToken = default);
}
