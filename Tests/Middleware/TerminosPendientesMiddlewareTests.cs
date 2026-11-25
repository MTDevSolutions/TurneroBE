using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Shared.Constantes;
using Tests.Fakes;
using Web.Middleware;

namespace Tests.Middleware;

/// <summary>
/// Sin aceptar los términos vigentes, la cuenta no usa nada más que lo necesario para leerlos
/// y aceptarlos. El superadmin no tiene términos que aceptar.
/// </summary>
public class TerminosPendientesMiddlewareTests
{
    private static async Task<(int Status, bool Paso, string Body)> EjecutarAsync(
        string ruta, string? versionAceptada, int? tenantId = 7, bool esSuperadmin = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = ruta;
        context.Response.Body = new MemoryStream();

        var paso = false;
        var middleware = new TerminosPendientesMiddleware(_ => { paso = true; return Task.CompletedTask; });

        await middleware.Invoke(context,
            new TenantContextFake(tenantId, esSuperadmin),
            new TenantEstadoServiceFake(versionTerminos: versionAceptada));

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, paso, body);
    }

    [Theory]
    [InlineData(null)]          // nunca aceptó
    [InlineData("2020-01-01")]  // aceptó una versión anterior
    public async Task Sin_la_version_vigente_bloquea_con_codigo_identificable(string? version)
    {
        var (status, paso, body) = await EjecutarAsync("/api/configuracion", version);

        Assert.Equal(403, status);
        Assert.False(paso);
        Assert.Equal(CteTerminos.CodigoPendientes, JsonDocument.Parse(body).RootElement.GetProperty("data").GetString());
    }

    [Theory]
    [InlineData("/api/terminos")]
    [InlineData("/api/terminos/aceptar")]
    [InlineData("/api/auth/logout")]
    public async Task Deja_pasar_lo_necesario_para_leer_y_aceptar(string ruta)
    {
        var (_, paso, _) = await EjecutarAsync(ruta, versionAceptada: null);

        Assert.True(paso);
    }

    [Fact]
    public async Task Una_ruta_que_solo_empieza_parecido_no_queda_libre()
    {
        var (status, paso, _) = await EjecutarAsync("/api/terminosx", versionAceptada: null);

        Assert.Equal(403, status);
        Assert.False(paso);
    }

    [Fact]
    public async Task Con_la_version_vigente_pasa()
    {
        var (_, paso, _) = await EjecutarAsync("/api/configuracion", CteTerminos.VersionVigente);

        Assert.True(paso);
    }

    [Fact]
    public async Task El_superadmin_pasa_sin_chequeo()
    {
        var (_, paso, _) = await EjecutarAsync("/api/tenants", versionAceptada: null, tenantId: null, esSuperadmin: true);

        Assert.True(paso);
    }
}
