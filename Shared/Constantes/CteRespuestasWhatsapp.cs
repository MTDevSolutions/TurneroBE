namespace Shared.Constantes;

/// <summary>Cómo se interpretó un mensaje entrante de un paciente.</summary>
public static class CteRespuestasWhatsapp
{
    /// <summary>Respondió que sí: el turno quedó confirmado.</summary>
    public const string Confirmo = "confirmo";
    /// <summary>Respondió que no: el turno se canceló.</summary>
    public const string Cancelo = "cancelo";
    /// <summary>Escribió otra cosa: no se toca el turno.</summary>
    public const string NoEntendido = "no_entendido";
    /// <summary>El número no es de un paciente de la cuenta.</summary>
    public const string PacienteDesconocido = "paciente_desconocido";
    /// <summary>Es de un paciente, pero no tiene turnos próximos que confirmar o cancelar.</summary>
    public const string SinTurno = "sin_turno";
}
