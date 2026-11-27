using Application.Interfaces;
using Domain.Entities;

namespace Tests.Fakes;

/// <summary>Fake in-memory de INotaConsultaRepository (una sola cuenta: el filtro de tenant ya está aplicado).</summary>
public class NotaConsultaRepositoryFake : INotaConsultaRepository
{
    public List<NotaConsulta> Notas { get; } = [];
    public List<NotaConsultaVersion> Versiones { get; } = [];

    public Task<List<NotaConsulta>> ListarPorPacienteAsync(int pacienteId, bool incluirAnuladas, CancellationToken cancellationToken = default)
        => Task.FromResult(Notas
            .Where(n => n.PacienteId == pacienteId && (incluirAnuladas || !n.Anulada))
            .OrderByDescending(n => n.CreadaEn).ThenByDescending(n => n.Id)
            .ToList());

    public Task<NotaConsulta?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Notas.FirstOrDefault(n => n.Id == id));

    public Task<List<NotaConsultaVersion>> ListarVersionesAsync(int notaId, CancellationToken cancellationToken = default)
        => Task.FromResult(Versiones.Where(v => v.NotaConsultaId == notaId).OrderByDescending(v => v.Version).ToList());

    public Task<Dictionary<int, int>> ContarPorTurnoAsync(IReadOnlyCollection<int> turnoIds, CancellationToken cancellationToken = default)
        => Task.FromResult(Notas
            .Where(n => n.TurnoId is int t && turnoIds.Contains(t) && !n.Anulada)
            .GroupBy(n => n.TurnoId!.Value)
            .ToDictionary(g => g.Key, g => g.Count()));

    public void Agregar(NotaConsulta nota)
    {
        nota.Id = Notas.Count + 1;
        Notas.Add(nota);
    }

    public void AgregarVersion(NotaConsultaVersion version)
    {
        version.Id = Versiones.Count + 1;
        Versiones.Add(version);
    }
}
