using Application.UseCases.Security_Context;
using Microsoft.AspNetCore.Http;
using Shared.Constantes;
using System.Security.Claims;

namespace Infrastructure.Security;

/// <summary>
/// Implementación de <see cref="ITenantContext"/> que lee los claims multi-tenant
/// (tenant_id, rol, tipo_acceso) desde el HttpContext del request autenticado.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? TenantId
    {
        get
        {
            var value = User?.FindFirst(CteSeguridad.ClaimTenantId)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Rol => User?.FindFirst(CteSeguridad.ClaimRol)?.Value;

    public bool EsSuperadmin =>
        string.Equals(
            User?.FindFirst(CteSeguridad.ClaimTipoAcceso)?.Value,
            CteSeguridad.TipoAccesoPlataforma,
            StringComparison.OrdinalIgnoreCase);
}
