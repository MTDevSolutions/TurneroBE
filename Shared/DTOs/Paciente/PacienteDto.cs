namespace Shared.DTOs.Paciente;

public class PacienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;

    /// <summary>Normalizado: 549 + área + número.</summary>
    public string Celular { get; set; } = null!;
    public string? Email { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }
}
