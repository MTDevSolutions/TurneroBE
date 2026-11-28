using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Tests.Fakes;

namespace Tests.Services;

public class AvisoTurnoServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly TenantRepositoryFake _tenants = new();
    private readonly MensajeWhatsappRepositoryFake _mensajes = new();
    private readonly PlantillaMensajeRepositoryFake _plantillas = new();
    private readonly Configuracion _config = new() { AvisosWhatsappActivos = true, HorasRecordatorio = 24 };

    public AvisoTurnoServiceTests()
        => _tenants.Agregar(new Tenant { Id = 1, Nombre = "Lic. Laura Pérez", Especialidad = "psicologia", Estado = "activo" });

    private AvisoTurnoService Servicio() => new(
        new TenantContextFake(1), _tenants, new ConfiguracionRepositoryFake(_config), _mensajes, _plantillas, new RelojFake(Ahora));

    private static Turno Turno(DateTime inicioUtc) => new()
    {
        Id = 5,
        TenantId = 1,
        Inicio = inicioUtc,
        Fin = inicioUtc.AddMinutes(45),
        Estado = CteEstadosTurno.Pendiente,
        Paciente = new Paciente { Nombre = "Ana", Apellido = "Ruiz", Celular = "5491123456789" },
        Sucursal = new Sucursal { Nombre = "Consultorio Palermo", Direccion = "Gorriti 1234" },
    };

    [Fact]
    public async Task Aviso_va_al_celular_del_paciente_con_fecha_lugar_y_pedido_de_respuesta()
    {
        var turno = Turno(new DateTime(2026, 12, 3, 18, 0, 0, DateTimeKind.Utc)); // jueves 15:00 AR

        await Servicio().EncolarAsync(turno, CteMensajesWhatsapp.TipoAviso);

        var m = Assert.Single(_mensajes.Mensajes);
        Assert.Equal("5491123456789", m.Destino);
        Assert.Equal(1, m.TenantId);
        Assert.Equal(CteMensajesWhatsapp.Pendiente, m.Estado);
        Assert.Contains("Ana", m.Texto);
        Assert.Contains("Lic. Laura Pérez", m.Texto);
        Assert.Contains("jueves 3/12 a las 15:00", m.Texto);
        Assert.Contains("Consultorio Palermo (Gorriti 1234)", m.Texto);
        Assert.Contains("*SÍ*", m.Texto);
        Assert.Null(turno.RecordatorioEncoladoEn); // faltan más de 24 h: el recordatorio sale después
    }

    [Fact]
    public async Task Turno_dentro_de_la_ventana_no_tiene_recordatorio_aparte()
    {
        var turno = Turno(Ahora.UtcDateTime.AddHours(5));

        await Servicio().EncolarAsync(turno, CteMensajesWhatsapp.TipoAviso);

        Assert.Equal(Ahora.UtcDateTime, turno.RecordatorioEncoladoEn);
    }

    [Fact]
    public async Task Con_los_avisos_apagados_no_encola_nada()
    {
        _config.AvisosWhatsappActivos = false;

        await Servicio().EncolarAsync(Turno(Ahora.UtcDateTime.AddDays(2)), CteMensajesWhatsapp.TipoAviso);

        Assert.Empty(_mensajes.Mensajes);
    }

    [Fact]
    public async Task Turno_que_ya_empezo_no_se_avisa()
    {
        await Servicio().EncolarAsync(Turno(Ahora.UtcDateTime.AddMinutes(-10)), CteMensajesWhatsapp.TipoCancelado);

        Assert.Empty(_mensajes.Mensajes);
    }

    [Fact]
    public async Task Cancelacion_avisa_sin_pedir_respuesta()
    {
        await Servicio().EncolarAsync(Turno(Ahora.UtcDateTime.AddDays(2)), CteMensajesWhatsapp.TipoCancelado);

        var m = Assert.Single(_mensajes.Mensajes);
        Assert.Contains("se canceló", m.Texto);
        Assert.DoesNotContain("*SÍ*", m.Texto);
    }

    [Fact]
    public async Task Un_aviso_nuevo_descarta_los_pendientes_del_horario_anterior()
    {
        var turno = Turno(new DateTime(2026, 12, 3, 18, 0, 0, DateTimeKind.Utc));
        await Servicio().EncolarAsync(turno, CteMensajesWhatsapp.TipoAviso);
        var viejo = _mensajes.Mensajes.Single();

        turno.Inicio = turno.Inicio.AddHours(2);
        await Servicio().EncolarAsync(turno, CteMensajesWhatsapp.TipoReprogramado);

        Assert.Equal(CteMensajesWhatsapp.Descartado, viejo.Estado);
        Assert.Equal(CteMensajesWhatsapp.Pendiente, _mensajes.Mensajes.Last().Estado);
    }

    [Fact]
    public async Task Usa_el_texto_propio_de_la_cuenta_y_conserva_el_pedido_de_respuesta()
    {
        _plantillas.Con(1, CtePlantillasMensaje.Aviso, "Hola {paciente}! Turno el {fecha} a las {hora} en {lugar}.")
            .Con(2, CtePlantillasMensaje.Aviso, "Texto de otra cuenta");
        var turno = Turno(new DateTime(2026, 12, 3, 18, 0, 0, DateTimeKind.Utc));

        await Servicio().EncolarAsync(turno, CteMensajesWhatsapp.TipoAviso);

        Assert.Equal(
            $"Hola Ana! Turno el jueves 3/12 a las 15:00 en Consultorio Palermo (Gorriti 1234).\n\n{CtePlantillasMensaje.PiePedidoConfirmacion}",
            Assert.Single(_mensajes.Mensajes).Texto);
    }
}
