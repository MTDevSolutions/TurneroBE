using Shared.DTOs.Whatsapp;

namespace Application.Interfaces;

public interface IHistorialWhatsappService
{
    /// <summary>Mensajes enviados y respuestas recibidas de un turno de la cuenta, en orden.</summary>
    Task<List<MensajeWhatsappDto>> ListarPorTurnoAsync(int turnoId, CancellationToken cancellationToken = default);
}
