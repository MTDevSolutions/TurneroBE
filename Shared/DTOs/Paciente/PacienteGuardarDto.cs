using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Paciente;

/// <summary>Alta y edición de paciente (mismo contrato).</summary>
public class PacienteGuardarDto
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Apellido { get; set; } = null!;

    /// <summary>Texto libre (ej. "11 2345-6789"); se normaliza al formato de WhatsApp.</summary>
    [Required, MaxLength(30)]
    public string Celular { get; set; } = null!;

    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>Notas administrativas. No cargar datos clínicos.</summary>
    [MaxLength(500)]
    public string? Notas { get; set; }
}
