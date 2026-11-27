using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Usuario de una cuenta (ver CteRoles). Entidad de dominio: se filtra por tenant.
/// El Email es único GLOBAL (sirve de identificador en el login multi-tenant).
/// </summary>
public class Usuario : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int SucursalId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;

    /// <summary>admin | secretaria</summary>
    public string Rol { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;
    public bool Activo { get; set; } = true;

    /// <summary>Celular personal, normalizado para WhatsApp (549 + área + número).
    /// Ahí se le avisa al profesional cuando un paciente cancela.</summary>
    public string? Celular { get; set; }

    public Sucursal Sucursal { get; set; } = null!;
}
