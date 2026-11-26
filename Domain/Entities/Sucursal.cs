using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Sucursal de un negocio: en el turnero, cada consultorio donde atiende el profesional.
/// Entidad de dominio: se filtra por tenant.
/// </summary>
public class Sucursal : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Nombre { get; set; } = null!;

    /// <summary>Dirección del consultorio; va en los avisos al paciente.</summary>
    public string? Direccion { get; set; }

    public bool Activa { get; set; } = true;
}
