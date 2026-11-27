using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Tests.Fakes;

namespace Tests.Services;

/// <summary>Ficha del paciente: resumen de sus turnos, próximo turno y cantidad de notas por turno.</summary>
public class FichaPacienteServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PacienteRepositoryFake _pacientes = new();
    private readonly TurnoRepositoryFake _turnos = new();
    private readonly NotaConsultaRepositoryFake _notas = new();
    private readonly Paciente _ana;
    private readonly Paciente _beto;
    private readonly Sucursal _consultorio = new() { Id = 1, Nombre = "Palermo" };

    public FichaPacienteServiceTests()
    {
        _ana = _pacientes.Agregar("Ana", "Ruiz", "5491123456789");
        _beto = _pacientes.Agregar("Beto", "Sosa", "5491198765432");
    }

    private FichaPacienteService Servicio() => new(_pacientes, _turnos, _notas, new RelojFake(Ahora));

    private Turno Turno(Paciente p, int dias, string estado)
    {
        var inicio = Ahora.UtcDateTime.AddDays(dias);
        var t = new Turno
        {
            PacienteId = p.Id, Paciente = p, SucursalId = 1, Sucursal = _consultorio,
            Inicio = inicio, Fin = inicio.AddMinutes(45), Estado = estado
        };
        _turnos.Agregar(t);
        return t;
    }

    [Fact]
    public async Task Resume_los_turnos_del_paciente_y_cuenta_las_notas()
    {
        var atendido = Turno(_ana, -14, CteEstadosTurno.Atendido);
        Turno(_ana, -7, CteEstadosTurno.Ausente);
        Turno(_ana, -3, CteEstadosTurno.Cancelado);
        var proximo = Turno(_ana, 2, CteEstadosTurno.Confirmado);
        Turno(_ana, 9, CteEstadosTurno.Pendiente);
        Turno(_beto, 1, CteEstadosTurno.Pendiente);

        _notas.Agregar(new NotaConsulta { PacienteId = _ana.Id, TurnoId = atendido.Id, Texto = "a" });
        _notas.Agregar(new NotaConsulta { PacienteId = _ana.Id, TurnoId = atendido.Id, Texto = "aclaración" });
        _notas.Agregar(new NotaConsulta { PacienteId = _ana.Id, TurnoId = atendido.Id, Texto = "anulada", Anulada = true });

        var ficha = await Servicio().ObtenerAsync(_ana.Id);

        Assert.Equal("Ana", ficha.Paciente.Nombre);
        Assert.Equal(5, ficha.TotalTurnos);
        Assert.Equal(1, ficha.Atendidos);
        Assert.Equal(1, ficha.Ausentes);
        Assert.Equal(1, ficha.Cancelados);
        Assert.Equal(proximo.Id, ficha.ProximoTurno!.Id);
        Assert.Equal(ficha.Turnos.Select(t => t.Inicio).OrderByDescending(i => i), ficha.Turnos.Select(t => t.Inicio));
        Assert.Equal(2, ficha.Turnos.Single(t => t.Id == atendido.Id).CantidadNotas); // la anulada no cuenta
    }

    [Fact]
    public async Task Sin_turnos_futuros_no_hay_proximo()
    {
        Turno(_ana, -1, CteEstadosTurno.Atendido);
        Turno(_ana, 3, CteEstadosTurno.Cancelado);

        var ficha = await Servicio().ObtenerAsync(_ana.Id);

        Assert.Null(ficha.ProximoTurno);
    }

    [Fact]
    public async Task Paciente_inexistente_da_404()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().ObtenerAsync(99));
        Assert.Equal(404, ex.StatusCode);
    }
}
