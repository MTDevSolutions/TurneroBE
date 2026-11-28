using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Configuracion;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

[ApiController]
[Route("api/configuracion")]
[Authorize(Policy = "Tenant")]
[Tags("Configuración")]
public class ConfiguracionController(IConfiguracionService configuracionService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Obtener configuración",
        Description = "Devuelve la configuración de la agenda de la cuenta autenticada. Si aún no existe, la crea con valores por defecto.")]
    public async Task<ActionResult<BaseResponse<ConfiguracionDto>>> Obtener(CancellationToken cancellationToken)
        => Ok(BaseResponse<ConfiguracionDto>.Ok(await configuracionService.ObtenerAsync(cancellationToken)));

    [HttpPut]
    [SwaggerOperation(Summary = "Actualizar configuración",
        Description = "Duración por defecto de los turnos, horas de anticipación del recordatorio y llave general de avisos.")]
    public async Task<ActionResult<BaseResponse<ConfiguracionDto>>> Actualizar([FromBody] ConfiguracionActualizarDto dto, CancellationToken cancellationToken)
    {
        var actualizada = await configuracionService.ActualizarAsync(dto, cancellationToken);
        return Ok(BaseResponse<ConfiguracionDto>.Ok(actualizada, "Configuración actualizada.", true));
    }

    [HttpPut("color")]
    [SwaggerOperation(Summary = "Cambiar el color de la app",
        Description = "Guarda la paleta del color principal de la cuenta: rosa, lavanda, coral, salvia o celeste. 400 si es otra.")]
    public async Task<ActionResult<BaseResponse<ConfiguracionDto>>> ActualizarColor([FromBody] ColorPrincipalDto dto, CancellationToken cancellationToken)
    {
        var actualizada = await configuracionService.ActualizarColorAsync(dto.Color, cancellationToken);
        return Ok(BaseResponse<ConfiguracionDto>.Ok(actualizada, "Color guardado."));
    }
}
