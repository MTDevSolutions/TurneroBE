using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Notas de consulta de la cuenta. No hay borrado: la base lo impide con un trigger.</summary>
public interface INotaConsultaRepository
{
    /// <summary>Notas del paciente, de la más nueva a la más vieja, con su turno.</summary>
    Task<List<NotaConsulta>> ListarPorPacienteAsync(int pacienteId, bool incluirAnuladas, CancellationToken cancellationToken = default);

    /// <summary>Nota de la cuenta (trackeada, con su turno); null si no existe.</summary>
    Task<NotaConsulta?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Versiones anteriores de la nota, de la más nueva a la más vieja.</summary>
    Task<List<NotaConsultaVersion>> ListarVersionesAsync(int notaId, CancellationToken cancellationToken = default);

    /// <summary>Cantidad de notas no anuladas de cada turno. Los turnos sin notas no aparecen.</summary>
    Task<Dictionary<int, int>> ContarPorTurnoAsync(IReadOnlyCollection<int> turnoIds, CancellationToken cancellationToken = default);

    void Agregar(NotaConsulta nota);
    void AgregarVersion(NotaConsultaVersion version);
}
