using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Terminos;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

[ApiController]
[Route("api/terminos")]
[Authorize(Policy = "Tenant")]
[Tags("Términos de uso")]
public class TerminosController(ITerminosService terminosService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Términos vigentes",
        Description = "Texto y versión vigentes, y si la cuenta ya los aceptó. Accesible aunque estén pendientes.")]
    public async Task<ActionResult<BaseResponse<TerminosDto>>> Obtener(CancellationToken cancellationToken)
        => Ok(BaseResponse<TerminosDto>.Ok(await terminosService.ObtenerAsync(cancellationToken)));

    [HttpPost("aceptar")]
    [SwaggerOperation(Summary = "Aceptar términos",
        Description = "Aceptación explícita de la versión indicada. Solo el admin de la cuenta. 409 si la versión ya no es la vigente.")]
    public async Task<ActionResult<BaseResponse<TerminosDto>>> Aceptar([FromBody] TerminosAceptarDto dto, CancellationToken cancellationToken)
    {
        var terminos = await terminosService.AceptarAsync(dto, cancellationToken);
        return Ok(BaseResponse<TerminosDto>.Ok(terminos, "Términos aceptados.", true));
    }
}
