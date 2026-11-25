using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Turno de un paciente. Entidad de dominio: se filtra por tenant. Inicio y Fin en UTC.
/// Dos turnos vivos (no cancelados) de la misma cuenta no se pueden superponer: lo garantiza un
/// exclusion constraint de PostgreSQL además de la validación del servicio.
/// </summary>
public class Turno : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PacienteId { get; set; }

    /// <summary>Consultorio donde se atiende.</summary>
    public int SucursalId { get; set; }

    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    /// <summary>pendiente | confirmado | cancelado | atendido | ausente (ver CteEstadosTurno).</summary>
    public string Estado { get; set; } = "pendiente";

    /// <summary>Quién canceló: profesional | paciente. Null si no está cancelado.</summary>
    public string? CanceladoPor { get; set; }

    public string? MotivoCancelacion { get; set; }

    /// <summary>Notas administrativas del turno. No es historia clínica.</summary>
    public string? Notas { get; set; }

    public DateTime FechaAlta { get; set; }

    /// <summary>Cuándo se encoló el recordatorio (o el aviso, si ya caía dentro de la ventana del
    /// recordatorio). Null = falta mandarlo. Mover el turno lo vuelve a null.</summary>
    public DateTime? RecordatorioEncoladoEn { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Sucursal Sucursal { get; set; } = null!;
}
