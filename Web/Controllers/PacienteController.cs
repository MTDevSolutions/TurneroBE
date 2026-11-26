using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Paciente;
using Shared.DTOs.Paginacion;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

[ApiController]
[Route("api/pacientes")]
[Authorize(Policy = "Tenant")]
[Tags("Pacientes")]
public class PacienteController(IPacienteService pacienteService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Listar / buscar pacientes",
        Description = "Pacientes de la cuenta, paginado y ordenado por apellido. 'buscar' filtra por nombre, apellido o celular. Por defecto solo activos.")]
    public async Task<ActionResult<BaseResponse<PaginaDto<PacienteDto>>>> Listar(
        [FromQuery] PaginacionQuery query, [FromQuery] string? buscar, [FromQuery] bool incluirInactivos, CancellationToken cancellationToken)
        => Ok(BaseResponse<PaginaDto<PacienteDto>>.Ok(await pacienteService.ListarAsync(query, buscar, incluirInactivos, cancellationToken)));

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Obtener paciente")]
    public async Task<ActionResult<BaseResponse<PacienteDto>>> Obtener(int id, CancellationToken cancellationToken)
        => Ok(BaseResponse<PacienteDto>.Ok(await pacienteService.ObtenerAsync(id, cancellationToken)));

    [HttpPost]
    [SwaggerOperation(Summary = "Crear paciente",
        Description = "El celular se normaliza al formato de WhatsApp (549 + área + número). 400 si no se puede normalizar, 409 si otro paciente ya lo usa.")]
    public async Task<ActionResult<BaseResponse<PacienteDto>>> Crear([FromBody] PacienteGuardarDto dto, CancellationToken cancellationToken)
    {
        var creado = await pacienteService.CrearAsync(dto, cancellationToken);
        return Ok(BaseResponse<PacienteDto>.Ok(creado, "Paciente creado.", true));
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Editar paciente")]
    public async Task<ActionResult<BaseResponse<PacienteDto>>> Editar(int id, [FromBody] PacienteGuardarDto dto, CancellationToken cancellationToken)
    {
        var editado = await pacienteService.ActualizarAsync(id, dto, cancellationToken);
        return Ok(BaseResponse<PacienteDto>.Ok(editado, "Paciente actualizado.", true));
    }

    [HttpPost("{id:int}/desactivar")]
    [SwaggerOperation(Summary = "Desactivar paciente", Description = "Deja de aparecer en la lista y no se le pueden dar turnos nuevos. No borra nada.")]
    public async Task<ActionResult<BaseResponse<PacienteDto>>> Desactivar(int id, CancellationToken cancellationToken)
        => Ok(BaseResponse<PacienteDto>.Ok(await pacienteService.CambiarActivoAsync(id, false, cancellationToken), "Paciente desactivado.", true));

    [HttpPost("{id:int}/reactivar")]
    [SwaggerOperation(Summary = "Reactivar paciente")]
    public async Task<ActionResult<BaseResponse<PacienteDto>>> Reactivar(int id, CancellationToken cancellationToken)
        => Ok(BaseResponse<PacienteDto>.Ok(await pacienteService.CambiarActivoAsync(id, true, cancellationToken), "Paciente reactivado.", true));
}
