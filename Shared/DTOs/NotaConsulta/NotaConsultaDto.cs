namespace Shared.DTOs.NotaConsulta;

public class NotaConsultaDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public int? TurnoId { get; set; }

    /// <summary>Inicio del turno asociado (UTC); null en las notas sueltas.</summary>
    public DateTimeOffset? TurnoInicio { get; set; }

    public string Texto { get; set; } = null!;
    public int Version { get; set; }
    public DateTimeOffset CreadaEn { get; set; }
    public DateTimeOffset? EditadaEn { get; set; }

    /// <summary>Hasta cuándo se puede editar (UTC).</summary>
    public DateTimeOffset EditableHasta { get; set; }

    /// <summary>True si todavía se puede editar: dentro de la ventana y sin anular.</summary>
    public bool Editable { get; set; }

    public bool Anulada { get; set; }
    public DateTimeOffset? AnuladaEn { get; set; }
    public string? MotivoAnulacion { get; set; }
}
