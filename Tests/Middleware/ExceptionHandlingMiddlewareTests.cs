using System.Data.Common;
using System.Text.Json;
using Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tests.Fakes;
using Web.Middleware;

namespace Tests.Middleware;

/// <summary>
/// Traducción de excepciones a HTTP. Dos cosas se cuidan acá: que el código refleje de quién
/// es el problema (nuestro vs. base caída) y que nunca se escriba un error encima de una
/// respuesta ya empezada.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, string Body)> EjecutarAsync(Exception aLanzar)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw aLanzar,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    private static string? MensajeDe(string body)
        => JsonDocument.Parse(body).RootElement.GetProperty("message").GetString();

    private static bool ToastDe(string body)
        => JsonDocument.Parse(body).RootElement.GetProperty("toastShow").GetBoolean();

    [Fact]
    public async Task AppException_conserva_su_status_y_su_mensaje()
    {
        var (status, body) = await EjecutarAsync(AppException.Validacion("Falta el cliente."));

        Assert.Equal(400, status);
        Assert.Equal("Falta el cliente.", MensajeDe(body));
    }

    [Fact]
    public async Task Error_devuelto_por_postgres_es_500_no_503()
    {
        // 23505 = unique_violation. La base está viva y contestó: no es un problema de conexión.
        var duplicado = new PostgresException("duplicate key", "ERROR", "ERROR", "23505");

        var (status, _) = await EjecutarAsync(duplicado);

        Assert.Equal(500, status);
    }

    [Fact]
    public async Task Base_inalcanzable_sigue_siendo_503()
    {
        var (status, _) = await EjecutarAsync(new FalloDeConexion());

        Assert.Equal(503, status);
    }

    /// <summary>
    /// Los tres caminos de BD comparten un mensaje genérico: al usuario no le sirve saber que
    /// existe una base ni qué capa falló. El status sí distingue (503 caída / 500 el resto).
    /// </summary>
    [Theory]
    [MemberData(nameof(FallasDeBd))]
    public async Task Las_fallas_de_bd_dan_un_mensaje_generico_que_no_menciona_la_base(Exception falla)
    {
        var (_, body) = await EjecutarAsync(falla);

        Assert.Equal("Error de conexión con el sistema. Intentá de nuevo en unos minutos.", MensajeDe(body));
        Assert.DoesNotContain("base de datos", body, StringComparison.OrdinalIgnoreCase);
    }

    public static TheoryData<Exception> FallasDeBd() => new()
    {
        new PostgresException("duplicate key", "ERROR", "ERROR", "23505"),
        new FalloDeConexion(),
        new DbUpdateException("no se pudo guardar"),
    };

    /// <summary>
    /// Todo lo que llega al middleware es un error que el usuario tiene que ver: sin el toast, un
    /// 500 o un 503 dejaban la pantalla igual y parecía que no había pasado nada.
    /// </summary>
    [Fact]
    public async Task Todos_los_errores_piden_toast()
    {
        foreach (var falla in new Exception[]
                 {
                     AppException.Validacion("Falta el cliente."),
                     new PostgresException("duplicate key", "ERROR", "ERROR", "23505"),
                     new FalloDeConexion(),
                     new DbUpdateException("no se pudo guardar"),
                     new InvalidOperationException("boom"),
                 })
        {
            var (_, body) = await EjecutarAsync(falla);
            Assert.True(ToastDe(body), $"{falla.GetType().Name} no pidió toast");
        }
    }

    [Fact]
    public async Task DbUpdateException_es_500()
    {
        var (status, _) = await EjecutarAsync(new DbUpdateException("no se pudo guardar"));

        Assert.Equal(500, status);
    }

    [Fact]
    public async Task Excepcion_inesperada_es_500_generico()
    {
        var (status, body) = await EjecutarAsync(new InvalidOperationException("boom"));

        Assert.Equal(500, status);
        // El detalle interno no se filtra al cliente.
        Assert.DoesNotContain("boom", body);
    }

    [Fact]
    public async Task Con_la_respuesta_ya_empezada_relanza_en_vez_de_corromper_el_body()
    {
        // Caso real: GET /api/ordenes/{id}/comprobante-ingreso devuelve el PDF en streaming.
        // Si falla a mitad, pegarle un JSON al final le entrega al cliente un archivo roto.
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new ResponseYaEmpezadaFeature());

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw AppException.Validacion("explotó a mitad del PDF"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var ex = await Assert.ThrowsAsync<AppException>(() => middleware.Invoke(context));
        Assert.Equal("explotó a mitad del PDF", ex.Message);
    }

    /// <summary>DbException mínima: representa "no se pudo hablar con la base".</summary>
    private class FalloDeConexion : DbException
    {
        public FalloDeConexion() : base("host inalcanzable") { }
    }
}
