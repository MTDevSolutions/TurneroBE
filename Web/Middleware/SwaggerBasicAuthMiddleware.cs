using System.Text;

namespace Web.Middleware;

/// <summary>
/// Exige Basic Auth en las rutas /swagger*. Independiente del login de la app.
/// Se engancha solo cuando Swagger se expone fuera de desarrollo (ENABLE_SWAGGER=true).
/// </summary>
public class SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var user = configuration["SwaggerAuth:Username"];
        var pass = configuration["SwaggerAuth:Password"];

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            // Cerrado por defecto: sin credenciales configuradas no se expone Swagger.
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Swagger no disponible: credenciales no configuradas.");
            return;
        }

        if (TryGetBasicCredentials(context.Request, out var u, out var p) && u == user && p == pass)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
    }

    private static bool TryGetBasicCredentials(HttpRequest request, out string user, out string pass)
    {
        user = pass = string.Empty;
        var header = request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
            var idx = decoded.IndexOf(':');
            if (idx < 0) return false;

            user = decoded[..idx];
            pass = decoded[(idx + 1)..];
            return true;
        }
        catch
        {
            return false;
        }
    }
}
