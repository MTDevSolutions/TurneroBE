using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Terminos;

/// <summary>
/// Aceptación explícita. La versión viaja en el request para que nadie acepte, sin verlo,
/// un texto que cambió mientras tenía la pantalla abierta.
/// </summary>
public class TerminosAceptarDto
{
    [Required, MaxLength(20)]
    public string Version { get; set; } = null!;
}
