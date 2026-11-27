using Application.Exceptions;
using Application.Services;
using Shared.Constantes;
using Shared.DTOs.Paciente;
using Shared.DTOs.Paginacion;
using Tests.Fakes;

namespace Tests.Services;

/// <summary>
/// El celular del paciente es a donde salen los avisos y la clave con la que se reconoce su
/// respuesta: tiene que quedar normalizado y ser único dentro de la cuenta.
/// </summary>
public class PacienteServiceTests
{
    private readonly PacienteRepositoryFake _repo = new();
    private readonly UnitOfWorkFake _uow = new();

    private PacienteService Servicio() => new(_repo, _uow, new RelojFake(new DateTimeOffset(2026, 12, 1, 12, 0, 0, TimeSpan.Zero)));

    private static PacienteGuardarDto Dto(string celular = "11 2345-6789") => new()
    {
        Nombre = " Lucía ", Apellido = " Gómez ", Celular = celular, Email = " ", Notas = "  prefiere la tarde "
    };

    [Fact]
    public async Task Crear_normaliza_celular_y_limpia_textos()
    {
        var dto = await Servicio().CrearAsync(Dto());

        Assert.Equal("Lucía", dto.Nombre);
        Assert.Equal("Gómez", dto.Apellido);
        Assert.Equal("5491123456789", dto.Celular);
        Assert.Null(dto.Email);
        Assert.Equal("prefiere la tarde", dto.Notas);
        Assert.True(dto.Activo);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Crear_con_celular_ambiguo_da_400()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto(celular: "15 2345 6789")));

        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(_repo.Pacientes);
    }

    [Fact]
    public async Task Crear_con_celular_de_otro_paciente_da_409_aunque_venga_escrito_distinto()
    {
        _repo.Agregar("Ana", "Ruiz", "5491123456789");

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto(celular: "+54 9 11 2345 6789")));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task La_carrera_del_celular_duplicado_tambien_da_409()
    {
        _uow.ProximoDuplicadoEnIndice = CteIndices.PacienteCelular;

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Dto()));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Editar_puede_conservar_su_propio_celular()
    {
        var p = _repo.Agregar("Ana", "Ruiz", "5491123456789");

        var dto = await Servicio().ActualizarAsync(p.Id, Dto());

        Assert.Equal("Lucía", dto.Nombre);
    }

    [Fact]
    public async Task Editar_un_paciente_inexistente_da_404()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().ActualizarAsync(99, Dto()));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Desactivar_lo_saca_del_listado_por_defecto()
    {
        var p = _repo.Agregar("Ana", "Ruiz", "5491123456789");
        _repo.Agregar("Bea", "Sosa", "5491100000000");

        await Servicio().CambiarActivoAsync(p.Id, false);
        var activos = await Servicio().ListarAsync(new PaginacionQuery(), null, incluirInactivos: false);
        var todos = await Servicio().ListarAsync(new PaginacionQuery(), null, incluirInactivos: true);

        Assert.Equal(1, activos.Total);
        Assert.Equal(2, todos.Total);
    }
}
