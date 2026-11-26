using Application.Helpers;
using Application.Interfaces;
using Application.UseCases.Turnos;
using Application.UseCases.Whatsapp;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.Constantes;

namespace Application.Services;

/// <summary>
/// Una pasada del envío: reclama pendientes de todas las cuentas, los manda al gateway y registra
/// el resultado. Separada del BackgroundService para poder probarla sin levantar un host.
/// </summary>
public class ProcesadorEnvioWhatsapp(
    IMensajeWhatsappRepository mensajeRepository,
    ITenantRepository tenantRepository,
    IWhatsappGateway gateway,
    IUnitOfWork uow,
    ILogger<ProcesadorEnvioWhatsapp> logger)
{
    private const int MaxLargoError = 500;

    /// <summary>Devuelve cuántos mensajes reclamó (0 = no había nada para hacer).</summary>
    public async Task<int> ProcesarAsync(DateTime ahora, CancellationToken cancellationToken)
    {
        var lote = await mensajeRepository.ReclamarPendientesAsync(
            ahora, CteMensajesWhatsapp.MaxPorPasada, CteMensajesWhatsapp.Lease, cancellationToken);
        if (lote.Count == 0)
            return 0;

        var tenants = (await tenantRepository.WhatsappInfoAsync(
                lote.Select(m => m.TenantId).Distinct().ToList(), cancellationToken))
            .ToDictionary(t => t.TenantId);

        foreach (var mensaje in lote)
        {
            if (cancellationToken.IsCancellationRequested)
                break; // lo no procesado vuelve a la cola al vencer el lease

            await ProcesarUnoAsync(mensaje, tenants.GetValueOrDefault(mensaje.TenantId), ahora, cancellationToken);

            // Un save por mensaje: si el proceso muere a mitad del lote, lo ya enviado no se repite.
            // Sin cancellationToken: si el host se apaga justo después de enviar, el resultado igual
            // tiene que quedar guardado; si no, al vencer el lease el mensaje sale de nuevo.
            await uow.GuardarAsync(CancellationToken.None);
        }

        return lote.Count;
    }

    private async Task ProcesarUnoAsync(MensajeWhatsapp mensaje, TenantWhatsappInfo? tenant, DateTime ahora, CancellationToken ct)
    {
        if (ahora - mensaje.CreadoEn > CteMensajesWhatsapp.Vigencia)
        {
            Descartar(mensaje, "Vencido: no se pudo enviar a tiempo.");
            return;
        }

        // El estado de la cuenta se mira acá y no al encolar: una cuenta suspendida después de encolar
        // no debe seguir mandando, y una desvinculada nunca debe mandar por otro número.
        if (tenant is null)
        {
            Descartar(mensaje, "La cuenta no existe.");
            return;
        }

        if (tenant.Estado != CteEstadosTenant.Activo)
        {
            Descartar(mensaje, "La cuenta está suspendida.");
            return;
        }

        // Sin la aceptación vigente de los términos no sale nada por el canal no oficial.
        if (tenant.TerminosVersionAceptada != CteTerminos.VersionVigente)
        {
            Descartar(mensaje, "La cuenta no aceptó los términos de uso vigentes.");
            return;
        }

        if (tenant.WhatsappTelefonoId is not int telefonoId)
        {
            Descartar(mensaje, "Todavía no está vinculado el WhatsApp de la cuenta.");
            return;
        }

        if (MotivoTurnoCambio(mensaje, ahora) is string motivo)
        {
            Descartar(mensaje, motivo);
            return;
        }

        if (string.IsNullOrWhiteSpace(mensaje.Destino))
        {
            Descartar(mensaje, "No hay un celular al que mandarlo.");
            return;
        }

        var respuesta = await gateway.EnviarTextoAsync(telefonoId, mensaje.Destino, mensaje.Texto, ct);

        // `ahora` es el de la pasada (reclamo y vigencia); tras la llamada al gateway ya pasó tiempo,
        // así que el backoff y EnviadoEn salen del reloj de este momento.
        var alResultado = DateTime.UtcNow;

        switch (ResultadoEnvioHelper.Clasificar(respuesta.StatusCode))
        {
            case TipoResultadoEnvio.Enviado:
                mensaje.Estado = CteMensajesWhatsapp.Enviado;
                mensaje.EnviadoEn = alResultado;
                mensaje.GatewayMessageId = respuesta.MessageId;
                mensaje.UltimoError = null;
                break;

            case TipoResultadoEnvio.Permanente:
                mensaje.Intentos++;
                mensaje.Estado = CteMensajesWhatsapp.Fallido;
                mensaje.UltimoError = Recortar(respuesta.Error);
                break;

            default:
                mensaje.Intentos++;
                mensaje.UltimoError = Recortar(respuesta.Error);
                if (respuesta.StatusCode is 401 or 403)
                    logger.LogError("El gateway de WhatsApp rechazó al turnero (API key inválida o empresa inactiva): revisar WhatsappGateway:ApiKey y el estado de la empresa en el gateway.");

                if (mensaje.Intentos >= CteMensajesWhatsapp.MaxIntentos)
                    mensaje.Estado = CteMensajesWhatsapp.Fallido;
                else
                    mensaje.ProximoIntentoEn = alResultado + CteMensajesWhatsapp.Backoff(mensaje.Intentos);
                break;
        }
    }

    /// <summary>
    /// Un mensaje que espera reintento puede quedar viejo: el turno se canceló, ya empezó o cambió de
    /// paciente. Mandarlo igual le diría al paciente algo falso (y un "Sí" confirmaría otro horario).
    /// </summary>
    private static string? MotivoTurnoCambio(MensajeWhatsapp mensaje, DateTime ahora)
    {
        if (mensaje.Turno is not { } turno || !CteMensajesWhatsapp.TiposSobreHorario.Contains(mensaje.Tipo))
            return null;

        if (!MaquinaEstadosTurno.EsEditable(turno.Estado))
            return $"El turno ya está {turno.Estado}.";
        if (turno.Inicio <= ahora)
            return "El turno ya empezó.";
        if (turno.Paciente is { } paciente && paciente.Celular != mensaje.Destino)
            return "El turno cambió de paciente.";
        return null;
    }

    private static void Descartar(MensajeWhatsapp mensaje, string motivo)
    {
        mensaje.Estado = CteMensajesWhatsapp.Descartado;
        mensaje.UltimoError = motivo;
    }

    private static string? Recortar(string? error)
        => error is { Length: > MaxLargoError } ? error[..MaxLargoError] : error;
}
