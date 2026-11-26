using Application.Interfaces;
using Domain.Entities;

namespace Tests.Fakes;

/// <summary>Fake in-memory de IPacienteRepository (una sola cuenta: el filtro de tenant ya está aplicado).</summary>
public class PacienteRepositoryFake : IPacienteRepository
{
    public List<Paciente> Pacientes { get; } = [];

    public Paciente Agregar(string nombre, string apellido, string celular, bool activo = true)
    {
        var p = new Paciente { Id = Pacientes.Count + 1, Nombre = nombre, Apellido = apellido, Celular = celular, Activo = activo };
        Pacientes.Add(p);
        return p;
    }

    public Task<(List<Paciente> Items, int Total)> ListarAsync(int page, int pageSize, string? buscar, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        var q = Pacientes.Where(p => incluirInactivos || p.Activo)
            .Where(p => string.IsNullOrWhiteSpace(buscar)
                        || $"{p.Nombre} {p.Apellido}".Contains(buscar, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Apellido).ThenBy(p => p.Nombre).ToList();
        return Task.FromResult((q.Skip((page - 1) * pageSize).Take(pageSize).ToList(), q.Count));
    }

    public Task<Paciente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Pacientes.FirstOrDefault(p => p.Id == id));

    public Task<bool> CelularEnUsoAsync(string celular, int? excluirId, CancellationToken cancellationToken = default)
        => Task.FromResult(Pacientes.Any(p => p.Celular == celular && p.Id != excluirId));

    public void Agregar(Paciente paciente)
    {
        paciente.Id = Pacientes.Count + 1;
        Pacientes.Add(paciente);
    }
}
