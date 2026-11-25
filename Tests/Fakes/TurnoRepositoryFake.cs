using Application.Interfaces;
using Domain.Entities;
using Shared.Constantes;

namespace Tests.Fakes;

/// <summary>Fake in-memory de ITurnoRepository con la misma regla de solapamiento que el real.</summary>
public class TurnoRepositoryFake : ITurnoRepository
{
    public List<Turno> Turnos { get; } = [];

    public Task<List<Turno>> ListarRangoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos.Where(t => t.Inicio < hastaUtc && t.Fin > desdeUtc).OrderBy(t => t.Inicio).ToList());

    public Task<Turno?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos.FirstOrDefault(t => t.Id == id));

    public Task<bool> HaySolapamientoAsync(DateTime inicioUtc, DateTime finUtc, int? excluirId, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos.Any(t =>
            t.Estado != CteEstadosTurno.Cancelado && t.Id != excluirId && t.Inicio < finUtc && t.Fin > inicioUtc));

    public void Agregar(Turno turno)
    {
        turno.Id = Turnos.Count + 1;
        Turnos.Add(turno);
    }
}
