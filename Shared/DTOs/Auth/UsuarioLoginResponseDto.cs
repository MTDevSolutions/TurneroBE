namespace Shared.DTOs.Auth;

public class UsuarioLoginResponseDto
{
    public string Token { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Rol { get; set; } = null!;
    public int TenantId { get; set; }
    public DateTimeOffset ExpiraEn { get; set; }

    /// <summary>True si la cuenta no aceptó los términos vigentes: el front muestra la pantalla
    /// de aceptación antes que nada (el resto de la API responde 403 TERMINOS_PENDIENTES).</summary>
    public bool TerminosPendientes { get; set; }

    /// <summary>Paleta del color principal de la cuenta, para pintar la app desde el login.</summary>
    public string ColorPrincipal { get; set; } = null!;
}
