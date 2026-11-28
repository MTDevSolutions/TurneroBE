using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Whatsapp;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Controllers;

/// <summary>
/// Entrada de las respuestas de los pacientes. La llama Wabix Gateway (no un usuario), autenticada
/// con el header X-Webhook-Secret. Responde 200 a todo lo que no tenga arreglo reintentando (número
/// desconocido, cuenta inexistente); un error de base devuelve 5xx y el gateway reintenta.
/// </summary>
[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
[Tags("WhatsApp")]
public class WhatsappWebhookController(
    IRespuestaWhatsappService respuestaService,
    IAvisoSesionWhatsappService avisoSesionService) : ControllerBase
{
    private const string HeaderSecreto = "X-Webhook-Secret";

    [HttpPost]
    [SwaggerOperation(Summary = "Respuesta de un paciente (webhook del gateway)",
        Description = "\"Sí\" confirma y \"No\" cancela el próximo turno del paciente. 401 si el secreto no coincide.")]
    public async Task<ActionResult<BaseResponse<object>>> Recibir([FromBody] WebhookEntranteDto dto, CancellationToken cancellationToken)
    {
        if (!respuestaService.SecretoValido(Request.Headers[HeaderSecreto].ToString()))
            return Unauthorized(BaseResponse<object>.Fail("Secreto de webhook inválido."));

        await respuestaService.ProcesarAsync(dto, cancellationToken);
        return Ok(BaseResponse<object>.Ok(null!));
    }

    [HttpPost("sesion")]
    [SwaggerOperation(Summary = "Cambio de sesión de WhatsApp (aviso del gateway)",
        Description = "El gateway avisa que la sesión del número de una cuenta se cayó, se cerró o volvió. 401 si el secreto no coincide.")]
    public async Task<ActionResult<BaseResponse<object>>> Sesion([FromBody] WebhookSesionDto dto, CancellationToken cancellationToken)
    {
        // Mismo secreto que las respuestas: el gateway usa uno por empresa para los avisos y Turnix
        // lo configura igual que el de los entrantes.
        if (!respuestaService.SecretoValido(Request.Headers[HeaderSecreto].ToString()))
            return Unauthorized(BaseResponse<object>.Fail("Secreto de webhook inválido."));

        await avisoSesionService.ProcesarAsync(dto, cancellationToken);
        return Ok(BaseResponse<object>.Ok(null!));
    }
}
