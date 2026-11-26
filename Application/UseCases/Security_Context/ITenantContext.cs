namespace Application.UseCases.Security_Context;

/// <summary>
/// Contexto de tenant del request actual, resuelto desde los claims del JWT.
/// </summary>
public interface ITenantContext
{
    /// <summary>Id del tenant del usuario autenticado; null si es superadmin o no hay auth.</summary>
    int? TenantId { get; }

    /// <summary>Rol del usuario de negocio (ver CteRoles); null si es superadmin o no hay auth.</summary>
    string? Rol { get; }

    /// <summary>True si el request corresponde al plano plataforma (superadmin).</summary>
    bool EsSuperadmin { get; }
}
