using Domain.Entities;

namespace Application.Interfaces;

public interface ITurnoRepository
{
    /// <summary>Turnos de la cuenta que se cruzan con [desde, hasta), con paciente y consultorio.
    /// Incluye los cancelados (el calendario los muestra tachados).</summary>
    Task<List<Turno>> ListarRangoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default);

    /// <summary>Turno de la cuenta (trackeado, con paciente y consultorio); null si no existe.</summary>
    Task<Turno?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>True si algún turno vivo de la cuenta se superpone con [inicio, fin).</summary>
    Task<bool> HaySolapamientoAsync(DateTime inicioUtc, DateTime finUtc, int? excluirId, CancellationToken cancellationToken = default);

    void Agregar(Turno turno);

    /// <summary>
    /// Turnos de TODAS las cuentas a los que les toca el recordatorio: vivos, que no empezaron, sin
    /// recordatorio encolado y que ya entraron en la ventana de horas de su cuenta (con los avisos
    /// activos). Trackeados, con paciente y consultorio. Lo usa el worker de recordatorios.
    /// </summary>
    Task<List<Turno>> ListarParaRecordatorioAsync(DateTime ahoraUtc, int maximo, CancellationToken cancellationToken = default);
}
