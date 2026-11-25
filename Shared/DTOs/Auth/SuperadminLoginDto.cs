using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Auth;

public class SuperadminLoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
