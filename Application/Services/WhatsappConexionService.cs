using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Security_Context;
using Shared.Constantes;
using Shared.DTOs.Whatsapp;

namespace Application.Services;

/// <summary>
/// Estado de la sesión de WhatsApp y QR del tenant del request. El teléfono sale siempre del
/// tenant (nunca del front): así una cuenta no puede ver el QR del número de otra.
/// </summary>
public class WhatsappConexionService(
    ITenantContext tenantContext,
    ITenantRepository tenantRepository,
    IConfiguracionService configuracionService,
    IWhatsappGateway gateway) : IWhatsappConexionService
{
    public async Task<WhatsappEstadoDto> EstadoAsync(CancellationToken cancellationToken = default)
    {
        var config = await configuracionService.ObtenerAsync(cancellationToken);
        var sesion = await SesionAsync(cancellationToken);

        return new WhatsappEstadoDto
        {
            EstadoSesion = sesion.Estado,
            Numero = sesion.Numero,
            AvisosActivos = config.AvisosWhatsappActivos,
        };
    }

    public async Task<WhatsappQrDto> QrAsync(CancellationToken cancellationToken = default)
    {
        var sesion = await SesionAsync(cancellationToken);

        return new WhatsappQrDto
        {
            EstadoSesion = sesion.Estado,
            Qr = CteSesionWhatsapp.MuestraQr(sesion.Estado) ? sesion.Qr : null,
        };
    }

    // Si el gateway no responde no es un error del request: la pantalla muestra "reconectando".
    private async Task<SesionWhatsapp> SesionAsync(CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId ?? throw AppException.RegistroNoExiste();
        var tenant = await tenantRepository.WhatsappInfoAsync(tenantId, ct);

        if (!gateway.Configurado || tenant?.WhatsappTelefonoId is not int telefonoId)
            return new SesionWhatsapp(CteSesionWhatsapp.SinConfigurar, null, null);

        return await gateway.ObtenerSesionAsync(telefonoId, ct)
            ?? new SesionWhatsapp(CteSesionWhatsapp.Desconectado, null, null);
    }
}
