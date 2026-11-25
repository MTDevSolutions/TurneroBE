namespace Shared.DTOs.Turno;

public class TurnoDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = null!;
    public string PacienteCelular { get; set; } = null!;
    public int SucursalId { get; set; }
    public string SucursalNombre { get; set; } = null!;

    /// <summary>En UTC (el front lo muestra en la hora local).</summary>
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fin { get; set; }

    public string Estado { get; set; } = null!;
    public string? CanceladoPor { get; set; }
    public string? MotivoCancelacion { get; set; }
    public string? Notas { get; set; }
}
