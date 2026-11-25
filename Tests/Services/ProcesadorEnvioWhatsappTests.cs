using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constantes;
using Tests.Fakes;

namespace Tests.Services;

public class ProcesadorEnvioWhatsappTests
{
    private static readonly DateTime Ahora = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly MensajeWhatsappRepositoryFake _mensajes = new();
    private readonly TenantRepositoryFake _tenants = new();
    private readonly WhatsappGatewayFake _gateway = new();
    private readonly UnitOfWorkFake _uow = new();
    private readonly Tenant _tenant;

    public ProcesadorEnvioWhatsappTests()
        => _tenant = _tenants.Agregar(new Tenant { Id = 1, Nombre = "Lic. Norte", Especialidad = "psicologia", Estado = "activo", WhatsappTelefonoId = 7, TerminosVersionAceptada = CteTerminos.VersionVigente });

    private ProcesadorEnvioWhatsapp Procesador()
        => new(_mensajes, _tenants, _gateway, _uow, NullLogger<ProcesadorEnvioWhatsapp>.Instance);

    private MensajeWhatsapp Pendiente(DateTime? creadoEn = null, int intentos = 0)
    {
        var m = new MensajeWhatsapp
        {
            TenantId = 1, TurnoId = 1, Tipo = CteMensajesWhatsapp.TipoAviso,
            Destino = "5491123456789", Texto = "hola", Estado = CteMensajesWhatsapp.Pendiente,
            Intentos = intentos, CreadoEn = creadoEn ?? Ahora.AddMinutes(-1), ProximoIntentoEn = Ahora.AddMinutes(-1)
        };
        _mensajes.Agregar(m);
        return m;
    }

    [Fact]
    public async Task Envio_ok_queda_enviado_con_el_id_del_gateway()
    {
        var m = Pendiente();

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Enviado, m.Estado);
        Assert.Equal("m-1", m.GatewayMessageId);
        Assert.NotNull(m.EnviadoEn);
        Assert.Equal((7, "5491123456789", "hola"), _gateway.Envios.Single());
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Error_transitorio_reintenta_con_backoff()
    {
        var m = Pendiente();
        _gateway.Respuestas.Enqueue(new RespuestaEnvioGateway(503, null, "Sesión caída"));

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Pendiente, m.Estado);
        Assert.Equal(1, m.Intentos);
        // El backoff cuenta desde el momento del resultado, no desde el `ahora` de la pasada.
        Assert.True(m.ProximoIntentoEn >= Ahora + CteMensajesWhatsapp.Backoff(1));
        Assert.Equal("Sesión caída", m.UltimoError);
    }

    [Fact]
    public async Task Sin_respuesta_del_gateway_reintenta()
    {
        var m = Pendiente();
        _gateway.Respuestas.Enqueue(new RespuestaEnvioGateway(null, null, "No se pudo contactar al gateway de WhatsApp."));

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Pendiente, m.Estado);
        Assert.Equal(1, m.Intentos);
    }

    [Fact]
    public async Task Error_permanente_queda_fallido_sin_reintento()
    {
        var m = Pendiente();
        _gateway.Respuestas.Enqueue(new RespuestaEnvioGateway(400, null, "Número inválido"));

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Fallido, m.Estado);
        Assert.Equal("Número inválido", m.UltimoError);
    }

    [Fact]
    public async Task Sexto_fallo_transitorio_queda_fallido()
    {
        var m = Pendiente(intentos: CteMensajesWhatsapp.MaxIntentos - 1);
        _gateway.Respuestas.Enqueue(new RespuestaEnvioGateway(503, null, "Sesión caída"));

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Fallido, m.Estado);
        Assert.Equal(CteMensajesWhatsapp.MaxIntentos, m.Intentos);
    }

    [Fact]
    public async Task Mensaje_vencido_se_descarta_sin_llamar_al_gateway()
    {
        var m = Pendiente(creadoEn: Ahora - CteMensajesWhatsapp.Vigencia - TimeSpan.FromMinutes(1));

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Descartado, m.Estado);
        Assert.Empty(_gateway.Envios);
    }

    [Fact]
    public async Task Tenant_suspendido_descarta_sin_llamar_al_gateway()
    {
        _tenant.Estado = "suspendido";
        var m = Pendiente();

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Descartado, m.Estado);
        Assert.Empty(_gateway.Envios);
    }

    [Fact]
    public async Task Tenant_inexistente_descarta_con_motivo_propio_sin_llamar_al_gateway()
    {
        var m = Pendiente();
        m.TenantId = 99;

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Descartado, m.Estado);
        Assert.Equal("La cuenta no existe.", m.UltimoError);
        Assert.Empty(_gateway.Envios);
    }

    [Fact]
    public async Task Gateway_403_reintenta_en_vez_de_fallar_el_mensaje()
    {
        var m = Pendiente();
        _gateway.Respuestas.Enqueue(new RespuestaEnvioGateway(403, null, "Empresa inactiva"));

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Pendiente, m.Estado);
        Assert.Equal(1, m.Intentos);
    }

    [Fact]
    public async Task Tenant_desvinculado_descarta_sin_llamar_al_gateway()
    {
        _tenant.WhatsappTelefonoId = null;
        var m = Pendiente();

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Descartado, m.Estado);
        Assert.Empty(_gateway.Envios);
    }

    [Fact]
    public async Task Apagado_justo_despues_de_enviar_igual_guarda_el_resultado_y_no_sigue_con_el_lote()
    {
        var primero = Pendiente();
        var segundo = Pendiente();
        using var cts = new CancellationTokenSource();
        _gateway.AlEnviar = cts.Cancel;

        await Procesador().ProcesarAsync(Ahora, cts.Token);

        // Si el save se cancelara, el mensaje quedaría pendiente y saldría de nuevo al vencer el lease.
        Assert.Equal(CteMensajesWhatsapp.Enviado, primero.Estado);
        Assert.Equal(1, _uow.Guardados);
        Assert.Single(_gateway.Envios);
        Assert.Equal(CteMensajesWhatsapp.Pendiente, segundo.Estado);
    }

    [Fact]
    public async Task No_toma_los_que_todavia_no_vencieron()
    {
        var m = Pendiente();
        m.ProximoIntentoEn = Ahora.AddMinutes(5);

        var procesados = await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(0, procesados);
        Assert.Empty(_gateway.Envios);
    }

    [Fact]
    public async Task Cuenta_sin_terminos_vigentes_descarta_sin_llamar_al_gateway()
    {
        _tenant.TerminosVersionAceptada = null;
        var m = Pendiente();

        await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(CteMensajesWhatsapp.Descartado, m.Estado);
        Assert.Equal("La cuenta no aceptó los términos de uso vigentes.", m.UltimoError);
        Assert.Empty(_gateway.Envios);
    }
}
