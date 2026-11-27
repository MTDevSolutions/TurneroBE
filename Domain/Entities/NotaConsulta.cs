using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Nota de consulta que el profesional escribe sobre un paciente, atada o no a un turno atendido.
/// Entidad de dominio: se filtra por tenant. Nunca se borra (lo impide un trigger de la base):
/// se edita solo dentro de las primeras horas (CteNotasConsulta.HorasEdicion), guardando cada versión
/// anterior en <see cref="NotaConsultaVersion"/>, y después solo se puede anular con un motivo.
/// </summary>
public class NotaConsulta : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PacienteId { get; set; }

    /// <summary>Turno atendido al que corresponde. Null = nota suelta (una llamada, una novedad).</summary>
    public int? TurnoId { get; set; }

    /// <summary>Texto vigente (versión <see cref="Version"/>).</summary>
    public string Texto { get; set; } = null!;

    /// <summary>Arranca en 1 y suma uno con cada edición.</summary>
    public int Version { get; set; } = 1;

    /// <summary>UTC. Marca el inicio de la ventana de edición.</summary>
    public DateTime CreadaEn { get; set; }

    /// <summary>Id del usuario que la escribió.</summary>
    public int CreadaPor { get; set; }

    public DateTime? EditadaEn { get; set; }
    public int? EditadaPor { get; set; }

    public bool Anulada { get; set; }
    public DateTime? AnuladaEn { get; set; }
    public int? AnuladaPor { get; set; }
    public string? MotivoAnulacion { get; set; }

    public Turno? Turno { get; set; }
}
