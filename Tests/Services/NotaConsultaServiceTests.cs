using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.NotaConsulta;
using Tests.Fakes;

namespace Tests.Services;

/// <summary>
/// Notas de consulta: solo el profesional; con turno, solo de un turno atendido del mismo paciente;
/// se editan dentro de las primeras 24 h guardando la versión anterior; después solo se anulan, con motivo.
/// </summary>
public class NotaConsultaServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly NotaConsultaRepositoryFake _notas = new();
    private readonly PacienteRepositoryFake _pacientes = new();
    private readonly TurnoRepositoryFake _turnos = new();
    private readonly UnitOfWorkFake _uow = new();
    private readonly RelojFake _reloj = new(Ahora);
    private readonly Paciente _ana;
    private readonly Paciente _beto;

    public NotaConsultaServiceTests()
    {
        _ana = _pacientes.Agregar("Ana", "Ruiz", "5491123456789");
        _beto = _pacientes.Agregar("Beto", "Sosa", "5491198765432");
    }

    private NotaConsultaService Servicio(string rol = CteRoles.Admin, int usuarioId = 10)
        => new(_notas, _pacientes, _turnos, new TenantContextFake(1, rol: rol), new SecurityContextServiceFake(usuarioId), _uow, _reloj);

    private Turno Turno(Paciente paciente, string estado = CteEstadosTurno.Atendido)
    {
        var inicio = Ahora.UtcDateTime.AddHours(-2);
        var turno = new Turno { PacienteId = paciente.Id, Paciente = paciente, Inicio = inicio, Fin = inicio.AddMinutes(45), Estado = estado };
        _turnos.Agregar(turno);
        return turno;
    }

    private Task<NotaConsultaDto> Crear(string texto = "Primera consulta.", int? turnoId = null)
        => Servicio().CrearAsync(_ana.Id, new NotaConsultaCrearDto { Texto = texto, TurnoId = turnoId });

    [Fact]
    public async Task Crear_nota_suelta()
    {
        var nota = await Crear("  Llamó para avisar que mejoró.  ");

        Assert.Equal("Llamó para avisar que mejoró.", nota.Texto); // se recortan los espacios
        Assert.Null(nota.TurnoId);
        Assert.Equal(1, nota.Version);
        Assert.True(nota.Editable);
        Assert.Equal(Ahora.AddHours(CteNotasConsulta.HorasEdicion), nota.EditableHasta);
        Assert.Equal(10, _notas.Notas.Single().CreadaPor);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Crear_con_turno_atendido_del_paciente()
    {
        var turno = Turno(_ana);

        var nota = await Crear(turnoId: turno.Id);

        Assert.Equal(turno.Id, nota.TurnoId);
        Assert.Equal(new DateTimeOffset(turno.Inicio, TimeSpan.Zero), nota.TurnoInicio);
    }

    [Fact]
    public async Task Crear_con_turno_de_otro_paciente_da_400()
    {
        var turnoDeBeto = Turno(_beto);

        var ex = await Assert.ThrowsAsync<AppException>(() => Crear(turnoId: turnoDeBeto.Id));

        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(_notas.Notas);
    }

    [Theory]
    [InlineData(CteEstadosTurno.Pendiente)]
    [InlineData(CteEstadosTurno.Confirmado)]
    [InlineData(CteEstadosTurno.Ausente)]
    [InlineData(CteEstadosTurno.Cancelado)]
    public async Task Crear_con_turno_no_atendido_da_400(string estado)
    {
        var turno = Turno(_ana, estado);

        var ex = await Assert.ThrowsAsync<AppException>(() => Crear(turnoId: turno.Id));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Crear_con_turno_inexistente_da_400()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Crear(turnoId: 99));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_vacia_da_400(string texto)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Crear(texto));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Crear_demasiado_larga_da_400()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Crear(new string('a', CteNotasConsulta.MaxTexto + 1)));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Crear_para_paciente_inexistente_da_404()
    {
        var ex = await Assert.ThrowsAsync<AppException>(()
            => Servicio().CrearAsync(99, new NotaConsultaCrearDto { Texto = "x" }));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Editar_dentro_de_la_ventana_guarda_la_version_anterior()
    {
        var nota = await Crear("Texto original.");
        _reloj.Ahora = Ahora.AddHours(3);

        var editada = await Servicio(usuarioId: 11).EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "Texto corregido.", Version = 1 });

        Assert.Equal("Texto corregido.", editada.Texto);
        Assert.Equal(2, editada.Version);
        Assert.Equal(Ahora.AddHours(3), editada.EditadaEn);

        var anterior = Assert.Single(_notas.Versiones);
        Assert.Equal(1, anterior.Version);
        Assert.Equal("Texto original.", anterior.Texto);
        Assert.Equal(Ahora.UtcDateTime, anterior.GuardadaEn);
        Assert.Equal(10, anterior.GuardadaPor); // quien la había escrito, no quien la edita
    }

    [Fact]
    public async Task Dos_ediciones_dejan_dos_versiones_y_la_lista_trae_la_vigente_primero()
    {
        var nota = await Crear("v1");
        await Servicio().EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "v2", Version = 1 });
        await Servicio().EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "v3", Version = 2 });

        var versiones = await Servicio().VersionesAsync(nota.Id);

        Assert.Equal(["v3", "v2", "v1"], versiones.Select(v => v.Texto));
        Assert.Equal([3, 2, 1], versiones.Select(v => v.Version));
        Assert.True(versiones[0].Vigente);
        Assert.All(versiones.Skip(1), v => Assert.False(v.Vigente));
    }

    [Fact]
    public async Task Editar_sin_cambios_no_crea_version()
    {
        var nota = await Crear("Igual.");

        var editada = await Servicio().EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "Igual. ", Version = 1 });

        Assert.Equal(1, editada.Version);
        Assert.Empty(_notas.Versiones);
    }

    [Fact]
    public async Task Editar_pasadas_las_24_horas_da_409()
    {
        var nota = await Crear();
        _reloj.Ahora = Ahora.AddHours(CteNotasConsulta.HorasEdicion);

        var ex = await Assert.ThrowsAsync<AppException>(()
            => Servicio().EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "Tarde.", Version = 1 }));

        Assert.Equal(409, ex.StatusCode);
        Assert.Empty(_notas.Versiones);
    }

    [Fact]
    public async Task Pasadas_las_24_horas_la_lista_la_muestra_no_editable()
    {
        await Crear();
        _reloj.Ahora = Ahora.AddHours(CteNotasConsulta.HorasEdicion + 1);

        var lista = await Servicio().ListarAsync(_ana.Id, incluirAnuladas: true);

        Assert.False(Assert.Single(lista).Editable);
    }

    [Fact]
    public async Task Editar_con_version_vieja_da_409()
    {
        var nota = await Crear("v1");
        await Servicio().EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "v2", Version = 1 });

        // Otra pestaña seguía mostrando la versión 1.
        var ex = await Assert.ThrowsAsync<AppException>(()
            => Servicio().EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "pisaría v2", Version = 1 }));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("v2", _notas.Notas.Single().Texto);
    }

    [Fact]
    public async Task Anular_guarda_el_motivo_y_conserva_el_texto()
    {
        var nota = await Crear("Se cargó en el paciente equivocado.");

        var anulada = await Servicio().AnularAsync(nota.Id, new NotaConsultaAnularDto { Motivo = " Paciente equivocado " });

        Assert.True(anulada.Anulada);
        Assert.False(anulada.Editable);
        Assert.Equal("Paciente equivocado", anulada.MotivoAnulacion);
        Assert.Equal("Se cargó en el paciente equivocado.", anulada.Texto);
        Assert.Single(_notas.Notas); // no se borró
    }

    [Fact]
    public async Task Anular_sin_motivo_da_400()
    {
        var nota = await Crear();

        var ex = await Assert.ThrowsAsync<AppException>(()
            => Servicio().AnularAsync(nota.Id, new NotaConsultaAnularDto { Motivo = "  " }));

        Assert.Equal(400, ex.StatusCode);
        Assert.False(_notas.Notas.Single().Anulada);
    }

    [Fact]
    public async Task Anular_dos_veces_da_409()
    {
        var nota = await Crear();
        await Servicio().AnularAsync(nota.Id, new NotaConsultaAnularDto { Motivo = "Error" });

        var ex = await Assert.ThrowsAsync<AppException>(()
            => Servicio().AnularAsync(nota.Id, new NotaConsultaAnularDto { Motivo = "Otra vez" }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Una_nota_anulada_no_se_edita()
    {
        var nota = await Crear();
        await Servicio().AnularAsync(nota.Id, new NotaConsultaAnularDto { Motivo = "Error" });

        var ex = await Assert.ThrowsAsync<AppException>(()
            => Servicio().EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "Nuevo", Version = 1 }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Listar_trae_solo_las_del_paciente_de_la_mas_nueva_a_la_mas_vieja()
    {
        await Crear("vieja");
        _reloj.Ahora = Ahora.AddDays(7);
        await Crear("nueva");
        await Servicio().CrearAsync(_beto.Id, new NotaConsultaCrearDto { Texto = "de Beto" });

        var lista = await Servicio().ListarAsync(_ana.Id, incluirAnuladas: true);

        Assert.Equal(["nueva", "vieja"], lista.Select(n => n.Texto));
    }

    [Fact]
    public async Task Listar_puede_dejar_afuera_las_anuladas()
    {
        var nota = await Crear("anulada");
        await Crear("vigente");
        await Servicio().AnularAsync(nota.Id, new NotaConsultaAnularDto { Motivo = "Error" });

        Assert.Equal(2, (await Servicio().ListarAsync(_ana.Id, incluirAnuladas: true)).Count);
        Assert.Equal("vigente", Assert.Single(await Servicio().ListarAsync(_ana.Id, incluirAnuladas: false)).Texto);
    }

    [Fact]
    public async Task Un_rol_que_no_es_el_profesional_recibe_403_en_todo()
    {
        var nota = await Crear();
        var secretaria = Servicio(CteRoles.Secretaria);

        foreach (var accion in new Func<Task>[]
        {
            () => secretaria.ListarAsync(_ana.Id, true),
            () => secretaria.CrearAsync(_ana.Id, new NotaConsultaCrearDto { Texto = "x" }),
            () => secretaria.EditarAsync(nota.Id, new NotaConsultaEditarDto { Texto = "x", Version = 1 }),
            () => secretaria.AnularAsync(nota.Id, new NotaConsultaAnularDto { Motivo = "x" }),
            () => secretaria.VersionesAsync(nota.Id),
        })
        {
            var ex = await Assert.ThrowsAsync<AppException>(accion);
            Assert.Equal(403, ex.StatusCode);
        }
    }

    [Fact]
    public async Task Nota_inexistente_da_404()
    {
        var ex = await Assert.ThrowsAsync<AppException>(()
            => Servicio().EditarAsync(99, new NotaConsultaEditarDto { Texto = "x", Version = 1 }));
        Assert.Equal(404, ex.StatusCode);
    }
}
