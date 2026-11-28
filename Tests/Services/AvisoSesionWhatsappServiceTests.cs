using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constantes;
using Shared.DTOs.Whatsapp;
using Tests.Fakes;

namespace Tests.Services;

public class AvisoSesionWhatsappServiceTests
{
    private readonly TenantRepositoryFake _tenants = new();
    private readonly UnitOfWorkFake _uow = new();
    private readonly Tenant _tenant;
    private static readonly DateTime Desde = new(2026, 11, 28, 3, 12, 0, DateTimeKind.Utc);

    public AvisoSesionWhatsappServiceTests()
        => _tenant = _tenants.Agregar(new Tenant
        {
            Id = 1, Nombre = "Lic. Norte", Especialidad = "psicologia", Estado = "activo", WhatsappTelefonoId = 5,
        });

    private AvisoSesionWhatsappService Servicio() => new(_tenants, _uow, NullLogger<AvisoSesionWhatsappService>.Instance);

    private static WebhookSesionDto Aviso(long id = 10, string estado = CteSesionWhatsapp.Deslogueado,
        string? etiqueta = "1", int telefonoId = 5) => new()
    {
        Id = id, TelefonoId = telefonoId, Etiqueta = etiqueta, EstadoAnterior = "conectado", Estado = estado, Desde = Desde,
    };

    [Fact]
    public async Task Guarda_el_estado_el_desde_y_el_id()
    {
        await Servicio().ProcesarAsync(Aviso());

        Assert.Equal(CteSesionWhatsapp.Deslogueado, _tenant.WhatsappEstadoSesion);
        Assert.Equal(Desde, _tenant.WhatsappEstadoDesde);
        Assert.Equal(10, _tenant.WhatsappUltimoAvisoId);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Un_aviso_viejo_no_pisa_uno_nuevo()
    {
        await Servicio().ProcesarAsync(Aviso(id: 10, estado: CteSesionWhatsapp.Conectado));
        await Servicio().ProcesarAsync(Aviso(id: 9, estado: CteSesionWhatsapp.Deslogueado));

        Assert.Equal(CteSesionWhatsapp.Conectado, _tenant.WhatsappEstadoSesion);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Un_aviso_de_otro_telefono_se_ignora()
    {
        await Servicio().ProcesarAsync(Aviso(telefonoId: 99));

        Assert.Null(_tenant.WhatsappEstadoSesion);
        Assert.Equal(0, _uow.Guardados);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-un-id")]
    [InlineData("42")]
    public async Task Etiqueta_invalida_o_de_cuenta_inexistente_se_ignora(string? etiqueta)
    {
        await Servicio().ProcesarAsync(Aviso(etiqueta: etiqueta));

        Assert.Equal(0, _uow.Guardados);
    }

    [Fact]
    public async Task Un_estado_desconocido_se_guarda_como_desconectado()
    {
        await Servicio().ProcesarAsync(Aviso(estado: "inventado"));

        Assert.Equal(CteSesionWhatsapp.Desconectado, _tenant.WhatsappEstadoSesion);
    }
}

public class WhatsappConexionServiceAlertaTests
{
    private readonly TenantRepositoryFake _tenants = new();

    private WhatsappConexionService Servicio() => new(
        new TenantContextFake(1), _tenants,
        new ConfiguracionService(new ConfiguracionRepositoryFake(), new UnitOfWorkFake()), new WhatsappGatewayFake());

    private Tenant Cuenta(int? telefono = 5, string? estado = null) => _tenants.Agregar(new Tenant
    {
        Id = 1, Nombre = "Lic. Norte", Especialidad = "psicologia", Estado = "activo",
        WhatsappTelefonoId = telefono, WhatsappEstadoSesion = estado, WhatsappEstadoDesde = DateTime.UtcNow,
    });

    [Theory]
    [InlineData(CteSesionWhatsapp.Deslogueado)]
    [InlineData(CteSesionWhatsapp.EsperandoQr)]
    [InlineData(CteSesionWhatsapp.Desconectado)]
    public async Task Sesion_caida_muestra_el_cartel(string estado)
    {
        Cuenta(estado: estado);

        var alerta = await Servicio().AlertaAsync();

        Assert.True(alerta.Mostrar);
        Assert.Equal(estado, alerta.Estado);
        Assert.NotNull(alerta.Desde);
    }

    [Fact]
    public async Task Conectado_no_muestra_nada()
    {
        Cuenta(estado: CteSesionWhatsapp.Conectado);
        Assert.False((await Servicio().AlertaAsync()).Mostrar);
    }

    [Fact]
    public async Task Sin_aviso_todavia_no_muestra_nada()
    {
        Cuenta(estado: null);
        Assert.False((await Servicio().AlertaAsync()).Mostrar);
    }

    [Fact]
    public async Task Sin_telefono_vinculado_no_muestra_nada()
    {
        Cuenta(telefono: null, estado: CteSesionWhatsapp.Deslogueado);
        Assert.False((await Servicio().AlertaAsync()).Mostrar);
    }
}
