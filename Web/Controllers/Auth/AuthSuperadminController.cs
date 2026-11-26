using Application.Helpers;
using Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constantes;
using Shared.DTOs.Auth;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using Web.Helpers;

namespace Web.Controllers.Auth;

[ApiController]
[Route("api/auth/superadmin")]
[Tags("Auth Superadmin")]
public class AuthSuperadminController(
    AutenticacionSuperadmin autenticacion,
    JwtTokenGenerator jwtTokenGenerator,
    IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Login de superadmin",
        Description = "Valida credenciales y emite un JWT con tipo_acceso=plataforma (sin tenant_id). El token va en el body y en una cookie HttpOnly.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseResponse<SuperadminLoginResponseDto>>> Login([FromBody] SuperadminLoginDto dto, CancellationToken cancellationToken)
    {
        var superadmin = await autenticacion.ExecuteAsync(dto.Email, dto.Password, cancellationToken);

        var minutos = configuration.GetValue("JwtSettings:ExpirationInMinutes", 120);
        var expira = DateTimeOffset.UtcNow.AddMinutes(minutos);

        var token = jwtTokenGenerator.GenerarToken(
            superadmin.Id, superadmin.Email, superadmin.Email, superadmin.Nombre, habilitado: true);

        AuthCookies.Escribir(HttpContext, configuration, token, expira);

        var response = new SuperadminLoginResponseDto
        {
            Token = token,
            Nombre = superadmin.Nombre,
            Email = superadmin.Email,
            ExpiraEn = expira
        };

        return Ok(BaseResponse<SuperadminLoginResponseDto>.Ok(response, $"Bienvenido {superadmin.Nombre}", true));
    }

    [Authorize(Policy = "Superadmin")]
    [HttpGet("me")]
    [SwaggerOperation(Summary = "Identidad actual", Description = "Devuelve los claims del superadmin autenticado (para verificar el token).")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<BaseResponse<object>> Me()
    {
        var data = new
        {
            Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Nombre = User.FindFirst("NombreApellido")?.Value,
            TipoAcceso = User.FindFirst(CteSeguridad.ClaimTipoAcceso)?.Value
        };
        return Ok(BaseResponse<object>.Ok(data));
    }

    [Authorize(Policy = "Superadmin")]
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Logout", Description = "Expira las cookies access_token y csrf_token.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<BaseResponse<string>> Logout()
    {
        AuthCookies.Borrar(HttpContext, configuration);
        return Ok(BaseResponse<string>.Ok(null, "Sesión cerrada", true));
    }
}
