using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Sucursal;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

[ApiController]
[Route("api/sucursales")]
[Authorize(Policy = "Tenant")]
[Tags("Sucursales")]
public class SucursalController(ISucursalService sucursalService) : ControllerBase
{
    private readonly ISucursalService _sucursalService = sucursalService;

    [HttpGet]
    [SwaggerOperation(Summary = "Listar sucursales",
        Description = "Devuelve las sucursales del tenant autenticado (filtradas por el global query filter).")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<List<SucursalDto>>>> Listar()
    {
        var sucursales = await _sucursalService.ListarAsync();
        return Ok(BaseResponse<List<SucursalDto>>.Ok(sucursales));
    }
}
