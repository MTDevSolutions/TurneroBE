using Shared.DTOs.Whatsapp;

namespace Application.Interfaces;

public interface IWhatsappConexionService
{
    Task<WhatsappEstadoDto> EstadoAsync(CancellationToken cancellationToken = default);
    Task<WhatsappQrDto> QrAsync(CancellationToken cancellationToken = default);
}
