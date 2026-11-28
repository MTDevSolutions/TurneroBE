using Application.Helpers;
using Application.Interfaces;
using Application.UseCases.Security_Context;
using Domain.Entities;
using Shared.Constantes;

namespace Application.Services;

/// <summary>
/// Arma y encola los avisos al paciente que dispara la agenda. Si la cuenta todavía no vinculó su
/// WhatsApp el mensaje igual se encola: el worker lo descarta con el motivo a la vista en el
/// historial, en vez de que el aviso "desaparezca" sin explicación.
/// </summary>
public class AvisoTurnoService(
    ITenantContext tenantContext,
    ITenantRepository tenants,
    IConfiguracionRepository configuracion,
    IMensajeWhatsappRepository mensajes,
    IPlantillaMensajeRepository plantillas,
    TimeProvider reloj) : IAvisoTurnoService
{
    private static readonly HashSet<string> TiposValidos =
        [CteMensajesWhatsapp.TipoAviso, CteMensajesWhatsapp.TipoReprogramado, CteMensajesWhatsapp.TipoCancelado];

    public async Task EncolarAsync(Turno turno, string tipo, CancellationToken cancellationToken = default)
    {
        if (!TiposValidos.Contains(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de aviso de agenda desconocido.");

        // Lo que quedó sin salir sobre el horario anterior (o el paciente anterior) ya no es cierto.
        if (turno.Id != 0)
            await mensajes.DescartarPendientesDelTurnoAsync(turno.Id, "Reemplazado por un aviso más nuevo.", cancellationToken);

        var ahora = reloj.GetUtcNow().UtcDateTime;
        if (turno.Inicio <= ahora)
            return; // un turno que ya empezó no necesita aviso

        var config = await configuracion.ObtenerAsync(cancellationToken) ?? new Configuracion();
        if (!config.AvisosWhatsappActivos)
            return;

        var tenantId = tenantContext.TenantId ?? turno.TenantId;
        var profesional = (await tenants.WhatsappInfoAsync(tenantId, cancellationToken))?.Nombre ?? "tu profesional";

        // El tipo de los avisos de agenda coincide con la clave de su plantilla.
        var texto = TextosWhatsapp.Armar(tipo, turno, profesional, await plantillas.TextosAsync(tenantId, cancellationToken));

        mensajes.Agregar(new MensajeWhatsapp
        {
            TenantId = tenantId,
            Turno = turno,
            Tipo = tipo,
            Destino = turno.Paciente.Celular,
            Texto = texto,
            Estado = CteMensajesWhatsapp.Pendiente,
            ProximoIntentoEn = ahora,
            CreadoEn = ahora,
        });

        // El aviso ya pide confirmar: si el turno cae dentro de la ventana del recordatorio, mandar
        // además el recordatorio sería repetir el mismo mensaje minutos después.
        if (tipo != CteMensajesWhatsapp.TipoCancelado
            && config.HorasRecordatorio > 0
            && turno.Inicio.AddHours(-config.HorasRecordatorio) <= ahora)
        {
            turno.RecordatorioEncoladoEn = ahora;
        }
    }
}
