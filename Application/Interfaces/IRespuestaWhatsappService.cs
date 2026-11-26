using Shared.DTOs.Whatsapp;

namespace Application.Interfaces;

public interface IRespuestaWhatsappService
{
    /// <summary>True si el secreto del header coincide con el configurado (comparación en tiempo constante).</summary>
    bool SecretoValido(string? secreto);

    /// <summary>Procesa la respuesta de un paciente: confirma o cancela su próximo turno.</summary>
    Task ProcesarAsync(WebhookEntranteDto mensaje, CancellationToken cancellationToken = default);
}
