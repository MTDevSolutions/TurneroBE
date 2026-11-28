using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Configuracion;

public class ColorPrincipalDto
{
    /// <summary>rosa | lavanda | coral | salvia | celeste.</summary>
    [Required]
    public string Color { get; set; } = null!;
}
