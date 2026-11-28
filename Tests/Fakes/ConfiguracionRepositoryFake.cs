using Application.Interfaces;
using Domain.Entities;

namespace Tests.Fakes;

/// <summary>Fake in-memory de IConfiguracionRepository: a lo sumo una fila, como en la base.</summary>
public class ConfiguracionRepositoryFake(Configuracion? existente = null) : IConfiguracionRepository
{
    public Configuracion? Actual { get; private set; } = existente;

    public Task<Configuracion?> ObtenerAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Actual);

    public Task<string?> ObtenerColorAsync(int tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(Actual?.TenantId == tenantId ? Actual.ColorPrincipal : null);

    public void Agregar(Configuracion configuracion) => Actual = configuracion;
}
