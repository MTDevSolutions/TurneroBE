using Application.Interfaces;
using Domain.Entities;
using Shared.Constantes;

namespace Tests.Fakes;

/// <summary>Fake de IRespuestaWhatsappRepository: filtra siempre por el tenant recibido, como el real.</summary>
public class RespuestaWhatsappRepositoryFake : IRespuestaWhatsappRepository
{
    public List<Paciente> Pacientes { get; } = [];
    public List<Turno> Turnos { get; } = [];
    public List<Usuario> Usuarios { get; } = [];

    public Task<Paciente?> PacientePorCelularAsync(int tenantId, string celular, CancellationToken cancellationToken = default)
        => Task.FromResult(Pacientes.FirstOrDefault(p => p.TenantId == tenantId && p.Celular == celular && p.Activo));

    public Task<Turno?> ProximoTurnoVivoAsync(int tenantId, int pacienteId, DateTime ahoraUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(Turnos
            .Where(t => t.TenantId == tenantId && t.PacienteId == pacienteId
                && (t.Estado == CteEstadosTurno.Pendiente || t.Estado == CteEstadosTurno.Confirmado)
                && t.Inicio > ahoraUtc)
            .OrderBy(t => t.Inicio)
            .FirstOrDefault());

    public Task<List<string>> CelularesAdminsAsync(int tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(Usuarios
            .Where(u => u.TenantId == tenantId && u.Activo && u.Rol == CteRoles.Admin && u.Celular != null)
            .Select(u => u.Celular!)
            .Distinct()
            .ToList());
}
