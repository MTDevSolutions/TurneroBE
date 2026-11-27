using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Lecturas que necesita el webhook de respuestas. El webhook no tiene una cuenta en contexto (lo
/// llama el gateway, no un usuario), así que cada consulta recibe el tenant de forma explícita y
/// solo ve datos de ese tenant. Concentrarlas acá deja a la vista todo acceso fuera del query filter.
/// </summary>
public interface IRespuestaWhatsappRepository
{
    /// <summary>Paciente activo de la cuenta con ese celular normalizado; null si no hay.</summary>
    Task<Paciente?> PacientePorCelularAsync(int tenantId, string celular, CancellationToken cancellationToken = default);

    /// <summary>Próximo turno vivo (pendiente o confirmado) del paciente que todavía no empezó,
    /// trackeado y con paciente y consultorio; null si no tiene.</summary>
    Task<Turno?> ProximoTurnoVivoAsync(int tenantId, int pacienteId, DateTime ahoraUtc, CancellationToken cancellationToken = default);

    /// <summary>Celulares personales cargados de los admins activos de la cuenta.</summary>
    Task<List<string>> CelularesAdminsAsync(int tenantId, CancellationToken cancellationToken = default);
}
