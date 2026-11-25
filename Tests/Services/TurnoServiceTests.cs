using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Turno;
using Tests.Fakes;

namespace Tests.Services;

/// <summary>
/// Reglas de la agenda: sin turnos en el pasado, sin superposiciones entre turnos vivos, y un turno
/// movido de horario vuelve a pendiente porque la confirmación del paciente era para el anterior.
/// </summary>
public class TurnoServiceTests
{
    // "Ahora" = 1/12/2026 12:00 UTC (09:00 en Argentina).
    private static readonly DateTimeOffset Ahora = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Ar = TimeSpan.FromHours(-3);

    private readonly TurnoRepositoryFake _turnos = new();
    private readonly PacienteRepositoryFake _pacientes = new();
    private readonly SucursalRepositoryFake _sucursales = new();
    private readonly ConfiguracionRepositoryFake _config = new(new Configuracion { DuracionTurnoMinutos = 45 });
    private readonly UnitOfWorkFake _uow = new();
    private readonly AvisoTurnoServiceFake _avisos = new();
    private readonly RelojFake _reloj = new(Ahora);
    private readonly Paciente _ana;
    private readonly Sucursal _palermo;

    public TurnoServiceTests()
    {
        _ana = _pacientes.Agregar("Ana", "Ruiz", "5491123456789");
        _palermo = _sucursales.Agregar("Palermo");
    }

    private TurnoService Servicio() => new(_turnos, _pacientes, _sucursales, _config, _avisos, _uow, _reloj);

    private TurnoGuardarDto Dto(int hora, int minuto = 0, int? duracion = null, int? sucursalId = null, int dia = 2) => new()
    {
        PacienteId = _ana.Id,
        SucursalId = sucursalId,
        Inicio = new DateTimeOffset(2026, 12, dia, hora, minuto, 0, Ar),
        DuracionMinutos = duracion
    };

    [Fact]
    public async Task Crear_usa_la_duracion_de_la_configuracion_y_queda_pendiente()
    {
        var turno = await Servicio().CrearAsync(Dto(15));

        Assert.Equal(CteEstadosTurno.Pendiente, turno.Estado);
        Assert.Equal(new DateTimeOffset(2026, 12, 2, 18, 0, 0, TimeSpan.Zero), turno.Inicio); // guardado en UTC
        Assert.Equal(TimeSpan.FromMinutes(45), turno.Fin - turno.Inicio);
        Assert.Equal("Ana Ruiz", turno.PacienteNombre);
        Assert.Equal("Palermo", turno.SucursalNombre); // único consultorio: se elige solo
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Crear_en_el_pasado_da_400()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto(8, dia: 1))); // 08:00 AR < 09:00 AR

        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(_turnos.Turnos);
    }

    [Fact]
    public async Task Crear_superpuesto_con_otro_turno_da_409()
    {
        await Servicio().CrearAsync(Dto(15));

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto(15, 30)));

        Assert.Equal(409, ex.StatusCode);
        Assert.Single(_turnos.Turnos);
    }

    [Fact]
    public async Task Turnos_pegados_no_se_superponen()
    {
        await Servicio().CrearAsync(Dto(15, duracion: 60));

        await Servicio().CrearAsync(Dto(16)); // empieza justo cuando termina el anterior

        Assert.Equal(2, _turnos.Turnos.Count);
    }

    [Fact]
    public async Task Un_turno_cancelado_libera_el_horario()
    {
        var primero = await Servicio().CrearAsync(Dto(15));
        await Servicio().CambiarEstadoAsync(primero.Id, new TurnoEstadoDto { Estado = "cancelado" });

        await Servicio().CrearAsync(Dto(15));

        Assert.Equal(2, _turnos.Turnos.Count);
    }

    [Fact]
    public async Task La_carrera_de_dos_turnos_superpuestos_tambien_da_409()
    {
        _uow.ProximoDuplicadoEnIndice = CteIndices.TurnoSinSolapamiento;

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto(15)));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Con_varios_consultorios_hay_que_elegir()
    {
        _sucursales.Agregar("Belgrano");

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto(15)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Consultorio_inactivo_da_400()
    {
        var cerrado = _sucursales.Agregar("Cerrado", activa: false);

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto(15, sucursalId: cerrado.Id)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Paciente_desactivado_no_recibe_turnos_nuevos()
    {
        _ana.Activo = false;

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto(15)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Reprogramar_vuelve_a_pendiente_un_turno_confirmado()
    {
        var turno = await Servicio().CrearAsync(Dto(15));
        await Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = "confirmado" });

        var movido = await Servicio().ReprogramarAsync(turno.Id, Dto(17));

        Assert.Equal(CteEstadosTurno.Pendiente, movido.Estado);
        Assert.Equal(new DateTimeOffset(2026, 12, 2, 20, 0, 0, TimeSpan.Zero), movido.Inicio);
    }

    [Fact]
    public async Task Editar_solo_las_notas_no_pierde_la_confirmacion()
    {
        var turno = await Servicio().CrearAsync(Dto(15));
        await Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = "confirmado" });

        var dto = Dto(15);
        dto.Notas = "trae estudios";
        var editado = await Servicio().ReprogramarAsync(turno.Id, dto);

        Assert.Equal(CteEstadosTurno.Confirmado, editado.Estado);
        Assert.Equal("trae estudios", editado.Notas);
    }

    [Fact]
    public async Task Reprogramar_sobre_si_mismo_no_cuenta_como_superposicion()
    {
        var turno = await Servicio().CrearAsync(Dto(15, duracion: 60));

        var movido = await Servicio().ReprogramarAsync(turno.Id, Dto(15, 30, duracion: 60));

        Assert.Equal(new DateTimeOffset(2026, 12, 2, 18, 30, 0, TimeSpan.Zero), movido.Inicio);
    }

    [Fact]
    public async Task Un_turno_cancelado_no_se_puede_modificar()
    {
        var turno = await Servicio().CrearAsync(Dto(15));
        await Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = "cancelado" });

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().ReprogramarAsync(turno.Id, Dto(17)));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Cancelar_registra_quien_y_por_que()
    {
        var turno = await Servicio().CrearAsync(Dto(15));

        var cancelado = await Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = " Cancelado ", Motivo = " viaje " });

        Assert.Equal(CteEstadosTurno.Cancelado, cancelado.Estado);
        Assert.Equal(CteEstadosTurno.CanceladoPorProfesional, cancelado.CanceladoPor);
        Assert.Equal("viaje", cancelado.MotivoCancelacion);
    }

    [Fact]
    public async Task Un_cancelado_no_se_descancela()
    {
        var turno = await Servicio().CrearAsync(Dto(15));
        await Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = "cancelado" });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = "confirmado" }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Atendido_solo_cuando_el_turno_ya_empezo()
    {
        var turno = await Servicio().CrearAsync(Dto(15));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = "atendido" }));
        Assert.Equal(409, ex.StatusCode);

        _reloj.Ahora = new DateTimeOffset(2026, 12, 2, 15, 10, 0, Ar);
        var atendido = await Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = "atendido" });
        Assert.Equal(CteEstadosTurno.Atendido, atendido.Estado);
    }

    [Fact]
    public async Task Estado_desconocido_da_400()
    {
        var turno = await Servicio().CrearAsync(Dto(15));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Servicio().CambiarEstadoAsync(turno.Id, new TurnoEstadoDto { Estado = "borrado" }));

        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [InlineData(0)]    // hasta == desde
    [InlineData(-1)]   // invertido
    [InlineData(63)]   // demasiado largo
    public async Task Rango_invalido_da_400(int dias)
    {
        var desde = new DateTimeOffset(2026, 12, 1, 0, 0, 0, Ar);

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().ListarAsync(desde, desde.AddDays(dias)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Listar_devuelve_los_turnos_que_se_cruzan_con_el_rango_incluidos_cancelados()
    {
        var a = await Servicio().CrearAsync(Dto(15, dia: 2));
        await Servicio().CrearAsync(Dto(15, dia: 9));
        await Servicio().CambiarEstadoAsync(a.Id, new TurnoEstadoDto { Estado = "cancelado" });

        var semana = await Servicio().ListarAsync(
            new DateTimeOffset(2026, 11, 30, 0, 0, 0, Ar), new DateTimeOffset(2026, 12, 7, 0, 0, 0, Ar));

        var unico = Assert.Single(semana);
        Assert.Equal(CteEstadosTurno.Cancelado, unico.Estado);
    }

    // --- Avisos por WhatsApp que dispara la agenda ---

    [Fact]
    public async Task Crear_encola_el_aviso_al_paciente()
    {
        await Servicio().CrearAsync(Dto(15));

        var (turno, tipo) = Assert.Single(_avisos.Encolados);
        Assert.Equal(CteMensajesWhatsapp.TipoAviso, tipo);
        Assert.Equal(_ana.Id, turno.PacienteId);
    }

    [Fact]
    public async Task Mover_la_hora_avisa_el_cambio_y_rearma_el_recordatorio()
    {
        var creado = await Servicio().CrearAsync(Dto(15));
        _turnos.Turnos.Single().RecordatorioEncoladoEn = Ahora.UtcDateTime;

        await Servicio().ReprogramarAsync(creado.Id, Dto(17));

        Assert.Equal(CteMensajesWhatsapp.TipoReprogramado, _avisos.Encolados.Last().Tipo);
        Assert.Null(_turnos.Turnos.Single().RecordatorioEncoladoEn);
    }

    [Fact]
    public async Task Estirar_un_turno_confirmado_no_lo_desconfirma_ni_avisa()
    {
        var creado = await Servicio().CrearAsync(Dto(15, duracion: 45));
        await Servicio().CambiarEstadoAsync(creado.Id, new TurnoEstadoDto { Estado = "confirmado" });

        var estirado = await Servicio().ReprogramarAsync(creado.Id, Dto(15, duracion: 60));

        Assert.Equal(CteEstadosTurno.Confirmado, estirado.Estado);
        Assert.Single(_avisos.Encolados); // solo el del alta
    }

    [Fact]
    public async Task Cancelar_avisa_al_paciente_y_confirmar_no()
    {
        var creado = await Servicio().CrearAsync(Dto(15));
        await Servicio().CambiarEstadoAsync(creado.Id, new TurnoEstadoDto { Estado = "confirmado" });
        await Servicio().CambiarEstadoAsync(creado.Id, new TurnoEstadoDto { Estado = "cancelado" });

        Assert.Equal([CteMensajesWhatsapp.TipoAviso, CteMensajesWhatsapp.TipoCancelado],
            _avisos.Encolados.Select(a => a.Tipo).ToArray());
    }
}
