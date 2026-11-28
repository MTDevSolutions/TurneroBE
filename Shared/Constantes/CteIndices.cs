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

    /// <summary>Único (TenantId, Celular): un celular identifica a un solo paciente de la cuenta.</summary>
    public const string PacienteCelular = "UX_Pacientes_TenantId_Celular";

    /// <summary>Exclusion constraint: dos turnos vivos de la misma cuenta no se superponen.</summary>
    public const string TurnoSinSolapamiento = "EX_Turnos_SinSolapamiento";

    /// <summary>Único: el gateway reintenta el webhook, y una misma respuesta no se procesa dos veces.</summary>
    public const string MensajeEntranteGateway = "UX_MensajesEntrantes_GatewayMensajeId";

    /// <summary>Único (TenantId, Clave): una sola versión propia de cada mensaje por cuenta.</summary>
    public const string PlantillaMensajeClave = "UX_PlantillasMensaje_TenantId_Clave";
}
