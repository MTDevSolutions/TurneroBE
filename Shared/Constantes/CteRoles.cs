namespace Shared.Constantes;

/// <summary>Roles de los usuarios de una cuenta (claim "rol" del JWT).</summary>
public static class CteRoles
{
    /// <summary>El profesional dueño de la cuenta.</summary>
    public const string Admin = "admin";

    /// <summary>Reservado para una etapa posterior: maneja la agenda, no la cuenta.</summary>
    public const string Secretaria = "secretaria";
}
