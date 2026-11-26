using Application.Exceptions;
using Application.Interfaces;
using Shared.DTOs.Whatsapp;

namespace Application.Services;

/// <summary>Historial de WhatsApp de un turno: lo que salió y lo que respondió el paciente, mezclado por fecha.</summary>
public class HistorialWhatsappService(
    ITurnoRepository turnos,
    IMensajeWhatsappRepository salientes,
    IMensajeEntranteRepository entrantes) : IHistorialWhatsappService
{
    public async Task<List<MensajeWhatsappDto>> ListarPorTurnoAsync(int turnoId, CancellationToken cancellationToken = default)
    {
        // Con el query filter, un turno de otra cuenta "no existe": 404 y no una lista vacía engañosa.
        _ = await turnos.GetByIdAsync(turnoId, cancellationToken) ?? throw AppException.RegistroNoExiste();

        var enviados = (await salientes.ListarPorTurnoAsync(turnoId, cancellationToken)).Select(m => new MensajeWhatsappDto
        {
            Id = m.Id,
            Direccion = "saliente",
            Tipo = m.Tipo,
            Texto = m.Texto,
            Estado = m.Estado,
            Error = m.UltimoError,
            Fecha = Utc(m.EnviadoEn ?? m.CreadoEn),
        });

        var recibidos = (await entrantes.ListarPorTurnoAsync(turnoId, cancellationToken)).Select(m => new MensajeWhatsappDto
        {
            Id = m.Id,
            Direccion = "entrante",
            Tipo = m.Resultado,
            Texto = m.Texto,
            Estado = "recibido",
            Fecha = Utc(m.RecibidoEn),
        });

        return enviados.Concat(recibidos).OrderBy(m => m.Fecha).ThenBy(m => m.Id).ToList();
    }

    private static DateTimeOffset Utc(DateTime fecha) => new(DateTime.SpecifyKind(fecha, DateTimeKind.Utc));
}
