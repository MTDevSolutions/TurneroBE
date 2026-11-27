using Application.Interfaces;
using Domain.Entities;

namespace Tests.Fakes;

/// <summary>Registra qué avisos pidió encolar la agenda.</summary>
public class AvisoTurnoServiceFake : IAvisoTurnoService
{
    public List<(Turno Turno, string Tipo)> Encolados { get; } = [];

    public Task EncolarAsync(Turno turno, string tipo, CancellationToken cancellationToken = default)
    {
        Encolados.Add((turno, tipo));
        return Task.CompletedTask;
    }
}
