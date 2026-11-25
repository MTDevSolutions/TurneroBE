using Application.Interfaces;
using Application.UseCases.Security_Context;
using Shared.Constantes;
using Shared.Responses;

namespace Web.Middleware;

/// <summary>
/// Bloquea requests de usuarios de negocio cuyo tenant no esté "activo".
/// El superadmin (plano plataforma) y los requests sin tenant pasan sin chequeo.
/// Debe ir DESPUÉS de UseAuthentication (necesita los claims).
/// </summary>
public class TenantSuspendidoMiddleware(RequestDelegate next)
{
    private const string EstadoActivo = CteEstadosTenant.Activo;
    private const string CodigoSuspendido = "TENANT_SUSPENDIDO";

    public async Task Invoke(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantEstadoService tenantEstadoService)
    {
        var tenantId = tenantContext.TenantId;

        // Solo aplica a usuarios de negocio con tenant resuelto. Superadmin/anónimo pasan.
        if (tenantId is > 0 && !tenantContext.EsSuperadmin)
        {
            var estado = await tenantEstadoService.ObtenerEstadoAsync(tenantId.Value, context.RequestAborted);

            if (!string.Equals(estado, EstadoActivo, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    BaseResponse<string>.Fail("La cuenta está suspendida.", data: CodigoSuspendido, toast: true));
                return;
            }
        }

        await next(context);
    }
}
