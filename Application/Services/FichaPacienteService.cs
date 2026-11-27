using Application.Exceptions;
using Application.Interfaces;
using Shared.Constantes;
using Shared.DTOs.Paciente;

namespace Application.Services;

/// <summary>
/// Ficha del paciente: sus datos, un resumen de cómo le fue con los turnos y la lista completa,
/// cada turno con la cantidad de notas de consulta (el texto de las notas se pide aparte).
/// </summary>
public class FichaPacienteService(
    IPacienteRepository pacientes,
    ITurnoRepository turnos,
    INotaConsultaRepository notas,
    TimeProvider reloj) : IFichaPacienteService
{
    public async Task<FichaPacienteDto> ObtenerAsync(int pacienteId, CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.GetByIdAsync(pacienteId, cancellationToken)
            ?? throw AppException.RegistroNoExiste();

        var lista = await turnos.ListarPorPacienteAsync(pacienteId, cancellationToken);
        var cantidades = await notas.ContarPorTurnoAsync(lista.Select(t => t.Id).ToList(), cancellationToken);
        var ahora = reloj.GetUtcNow().UtcDateTime;

        var proximo = lista
            .Where(t => t.Inicio > ahora && t.Estado is CteEstadosTurno.Pendiente or CteEstadosTurno.Confirmado)
            .MinBy(t => t.Inicio);

        return new FichaPacienteDto
        {
            Paciente = PacienteService.Mapear(paciente),
            TotalTurnos = lista.Count,
            Atendidos = lista.Count(t => t.Estado == CteEstadosTurno.Atendido),
            Ausentes = lista.Count(t => t.Estado == CteEstadosTurno.Ausente),
            Cancelados = lista.Count(t => t.Estado == CteEstadosTurno.Cancelado),
            ProximoTurno = proximo is null ? null : TurnoService.Mapear(proximo),
            Turnos = lista.Select(t => TurnoService.Mapear(t, cantidades.GetValueOrDefault(t.Id))).ToList()
        };
    }
}
