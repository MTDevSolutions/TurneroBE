namespace Shared.Constantes;

/// <summary>Estados del turno. Texto (no enum de PG) para poder evolucionarlos.</summary>
public static class CteEstadosTurno
{
    /// <summary>Cargado, el paciente todavía no confirmó.</summary>
    public const string Pendiente = "pendiente";
    public const string Confirmado = "confirmado";
    public const string Cancelado = "cancelado";
    public const string Atendido = "atendido";
    public const string Ausente = "ausente";

    /// <summary>Estados que ocupan el horario (todo menos cancelado).</summary>
    public static readonly string[] Vivos = [Pendiente, Confirmado, Atendido, Ausente];

    /// <summary>Quién canceló el turno.</summary>
    public const string CanceladoPorProfesional = "profesional";
    public const string CanceladoPorPaciente = "paciente";
}
