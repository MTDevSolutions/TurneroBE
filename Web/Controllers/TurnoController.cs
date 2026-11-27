using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Turno;
using Shared.DTOs.Whatsapp;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

[ApiController]
[Route("api/turnos")]
[Authorize(Policy = "Tenant")]
[Tags("Turnos")]
public class TurnoController(ITurnoService turnoService, IHistorialWhatsappService historialService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Turnos de un rango",
        Description = "Turnos que se cruzan con [desde, hasta) (máximo 62 días), incluidos los cancelados. Fechas ISO con offset.")]
    public async Task<ActionResult<BaseResponse<List<TurnoDto>>>> Listar(
        [FromQuery] DateTimeOffset desde, [FromQuery] DateTimeOffset hasta, CancellationToken cancellationToken)
        => Ok(BaseResponse<List<TurnoDto>>.Ok(await turnoService.ListarAsync(desde, hasta, cancellationToken)));

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Obtener turno")]
    public async Task<ActionResult<BaseResponse<TurnoDto>>> Obtener(int id, CancellationToken cancellationToken)
        => Ok(BaseResponse<TurnoDto>.Ok(await turnoService.ObtenerAsync(id, cancellationToken)));

    [HttpPost]
    [SwaggerOperation(Summary = "Dar turno",
        Description = "Crea el turno en estado pendiente. Sin duración usa la de la configuración. 400 si es en el pasado, 409 si se superpone con otro.")]
    public async Task<ActionResult<BaseResponse<TurnoDto>>> Crear([FromBody] TurnoGuardarDto dto, CancellationToken cancellationToken)
    {
        var creado = await turnoService.CrearAsync(dto, cancellationToken);
        return Ok(BaseResponse<TurnoDto>.Ok(creado, "Turno agendado.", true));
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Modificar / reprogramar turno",
        Description = "Solo pendiente o confirmado. Si cambia el horario vuelve a pendiente.")]
    public async Task<ActionResult<BaseResponse<TurnoDto>>> Reprogramar(int id, [FromBody] TurnoGuardarDto dto, CancellationToken cancellationToken)
    {
        var editado = await turnoService.ReprogramarAsync(id, dto, cancellationToken);
        return Ok(BaseResponse<TurnoDto>.Ok(editado, "Turno actualizado.", true));
    }

    [HttpPost("{id:int}/estado")]
    [SwaggerOperation(Summary = "Cambiar estado",
        Description = "confirmado, pendiente, cancelado (con motivo opcional), atendido o ausente (estos dos solo si el turno ya empezó). 409 si la transición no es válida.")]
    public async Task<ActionResult<BaseResponse<TurnoDto>>> CambiarEstado(int id, [FromBody] TurnoEstadoDto dto, CancellationToken cancellationToken)
    {
        var turno = await turnoService.CambiarEstadoAsync(id, dto, cancellationToken);
        return Ok(BaseResponse<TurnoDto>.Ok(turno, "Turno actualizado.", true));
    }

    [HttpGet("{id:int}/mensajes")]
    [SwaggerOperation(Summary = "Mensajes de WhatsApp del turno",
        Description = "Avisos enviados (con su estado) y respuestas del paciente, en orden.")]
    public async Task<ActionResult<BaseResponse<List<MensajeWhatsappDto>>>> Mensajes(int id, CancellationToken cancellationToken)
        => Ok(BaseResponse<List<MensajeWhatsappDto>>.Ok(await historialService.ListarPorTurnoAsync(id, cancellationToken)));
}
