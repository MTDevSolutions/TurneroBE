namespace Domain.Entities;

/// <summary>
/// Dueño del SaaS (plano plataforma). Vive fuera de los tenants: sin tenant_id,
/// no es un rol del modelo de roles del negocio.
/// </summary>
public class Superadmin
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}
