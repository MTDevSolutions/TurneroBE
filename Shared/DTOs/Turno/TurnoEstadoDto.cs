using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Turno;

public class TurnoEstadoDto
{
    /// <summary>confirmado | pendiente | cancelado | atendido | ausente</summary>
    [Required, MaxLength(20)]
    public string Estado { get; set; } = null!;

    /// <summary>Solo para cancelado (opcional).</summary>
    [MaxLength(250)]
    public string? Motivo { get; set; }
}
