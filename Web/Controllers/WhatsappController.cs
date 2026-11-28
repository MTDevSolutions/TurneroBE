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
public class WhatsappController(IWhatsappConexionService conexionService) : ControllerBase
{
    [HttpGet("estado")]
    [SwaggerOperation(Summary = "Estado de WhatsApp",
        Description = "Estado de la sesión del número de la cuenta y si los avisos automáticos están activos.")]
    public async Task<ActionResult<BaseResponse<WhatsappEstadoDto>>> Estado(CancellationToken cancellationToken)
        => Ok(BaseResponse<WhatsappEstadoDto>.Ok(await conexionService.EstadoAsync(cancellationToken)));

    [HttpGet("alerta")]
    [SwaggerOperation(Summary = "Alerta de WhatsApp desconectado",
        Description = "Si hay que mostrar el cartel de WhatsApp desconectado. Sale del último aviso del gateway, sin consultar al bot.")]
    public async Task<ActionResult<BaseResponse<WhatsappAlertaDto>>> Alerta(CancellationToken cancellationToken)
        => Ok(BaseResponse<WhatsappAlertaDto>.Ok(await conexionService.AlertaAsync(cancellationToken)));

    [HttpGet("qr")]
    [SwaggerOperation(Summary = "QR de vinculación",
        Description = "QR para vincular el WhatsApp de la cuenta (data-url PNG). Null si ya está conectado o no hay QR.")]
    public async Task<ActionResult<BaseResponse<WhatsappQrDto>>> Qr(CancellationToken cancellationToken)
        => Ok(BaseResponse<WhatsappQrDto>.Ok(await conexionService.QrAsync(cancellationToken)));
}
