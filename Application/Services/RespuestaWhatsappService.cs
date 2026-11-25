using System.Security.Cryptography;
using System.Text;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Constantes;
using Shared.DTOs.Whatsapp;
using Shared.Settings;

namespace Application.Services;

/// <summary>
/// Respuestas de los pacientes que entrega el gateway. "Sí" confirma y "No" cancela el próximo
/// turno del paciente (libera el horario y avisa al profesional en su celular personal). Cualquier
/// otra cosa queda registrada sin tocar la agenda.
/// La cuenta sale de la Etiqueta del teléfono en el gateway (el id de la cuenta), y el paciente se
/// busca solo dentro de esa cuenta: un mismo celular puede ser paciente de dos profesionales.
/// </summary>
public class RespuestaWhatsappService(
    IOptions<WhatsappGatewaySettings> opciones,
    ITenantRepository tenants,
    IRespuestaWhatsappRepository respuestas,
    IMensajeEntranteRepository entrantes,
    IMensajeWhatsappRepository salientes,
    IUnitOfWork uow,
    TimeProvider reloj,
    ILogger<RespuestaWhatsappService> logger) : IRespuestaWhatsappService
{
    private const int MaxLargoTexto = 2000;
    private const string MotivoCancelacion = "Canceló por WhatsApp.";

    public bool SecretoValido(string? secreto)
    {
        var esperado = opciones.Value.WebhookSecret;
        if (string.IsNullOrEmpty(esperado) || string.IsNullOrEmpty(secreto))
            return false;

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(secreto), Encoding.UTF8.GetBytes(esperado));
    }

    public async Task ProcesarAsync(WebhookEntranteDto mensaje, CancellationToken cancellationToken = default)
    {
        // Lo que no se puede atribuir a una cuenta se ignora (y se responde 200): reintentarlo no lo arregla.
        if (!int.TryParse(mensaje.Etiqueta, out var tenantId))
        {
            logger.LogWarning("Respuesta de WhatsApp {Id} sin una etiqueta de cuenta válida ('{Etiqueta}').", mensaje.Id, mensaje.Etiqueta);
            return;
        }

        var tenant = await tenants.WhatsappInfoAsync(tenantId, cancellationToken);
        if (tenant is null || tenant.Estado != CteEstadosTenant.Activo)
        {
            logger.LogWarning("Respuesta de WhatsApp {Id} para la cuenta {TenantId}, que no existe o no está activa.", mensaje.Id, tenantId);
            return;
        }

        if (await entrantes.ExisteAsync(mensaje.Id, cancellationToken))
            return; // reintento del gateway: ya se procesó

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var numero = TelefonoWhatsappHelper.Normalizar(mensaje.Numero)
            ?? new string(mensaje.Numero.Where(char.IsAsciiDigit).ToArray());

        var entrante = new MensajeEntrante
        {
            TenantId = tenantId,
            GatewayMensajeId = mensaje.Id,
            Numero = numero.Length > 20 ? numero[..20] : numero,
            Texto = mensaje.Texto.Length > MaxLargoTexto ? mensaje.Texto[..MaxLargoTexto] : mensaje.Texto,
            RecibidoEn = mensaje.RecibidoEn == default ? ahora : DateTime.SpecifyKind(mensaje.RecibidoEn, DateTimeKind.Utc),
            ProcesadoEn = ahora,
        };
        entrantes.Agregar(entrante);

        entrante.Resultado = await AplicarAsync(entrante, tenantId, numero, ahora, cancellationToken);

        await uow.GuardarTraduciendoDuplicadoAsync(CteIndices.MensajeEntranteGateway,
            "Ese mensaje ya se procesó.", cancellationToken);
    }

    private async Task<string> AplicarAsync(MensajeEntrante entrante, int tenantId, string numero, DateTime ahora, CancellationToken ct)
    {
        var paciente = await respuestas.PacientePorCelularAsync(tenantId, numero, ct);
        if (paciente is null)
            return CteRespuestasWhatsapp.PacienteDesconocido;
        entrante.PacienteId = paciente.Id;

        var turno = await respuestas.ProximoTurnoVivoAsync(tenantId, paciente.Id, ahora, ct);
        if (turno is null)
            return CteRespuestasWhatsapp.SinTurno;

        switch (InterpreteRespuesta.Interpretar(entrante.Texto))
        {
            case IntencionRespuesta.Confirmar:
                entrante.TurnoId = turno.Id;
                if (turno.Estado == CteEstadosTurno.Pendiente)
                    turno.Estado = CteEstadosTurno.Confirmado;
                Responder(tenantId, turno, CteMensajesWhatsapp.TipoConfirmacionRecibida, turno.Paciente.Celular,
                    TextosWhatsapp.ConfirmacionRecibida(turno), ahora);
                return CteRespuestasWhatsapp.Confirmo;

            case IntencionRespuesta.Cancelar:
                entrante.TurnoId = turno.Id;
                turno.Estado = CteEstadosTurno.Cancelado;
                turno.CanceladoPor = CteEstadosTurno.CanceladoPorPaciente;
                turno.MotivoCancelacion = MotivoCancelacion;
                Responder(tenantId, turno, CteMensajesWhatsapp.TipoCancelacionRecibida, turno.Paciente.Celular,
                    TextosWhatsapp.CancelacionRecibida(turno), ahora);

                foreach (var celular in await respuestas.CelularesAdminsAsync(tenantId, ct))
                    Responder(tenantId, turno, CteMensajesWhatsapp.TipoAvisoProfesional, celular,
                        TextosWhatsapp.AvisoProfesional(turno), ahora);
                return CteRespuestasWhatsapp.Cancelo;

            default:
                // Un "gracias" o una pregunta no se contestan solos: lo ve el profesional en la ficha.
                entrante.TurnoId = turno.Id;
                return CteRespuestasWhatsapp.NoEntendido;
        }
    }

    private void Responder(int tenantId, Turno turno, string tipo, string destino, string texto, DateTime ahora)
        => salientes.Agregar(new MensajeWhatsapp
        {
            TenantId = tenantId,
            TurnoId = turno.Id,
            Tipo = tipo,
            Destino = destino,
            Texto = texto,
            Estado = CteMensajesWhatsapp.Pendiente,
            ProximoIntentoEn = ahora,
            CreadoEn = ahora,
        });
}
