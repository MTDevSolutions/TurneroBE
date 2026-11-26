using Application.Interfaces;
using Application.UseCases.Security_Context;
using Shared.Constantes;
using Shared.Responses;

namespace Web.Middleware;

/// <summary>
/// Bloquea los requests de una cuenta que no aceptó la versión vigente de los términos de uso
/// (403 TERMINOS_PENDIENTES): la aceptación explícita es condición para usar el canal no oficial.
/// Quedan libres las rutas que hacen falta para leerlos y aceptarlos (auth y términos).
/// Debe ir DESPUÉS de UseAuthentication y de TenantSuspendidoMiddleware.
/// </summary>
public class TerminosPendientesMiddleware(RequestDelegate next)
{
    private static readonly string[] RutasLibres = ["/api/auth", "/api/terminos"];

    public async Task Invoke(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantEstadoService tenantEstadoService)
    {
        var tenantId = tenantContext.TenantId;
        var ruta = context.Request.Path;

        if (tenantId is > 0
            && !tenantContext.EsSuperadmin
            && !RutasLibres.Any(libre => ruta.StartsWithSegments(libre, StringComparison.OrdinalIgnoreCase)))
        {
            var aceptada = await tenantEstadoService.ObtenerVersionTerminosAsync(tenantId.Value, context.RequestAborted);

            if (aceptada != CteTerminos.VersionVigente)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(BaseResponse<string>.Fail(
                    "Para seguir, aceptá los términos de uso.", data: CteTerminos.CodigoPendientes, toast: true));
                return;
            }
        }

        await next(context);
    }
}
