using Application.Interfaces;
using Shared.Constantes;

namespace Tests.Fakes;

/// <summary>
/// Fake de ITenantEstadoService: devuelve siempre el estado y la versión de términos con que se
/// lo construyó (por defecto, activo y con los términos vigentes aceptados).
/// </summary>
public class TenantEstadoServiceFake(string? estado = "activo", string? versionTerminos = CteTerminos.VersionVigente)
    : ITenantEstadoService
{
    public List<int> Invalidados { get; } = [];

    public Task<string?> ObtenerEstadoAsync(int tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(estado);

    public Task<string?> ObtenerVersionTerminosAsync(int tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(versionTerminos);

    public void InvalidarCache(int tenantId) => Invalidados.Add(tenantId);
}
