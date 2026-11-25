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
}
