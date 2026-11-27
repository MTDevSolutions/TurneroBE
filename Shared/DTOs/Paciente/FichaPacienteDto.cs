using Shared.DTOs.Turno;

namespace Shared.DTOs.Paciente;

/// <summary>Ficha del paciente: datos, resumen de su historia de turnos y la lista de turnos
/// (del más nuevo al más viejo, cada uno con su cantidad de notas). Las notas van aparte
/// (GET /api/pacientes/{id}/notas) porque solo las ve el profesional.</summary>
public class FichaPacienteDto
{
    public PacienteDto Paciente { get; set; } = null!;
    public int TotalTurnos { get; set; }
    public int Atendidos { get; set; }
    public int Ausentes { get; set; }
    public int Cancelados { get; set; }

    /// <summary>Próximo turno vivo (pendiente o confirmado) que todavía no empezó.</summary>
    public TurnoDto? ProximoTurno { get; set; }

    public List<TurnoDto> Turnos { get; set; } = [];
}
