using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Shared.Constantes;

namespace Application.Services;

/// <summary>
/// Una pasada de recordatorios: encola el recordatorio de los turnos que entraron en la ventana de
/// horas de su cuenta. Separada del BackgroundService para poder probarla sin host.
/// </summary>
public class ProcesadorRecordatorios(
    ITurnoRepository turnos,
    ITenantRepository tenants,
    IMensajeWhatsappRepository mensajes,
    IPlantillaMensajeRepository plantillas,
    IUnitOfWork uow)
{
    public const int MaxPorPasada = 100;

    private static readonly Dictionary<string, string> SinTextosPropios = [];

    /// <summary>Devuelve cuántos recordatorios encoló.</summary>
    public async Task<int> ProcesarAsync(DateTime ahora, CancellationToken cancellationToken)
    {
        var lote = await turnos.ListarParaRecordatorioAsync(ahora, MaxPorPasada, cancellationToken);
        if (lote.Count == 0)
            return 0;

        var cuentas = lote.Select(t => t.TenantId).Distinct().ToList();
        var nombres = (await tenants.WhatsappInfoAsync(cuentas, cancellationToken))
            .ToDictionary(t => t.TenantId, t => t.Nombre);
        var textosPropios = await plantillas.TextosAsync(cuentas, cancellationToken);

        foreach (var turno in lote)
        {
            var profesional = nombres.GetValueOrDefault(turno.TenantId, "tu profesional");
            mensajes.Agregar(new MensajeWhatsapp
            {
                TenantId = turno.TenantId, // el worker no tiene cuenta en contexto
                TurnoId = turno.Id,
                Tipo = CteMensajesWhatsapp.TipoRecordatorio,
                Destino = turno.Paciente.Celular,
                Texto = TextosWhatsapp.Armar(TextosWhatsapp.ClaveRecordatorio(turno), turno, profesional,
                    textosPropios.GetValueOrDefault(turno.TenantId) ?? SinTextosPropios),
                Estado = CteMensajesWhatsapp.Pendiente,
                ProximoIntentoEn = ahora,
                CreadoEn = ahora,
            });
            turno.RecordatorioEncoladoEn = ahora;
        }

        // Si alguien movió uno de estos turnos mientras tanto (xmin), se pierde esta pasada entera y
        // la próxima vuelve a leerlos: el recordatorio sale con el horario nuevo, nunca con el viejo.
        return await uow.IntentarGuardarAsync(cancellationToken) ? lote.Count : 0;
    }
}
