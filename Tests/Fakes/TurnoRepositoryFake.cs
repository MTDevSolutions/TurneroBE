using Application.Interfaces;
using Domain.Entities;
using Shared.Constantes;

namespace Tests.Fakes;

/// <summary>Fake in-memory de ITurnoRepository con la misma regla de solapamiento que el real.</summary>
public class TurnoRepositoryFake : ITurnoRepository
{
    public List<Turno> Turnos { get; } = [];

    /// <summary>Configuración de cada cuenta, para la ventana del recordatorio.</summary>
    public Dictionary<int, Configuracion> Configuraciones { get; } = [];

    public Task<List<Turno>> ListarRangoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos.Where(t => t.Inicio < hastaUtc && t.Fin > desdeUtc).OrderBy(t => t.Inicio).ToList());

    public Task<Turno?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos.FirstOrDefault(t => t.Id == id));

    public Task<List<Turno>> ListarPorPacienteAsync(int pacienteId, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos.Where(t => t.PacienteId == pacienteId).OrderByDescending(t => t.Inicio).ToList());

    public Task<bool> HaySolapamientoAsync(DateTime inicioUtc, DateTime finUtc, int? excluirId, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos.Any(t =>
            t.Estado != CteEstadosTurno.Cancelado && t.Id != excluirId && t.Inicio < finUtc && t.Fin > inicioUtc));

    public void Agregar(Turno turno)
    {
        turno.Id = Turnos.Count + 1;
        Turnos.Add(turno);
    }

    public Task<List<Turno>> ListarParaRecordatorioAsync(DateTime ahoraUtc, int maximo, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos
            .Where(t => Configuraciones.TryGetValue(t.TenantId, out var c)
                && c.AvisosWhatsappActivos
                && c.HorasRecordatorio > 0
                && t.RecordatorioEncoladoEn == null
                && (t.Estado == CteEstadosTurno.Pendiente || t.Estado == CteEstadosTurno.Confirmado)
                && t.Inicio > ahoraUtc
                && t.Inicio.AddHours(-c.HorasRecordatorio) <= ahoraUtc)
            .OrderBy(t => t.Inicio)
            .Take(maximo)
            .ToList());
}
