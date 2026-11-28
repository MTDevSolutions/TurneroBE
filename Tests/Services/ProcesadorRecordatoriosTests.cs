using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Tests.Fakes;

namespace Tests.Services;

public class ProcesadorRecordatoriosTests
{
    private static readonly DateTime Ahora = new(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly TurnoRepositoryFake _turnos = new();
    private readonly TenantRepositoryFake _tenants = new();
    private readonly MensajeWhatsappRepositoryFake _mensajes = new();
    private readonly UnitOfWorkFake _uow = new();
    private readonly PlantillaMensajeRepositoryFake _plantillas = new();

    public ProcesadorRecordatoriosTests()
    {
        _tenants.Agregar(new Tenant { Id = 1, Nombre = "Lic. Uno", Especialidad = "psicologia", Estado = "activo" });
        _tenants.Agregar(new Tenant { Id = 2, Nombre = "Lic. Dos", Especialidad = "fonoaudiologia", Estado = "activo" });
        _turnos.Configuraciones[1] = new Configuracion { TenantId = 1, HorasRecordatorio = 24, AvisosWhatsappActivos = true };
        _turnos.Configuraciones[2] = new Configuracion { TenantId = 2, HorasRecordatorio = 2, AvisosWhatsappActivos = true };
    }

    private ProcesadorRecordatorios Procesador() => new(_turnos, _tenants, _mensajes, _plantillas, _uow);

    private Turno Turno(int tenantId, DateTime inicio, string estado = CteEstadosTurno.Pendiente)
    {
        var t = new Turno
        {
            TenantId = tenantId, Inicio = inicio, Fin = inicio.AddMinutes(45), Estado = estado,
            Paciente = new Paciente { Nombre = "Ana", Apellido = "Ruiz", Celular = "5491123456789" },
            Sucursal = new Sucursal { Nombre = "Consultorio" },
        };
        _turnos.Agregar(t);
        return t;
    }

    [Fact]
    public async Task Encola_solo_los_que_entraron_en_la_ventana_de_su_cuenta()
    {
        var manana = Turno(1, Ahora.AddHours(20));   // cuenta 1: 24 h → sí
        var lejos = Turno(1, Ahora.AddHours(30));    // cuenta 1: todavía no
        var enTres = Turno(2, Ahora.AddHours(3));    // cuenta 2: 2 h → todavía no
        var enUna = Turno(2, Ahora.AddHours(1));     // cuenta 2: sí

        var encolados = await Procesador().ProcesarAsync(Ahora, default);

        Assert.Equal(2, encolados);
        Assert.Equal([manana.Id, enUna.Id], _mensajes.Mensajes.Select(m => m.TurnoId!.Value).Order().ToArray());
        Assert.All(_mensajes.Mensajes, m => Assert.Equal(CteMensajesWhatsapp.TipoRecordatorio, m.Tipo));
        Assert.Equal(Ahora, manana.RecordatorioEncoladoEn);
        Assert.Null(lejos.RecordatorioEncoladoEn);
        Assert.Null(enTres.RecordatorioEncoladoEn);
        Assert.Equal(2, _mensajes.Mensajes.Single(m => m.TurnoId == enUna.Id).TenantId);
    }

    [Fact]
    public async Task No_repite_el_recordatorio()
    {
        Turno(1, Ahora.AddHours(20));

        await Procesador().ProcesarAsync(Ahora, default);
        var segunda = await Procesador().ProcesarAsync(Ahora.AddMinutes(1), default);

        Assert.Equal(0, segunda);
        Assert.Single(_mensajes.Mensajes);
    }

    [Fact]
    public async Task Cancelados_y_cuentas_con_avisos_apagados_no_reciben_recordatorio()
    {
        Turno(1, Ahora.AddHours(20), CteEstadosTurno.Cancelado);
        _turnos.Configuraciones[2].AvisosWhatsappActivos = false;
        Turno(2, Ahora.AddHours(1));

        Assert.Equal(0, await Procesador().ProcesarAsync(Ahora, default));
        Assert.Empty(_mensajes.Mensajes);
    }

    [Fact]
    public async Task Confirmado_recibe_un_recordatorio_que_no_vuelve_a_pedir_confirmar()
    {
        Turno(1, Ahora.AddHours(20), CteEstadosTurno.Confirmado);

        await Procesador().ProcesarAsync(Ahora, default);

        var texto = Assert.Single(_mensajes.Mensajes).Texto;
        Assert.Contains("¡Te esperamos!", texto);
        Assert.DoesNotContain("*SÍ*", texto);
    }

    [Fact]
    public async Task Cada_cuenta_usa_su_propio_texto_de_recordatorio()
    {
        _plantillas.Con(1, CtePlantillasMensaje.Recordatorio, "Cuenta uno: {paciente}");
        var uno = Turno(1, Ahora.AddHours(20));
        var dos = Turno(2, Ahora.AddHours(1));

        await Procesador().ProcesarAsync(Ahora, default);

        var deUno = _mensajes.Mensajes.Single(m => m.TurnoId == uno.Id).Texto;
        var deDos = _mensajes.Mensajes.Single(m => m.TurnoId == dos.Id).Texto;
        Assert.Equal($"Cuenta uno: Ana\n\n{CtePlantillasMensaje.PiePedidoConfirmacion}", deUno);
        Assert.StartsWith("¡Hola Ana! Te recordamos tu turno con Lic. Dos", deDos);
    }
}
