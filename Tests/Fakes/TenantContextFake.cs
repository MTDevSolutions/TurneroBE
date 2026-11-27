using Application.UseCases.Security_Context;

namespace Tests.Fakes;

/// <summary>Contexto de tenant fijo, para servicios y para armar un TurneroContext en tests.</summary>
public class TenantContextFake(int? tenantId = null, bool esSuperadmin = false, string? rol = "admin") : ITenantContext
{
    public int? TenantId { get; } = tenantId;
    public string? Rol { get; } = tenantId is null ? null : rol;
    public bool EsSuperadmin { get; } = esSuperadmin;
}
