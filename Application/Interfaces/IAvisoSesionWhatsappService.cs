using Shared.DTOs.Whatsapp;

namespace Application.Interfaces;

public interface IAvisoSesionWhatsappService
{
    /// <summary>
    /// Aplica a la cuenta un aviso de sesión del gateway. Lo que no se puede atribuir (etiqueta
    /// inválida, cuenta inexistente, teléfono que ya no es de esa cuenta, aviso viejo) se ignora.
    /// </summary>
    Task ProcesarAsync(WebhookSesionDto aviso, CancellationToken cancellationToken = default);
}
