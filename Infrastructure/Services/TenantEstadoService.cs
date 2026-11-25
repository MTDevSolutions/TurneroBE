using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services;

/// <summary>
/// Lee el estado de un tenant y la versión de términos que aceptó. Tenant es entidad de
/// plataforma: no lleva global query filter, así que la consulta es directa. Los dos datos
/// salen de una sola consulta y se cachean unos minutos para no pegarle a la DB en cada request.
/// </summary>
public class TenantEstadoService : ITenantEstadoService
{
    private const string CacheKey = "tenant_acceso_{0}";
    private const int CacheMinutos = 2;

    private readonly TurneroContext _context;
    private readonly IMemoryCache _memoryCache;

    public TenantEstadoService(TurneroContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _memoryCache = memoryCache;
    }

    public async Task<string?> ObtenerEstadoAsync(int tenantId, CancellationToken cancellationToken = default)
        => (await ObtenerAccesoAsync(tenantId, cancellationToken))?.Estado;

    public async Task<string?> ObtenerVersionTerminosAsync(int tenantId, CancellationToken cancellationToken = default)
        => (await ObtenerAccesoAsync(tenantId, cancellationToken))?.TerminosVersion;

    public void InvalidarCache(int tenantId)
        => _memoryCache.Remove(string.Format(CacheKey, tenantId));

    private async Task<AccesoTenant?> ObtenerAccesoAsync(int tenantId, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(CacheKey, tenantId);

        if (_memoryCache.TryGetValue(cacheKey, out AccesoTenant? cacheado))
            return cacheado;

        var acceso = await _context.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => new AccesoTenant(t.Estado, t.TerminosVersionAceptada))
            .FirstOrDefaultAsync(cancellationToken);

        _memoryCache.Set(cacheKey, acceso,
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheMinutos)));

        return acceso;
    }

    private sealed record AccesoTenant(string Estado, string? TerminosVersion);
}
