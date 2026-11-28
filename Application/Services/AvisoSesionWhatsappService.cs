using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Constantes;
using Shared.DTOs.Whatsapp;

namespace Application.Services;

/// <summary>
/// Guarda en la cuenta el estado de su sesión de WhatsApp cada vez que el gateway avisa un cambio.
/// Con eso el front muestra el cartel de "se desconectó tu WhatsApp" sin consultar al bot.
/// </summary>
public class AvisoSesionWhatsappService(
    ITenantRepository tenants,
    IUnitOfWork uow,
    ILogger<AvisoSesionWhatsappService> logger) : IAvisoSesionWhatsappService
{
    public async Task ProcesarAsync(WebhookSesionDto aviso, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(aviso.Etiqueta, out var tenantId))
        {
            logger.LogWarning("Aviso de sesión {Id} sin una etiqueta de cuenta válida ('{Etiqueta}').", aviso.Id, aviso.Etiqueta);
            return;
        }

        var tenant = await tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("Aviso de sesión {Id} para la cuenta {TenantId}, que no existe.", aviso.Id, tenantId);
            return;
        }

        // Si a la cuenta le cambiaron el teléfono, el aviso es del número viejo.
        if (tenant.WhatsappTelefonoId != aviso.TelefonoId)
        {
            logger.LogWarning("Aviso de sesión {Id} del teléfono {TelefonoId}, que no es el de la cuenta {TenantId}.",
                aviso.Id, aviso.TelefonoId, tenantId);
            return;
        }

        // Un reintento atrasado no puede pisar un estado más nuevo.
        if (tenant.WhatsappUltimoAvisoId is long ultimo && aviso.Id <= ultimo)
            return;

        tenant.WhatsappEstadoSesion = CteSesionWhatsapp.Normalizar(aviso.Estado);
        tenant.WhatsappEstadoDesde = DateTime.SpecifyKind(aviso.Desde, DateTimeKind.Utc);
        tenant.WhatsappUltimoAvisoId = aviso.Id;
        await uow.GuardarAsync(cancellationToken);

        logger.LogInformation("WhatsApp de la cuenta {TenantId}: {Anterior} -> {Estado}",
            tenantId, aviso.EstadoAnterior ?? "sin dato", tenant.WhatsappEstadoSesion);
    }
}
