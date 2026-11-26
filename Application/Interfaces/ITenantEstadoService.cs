namespace Application.Interfaces;

/// <summary>
/// Provee el estado de un tenant (entidad de plataforma) para el bloqueo por suspensión
/// y por términos de uso sin aceptar.
/// </summary>
public interface ITenantEstadoService
{
    /// <summary>Devuelve el estado del tenant (activo/suspendido/cancelado) o null si no existe.</summary>
    Task<string?> ObtenerEstadoAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Versión de términos que aceptó el tenant; null si no aceptó ninguna o no existe.</summary>
    Task<string?> ObtenerVersionTerminosAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Limpia el caché del tenant (llamar al suspender/reactivar o al aceptar términos).</summary>
    void InvalidarCache(int tenantId);
}
