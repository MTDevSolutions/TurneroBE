using Shared.Constantes;

namespace Application.UseCases.Turnos;

/// <summary>
/// Transiciones válidas del turno. Cancelado, atendido y ausente son finales: un turno cancelado
/// no se "descancela" (se carga uno nuevo), así el historial de cancelaciones queda limpio.
/// </summary>
public static class MaquinaEstadosTurno
{
    private static readonly Dictionary<string, string[]> Transiciones = new()
    {
        [CteEstadosTurno.Pendiente]  = [CteEstadosTurno.Confirmado, CteEstadosTurno.Cancelado, CteEstadosTurno.Atendido, CteEstadosTurno.Ausente],
        [CteEstadosTurno.Confirmado] = [CteEstadosTurno.Pendiente, CteEstadosTurno.Cancelado, CteEstadosTurno.Atendido, CteEstadosTurno.Ausente],
        [CteEstadosTurno.Cancelado]  = [],
        [CteEstadosTurno.Atendido]   = [],
        [CteEstadosTurno.Ausente]    = [],
    };

    public static bool EsEstadoConocido(string estado) => Transiciones.ContainsKey(estado);

    public static bool EsTransicionValida(string desde, string hacia)
        => Transiciones.TryGetValue(desde, out var permitidos) && permitidos.Contains(hacia);

    /// <summary>Atendido y ausente solo tienen sentido una vez que el turno empezó.</summary>
    public static bool RequiereTurnoEmpezado(string hacia)
        => hacia is CteEstadosTurno.Atendido or CteEstadosTurno.Ausente;

    /// <summary>Se puede mover de horario o editar mientras no esté cerrado.</summary>
    public static bool EsEditable(string estado)
        => estado is CteEstadosTurno.Pendiente or CteEstadosTurno.Confirmado;
}
