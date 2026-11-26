using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Encola los avisos al paciente que dispara la agenda (alta, cambio de horario, cancelación).</summary>
public interface IAvisoTurnoService
{
    /// <summary>
    /// Agrega al scope el mensaje del tipo indicado (CteMensajesWhatsapp.Tipo*) para el paciente del
    /// turno, si la cuenta tiene los avisos activos y el turno todavía no empezó. No guarda: se
    /// commitea con el cambio del turno, así no hay turno sin aviso ni aviso sin turno.
    /// </summary>
    Task EncolarAsync(Turno turno, string tipo, CancellationToken cancellationToken = default);
}
