using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Turno;

/// <summary>Alta y reprogramación de un turno (mismo contrato).</summary>
public class TurnoGuardarDto
{
    [Range(1, int.MaxValue)]
    public int PacienteId { get; set; }

    /// <summary>Consultorio. Opcional si la cuenta tiene un solo consultorio activo.</summary>
    public int? SucursalId { get; set; }

    /// <summary>Con offset (ej. "2026-12-01T15:00:00-03:00").</summary>
    [Required]
    public DateTimeOffset Inicio { get; set; }

    /// <summary>Opcional: si no viene, se usa la duración por defecto de la configuración.</summary>
    [Range(5, 480)]
    public int? DuracionMinutos { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }
}
