namespace Shared.DTOs.Terminos;

public class TerminosDto
{
    public string Version { get; set; } = null!;
    public string Texto { get; set; } = null!;

    /// <summary>True si la cuenta ya aceptó esta versión.</summary>
    public bool Aceptados { get; set; }
}
