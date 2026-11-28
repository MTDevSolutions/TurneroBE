using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Whatsapp;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

[ApiController]
[Route("api/whatsapp")]
[Authorize(Policy = "Tenant")]
[Tags("WhatsApp")]
public class WhatsappController(
    IWhatsappConexionService conexionService,
    IPlantillaMensajeService plantillaService) : ControllerBase
{
    [HttpGet("estado")]
    [SwaggerOperation(Summary = "Estado de WhatsApp",
        Description = "Estado de la sesión del número de la cuenta y si los avisos automáticos están activos.")]
    public async Task<ActionResult<BaseResponse<WhatsappEstadoDto>>> Estado(CancellationToken cancellationToken)
        => Ok(BaseResponse<WhatsappEstadoDto>.Ok(await conexionService.EstadoAsync(cancellationToken)));

    [HttpGet("qr")]
    [SwaggerOperation(Summary = "QR de vinculación",
        Description = "QR para vincular el WhatsApp de la cuenta (data-url PNG). Null si ya está conectado o no hay QR.")]
    public async Task<ActionResult<BaseResponse<WhatsappQrDto>>> Qr(CancellationToken cancellationToken)
        => Ok(BaseResponse<WhatsappQrDto>.Ok(await conexionService.QrAsync(cancellationToken)));

    [HttpGet("plantillas")]
    [SwaggerOperation(Summary = "Mensajes de WhatsApp",
        Description = "Cada mensaje automático con su texto vigente (el de la cuenta o el de fábrica), el pie fijo y las variables disponibles.")]
    public async Task<ActionResult<BaseResponse<PlantillasMensajeDto>>> Plantillas(CancellationToken cancellationToken)
        => Ok(BaseResponse<PlantillasMensajeDto>.Ok(await plantillaService.ListarAsync(cancellationToken)));

    [HttpPut("plantillas/{clave}")]
    [SwaggerOperation(Summary = "Guardar mensaje",
        Description = "Reescribe el texto de un mensaje. 400 si queda vacío o usa variables desconocidas; 403 si no es el titular.")]
    public async Task<ActionResult<BaseResponse<PlantillaMensajeDto>>> GuardarPlantilla(string clave, [FromBody] PlantillaMensajeGuardarDto dto, CancellationToken cancellationToken)
    {
        var plantilla = await plantillaService.GuardarAsync(clave, dto, cancellationToken);
        return Ok(BaseResponse<PlantillaMensajeDto>.Ok(plantilla, "Mensaje guardado.", true));
    }

    [HttpDelete("plantillas/{clave}")]
    [SwaggerOperation(Summary = "Volver al mensaje original", Description = "Descarta el texto propio y vuelve al de fábrica.")]
    public async Task<ActionResult<BaseResponse<PlantillaMensajeDto>>> RestablecerPlantilla(string clave, CancellationToken cancellationToken)
    {
        var plantilla = await plantillaService.RestablecerAsync(clave, cancellationToken);
        return Ok(BaseResponse<PlantillaMensajeDto>.Ok(plantilla, "Volvió el mensaje original.", true));
    }

    [HttpPost("plantillas/vista-previa")]
    [SwaggerOperation(Summary = "Vista previa de un mensaje",
        Description = "Arma el texto con datos de ejemplo y el pie fijo, tal como le llegaría al paciente. No guarda nada.")]
    public async Task<ActionResult<BaseResponse<VistaPreviaMensajeResultadoDto>>> VistaPrevia([FromBody] VistaPreviaMensajeDto dto, CancellationToken cancellationToken)
        => Ok(BaseResponse<VistaPreviaMensajeResultadoDto>.Ok(await plantillaService.VistaPreviaAsync(dto, cancellationToken)));
}
