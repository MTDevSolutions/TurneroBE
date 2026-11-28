using Application.Helpers;
using Application.Interfaces;
using Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constantes;
using Shared.DTOs.Auth;
using Shared.Responses;
using Swashbuckle.AspNetCore.Annotations;
using Web.Helpers;

namespace Web.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[Tags("Auth Tenant")]
public class AuthTenantController(
    AutenticacionUsuario autenticacion,
    JwtTokenGenerator jwtTokenGenerator,
    ITenantEstadoService tenantEstadoService,
    IConfiguracionService configuracionService,
    IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Login de usuario de la cuenta",
        Description = "Valida credenciales (email único global) y emite un JWT con tenant_id y rol (tipo_acceso=tenant).")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse<UsuarioLoginResponseDto>>> Login([FromBody] UsuarioLoginDto dto, CancellationToken cancellationToken)
    {
        var usuario = await autenticacion.ExecuteAsync(dto.Email, dto.Password, cancellationToken);

        var minutos = configuration.GetValue("JwtSettings:ExpirationInMinutes", 120);
        var expira = DateTimeOffset.UtcNow.AddMinutes(minutos);

        var token = jwtTokenGenerator.GenerarToken(
            usuario.Id, usuario.Email, usuario.Email, usuario.Nombre, usuario.Activo,
            tenantId: usuario.TenantId, rol: usuario.Rol);

        AuthCookies.Escribir(HttpContext, configuration, token, expira);

        var versionAceptada = await tenantEstadoService.ObtenerVersionTerminosAsync(usuario.TenantId, cancellationToken);

        var response = new UsuarioLoginResponseDto
        {
            Token = token,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            TenantId = usuario.TenantId,
            ExpiraEn = expira,
            TerminosPendientes = versionAceptada != CteTerminos.VersionVigente,
            ColorPrincipal = await configuracionService.ObtenerColorAsync(usuario.TenantId, cancellationToken)
        };

        return Ok(BaseResponse<UsuarioLoginResponseDto>.Ok(response, $"Bienvenido {usuario.Nombre}", true));
    }

    [Authorize(Policy = "Tenant")]
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Logout", Description = "Expira las cookies access_token y csrf_token.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<BaseResponse<string>> Logout()
    {
        AuthCookies.Borrar(HttpContext, configuration);
        return Ok(BaseResponse<string>.Ok(null, "Sesión cerrada", true));
    }
}
