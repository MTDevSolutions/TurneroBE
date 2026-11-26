namespace Domain.Entities.Abstractions;

/// <summary>
/// Marca una entidad como perteneciente a un tenant. El <c>TurneroContext</c>
/// aplica automáticamente el global query filter por <see cref="TenantId"/> y
/// autocompleta el valor en los inserts. Las entidades de plataforma
/// (TENANT, PAGO_SUSCRIPCION, SUPERADMIN) NO implementan esta interfaz.
/// </summary>
public interface ITenantEntity
{
    int TenantId { get; set; }
}
