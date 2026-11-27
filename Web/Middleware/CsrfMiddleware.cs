using Microsoft.Extensions.Configuration;
using Shared.Responses;

namespace Web.Middleware;

/// <summary>
/// Protección CSRF (double-submit). Solo aplica a requests que MUTAN, autenticados
/// por la cookie <c>access_token</c> y SIN header Authorization. Es decir:
/// - login (sin cookie todavía) → exento;
/// - clientes API/Swagger con Bearer → exentos (el header no se manda solo, no hay CSRF);
/// - navegador que manda la cookie sola → debe enviar también X-CSRF-Token == cookie csrf_token.
/// </summary>
public class CsrfMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private static readonly HashSet<string> MetodosSeguros = new(StringComparer.OrdinalIgnoreCase)
        { "GET", "HEAD", "OPTIONS", "TRACE" };

    private static readonly HashSet<string> RutasExcluidas = new(StringComparer.OrdinalIgnoreCase)
        { "/api/auth/login", "/api/auth/superadmin/login" };

    public async Task Invoke(HttpContext context)
    {
        var habilitado = configuration.GetValue("CsrfSettings:Enabled", true);
        var metodo = context.Request.Method;
        var ruta = NormalizarRuta(context.Request.Path.Value);

        var autenticadoPorCookie = context.Request.Cookies.ContainsKey("access_token");
        var tieneBearer = context.Request.Headers.ContainsKey("Authorization");

        var aplica = habilitado
            && !MetodosSeguros.Contains(metodo)
            && !RutasExcluidas.Contains(ruta)
            && autenticadoPorCookie
            && !tieneBearer;

        if (aplica)
        {
            var headerToken = context.Request.Headers["X-CSRF-Token"].ToString();
            var cookieToken = context.Request.Cookies["csrf_token"];

            if (string.IsNullOrEmpty(headerToken) || !string.Equals(headerToken, cookieToken, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    BaseResponse<string>.Fail("Token CSRF inválido.", data: "CSRF_INVALIDO", toast: true));
                return;
            }
        }

        await next(context);
    }

    /// <summary>
    /// Quita la barra final para que la exclusión matchee igual con y sin ella: ASP.NET Core
    /// rutea <c>/api/auth/login/</c> al mismo action que <c>/api/auth/login</c>, así que si acá
    /// se compara el path crudo, el login con barra queda exigiendo un token CSRF que todavía
    /// no existe (la cookie se emite recién en la respuesta) y devuelve 403.
    /// </summary>
    private static string NormalizarRuta(string? path)
    {
        var ruta = path ?? string.Empty;
        return ruta.Length > 1 ? ruta.TrimEnd('/') : ruta;
    }
}
