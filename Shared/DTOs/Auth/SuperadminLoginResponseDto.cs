namespace Shared.DTOs.Auth;

public class SuperadminLoginResponseDto
{
    public string Token { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTimeOffset ExpiraEn { get; set; }
}
