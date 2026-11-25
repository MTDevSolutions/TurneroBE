namespace Shared.Constantes;

/// <summary>
/// Nombres de claims y valores usados en el JWT para el modelo de acceso multi-tenant.
/// </summary>
public static class CteSeguridad
{
    // --- Claims ---
    public const string ClaimTenantId = "tenant_id";
    public const string ClaimRol = "rol";
    public const string ClaimTipoAcceso = "tipo_acceso";

    // --- Valores de tipo_acceso ---
    public const string TipoAccesoPlataforma = "plataforma"; // superadmin (sin tenant_id)
    public const string TipoAccesoTenant = "tenant";         // usuario de negocio
}
