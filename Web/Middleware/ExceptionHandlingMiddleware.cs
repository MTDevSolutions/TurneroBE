using Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Responses;
using System.Data.Common;

namespace Web.Middleware;

/// <summary>
/// Atrapa excepciones no controladas y las traduce a respuestas <see cref="BaseResponse{T}"/>.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Mensaje único para cualquier falla de base de datos. Genérico a propósito: al usuario no le
    /// sirve —ni le corresponde— saber qué capa falló. El detalle queda en el log; los status codes
    /// sí distinguen (503 base caída / 500 el resto) para el front y el monitoreo.
    /// </summary>
    private const string MensajeErrorBd = "Error de conexión con el sistema. Intentá de nuevo en unos minutos.";

    private const string MensajeErrorGenerico = "Ocurrió un error en el sistema. Intentá de nuevo.";

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            if (!await ResponderAsync(context, ex.StatusCode, ex.Message)) throw;
        }
        // PostgresException = el servidor respondió con un error (constraint violada, SQL inválido).
        // La base está viva: es un problema nuestro, no de disponibilidad.
        catch (PostgresException ex)
        {
            logger.LogError(ex, "Error devuelto por PostgreSQL (SqlState {SqlState})", ex.SqlState);
            if (!await ResponderAsync(context, StatusCodes.Status500InternalServerError, MensajeErrorBd)) throw;
        }
        // Cualquier otra DbException = no se pudo hablar con la base (red, host caído, timeout).
        catch (DbException ex)
        {
            logger.LogError(ex, "Error de conexión con la base de datos");
            if (!await ResponderAsync(context, StatusCodes.Status503ServiceUnavailable, MensajeErrorBd)) throw;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error al guardar cambios en EF Core");
            if (!await ResponderAsync(context, StatusCodes.Status500InternalServerError, MensajeErrorBd)) throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excepción no controlada");
            if (!await ResponderAsync(context, StatusCodes.Status500InternalServerError, MensajeErrorGenerico)) throw;
        }
    }

    /// <summary>
    /// Escribe la respuesta de error y devuelve true. Si la respuesta ya empezó a enviarse
    /// (caso concreto: el comprobante en PDF, que se devuelve en streaming) no escribe nada y
    /// devuelve false: pisar el status es imposible y pegar el JSON al final del body le
    /// entrega al cliente un archivo corrupto. El llamador relanza para que corte la conexión.
    /// <para>
    /// El toast va en true por defecto: todo lo que llega acá es un error que el usuario tiene que
    /// ver. Sin esto, un 500 o un 503 dejaban la pantalla igual y parecía que no había pasado nada.
    /// </para>
    /// </summary>
    private async Task<bool> ResponderAsync(HttpContext context, int statusCode, string mensaje, bool toast = true)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning("La respuesta de {Path} ya había empezado: no se puede escribir el error.",
                context.Request.Path);
            return false;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(BaseResponse<string>.Fail(mensaje, toast: toast));
        return true;
    }
}
