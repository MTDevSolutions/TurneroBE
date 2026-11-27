using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.NotaConsulta;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

/// <summary>
/// Notas de consulta. Solo el profesional (rol admin): cualquier otro rol recibe 403.
/// No hay DELETE: una nota se anula con motivo y queda en el historial.
/// </summary>
[ApiController]
[Route("api")]
[Authorize(Policy = "Tenant")]
[Tags("Notas de consulta")]
public class NotaConsultaController(INotaConsultaService notaService) : ControllerBase
{
    [HttpGet("pacientes/{pacienteId:int}/notas")]
    [SwaggerOperation(Summary = "Notas del paciente",
        Description = "De la más nueva a la más vieja. Por defecto incluye las anuladas (se muestran tachadas).")]
    public async Task<ActionResult<BaseResponse<List<NotaConsultaDto>>>> Listar(
        int pacienteId, [FromQuery] bool incluirAnuladas = true, CancellationToken cancellationToken = default)
        => Ok(BaseResponse<List<NotaConsultaDto>>.Ok(await notaService.ListarAsync(pacienteId, incluirAnuladas, cancellationToken)));

    [HttpPost("pacientes/{pacienteId:int}/notas")]
    [SwaggerOperation(Summary = "Escribir nota",
        Description = "Con turnoId, el turno tiene que ser de este paciente y estar atendido. Sin turnoId es una nota suelta.")]
    public async Task<ActionResult<BaseResponse<NotaConsultaDto>>> Crear(
        int pacienteId, [FromBody] NotaConsultaCrearDto dto, CancellationToken cancellationToken)
    {
        var creada = await notaService.CrearAsync(pacienteId, dto, cancellationToken);
        return Ok(BaseResponse<NotaConsultaDto>.Ok(creada, "Nota guardada.", true));
    }

    [HttpPut("notas/{id:int}")]
    [SwaggerOperation(Summary = "Editar nota",
        Description = "Solo dentro de las 24 h desde el alta y si no está anulada; guarda la versión anterior. 'version' es la que se estaba editando: 409 si cambió.")]
    public async Task<ActionResult<BaseResponse<NotaConsultaDto>>> Editar(
        int id, [FromBody] NotaConsultaEditarDto dto, CancellationToken cancellationToken)
    {
        var editada = await notaService.EditarAsync(id, dto, cancellationToken);
        return Ok(BaseResponse<NotaConsultaDto>.Ok(editada, "Nota actualizada.", true));
    }

    [HttpPost("notas/{id:int}/anular")]
    [SwaggerOperation(Summary = "Anular nota", Description = "Con motivo obligatorio. El texto se conserva.")]
    public async Task<ActionResult<BaseResponse<NotaConsultaDto>>> Anular(
        int id, [FromBody] NotaConsultaAnularDto dto, CancellationToken cancellationToken)
    {
        var anulada = await notaService.AnularAsync(id, dto, cancellationToken);
        return Ok(BaseResponse<NotaConsultaDto>.Ok(anulada, "Nota anulada.", true));
    }

    [HttpGet("notas/{id:int}/versiones")]
    [SwaggerOperation(Summary = "Versiones de la nota", Description = "La vigente primero y después las anteriores, de la más nueva a la más vieja.")]
    public async Task<ActionResult<BaseResponse<List<NotaConsultaVersionDto>>>> Versiones(int id, CancellationToken cancellationToken)
        => Ok(BaseResponse<List<NotaConsultaVersionDto>>.Ok(await notaService.VersionesAsync(id, cancellationToken)));
}
