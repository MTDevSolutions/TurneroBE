namespace Shared.Constantes;

/// <summary>
/// Nombres de los índices únicos que el código traduce a un mensaje de negocio.
/// Viven acá y no en la configuración de EF para que el nombre que se declara en
/// <c>TurneroContext</c> y el que se busca al traducir el duplicado no puedan
/// separarse con un rename.
/// </summary>
public static class CteIndices
{
    /// <summary>Único entre no nulos: un teléfono del gateway pertenece a una sola cuenta.</summary>
    public const string TenantWhatsappTelefono = "UX_Tenants_WhatsappTelefonoId";

    /// <summary>Email único global de usuario (identificador del login).</summary>
    public const string UsuarioEmail = "IX_Usuarios_Email";
}
