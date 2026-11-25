using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Terminos;
using Tests.Fakes;

namespace Tests.Services;

/// <summary>
/// Aceptación de los términos de uso: explícita, de la versión que se leyó, y solo por el titular.
/// </summary>
public class TerminosServiceTests
{
    private const int TenantId = 7;
    private readonly TenantRepositoryFake _repo = new();
    private readonly TenantEstadoServiceFake _estado = new();
    private readonly UnitOfWorkFake _uow = new();

    public TerminosServiceTests()
        => _repo.Agregar(new Tenant { Id = TenantId, Nombre = "X", Especialidad = "psicologia" });

    private TerminosService Servicio(string rol = CteRoles.Admin) => new(
        _repo, new TenantContextFake(TenantId, rol: rol), new SecurityContextServiceFake(id: 3), _estado, _uow);

    [Fact]
    public async Task Sin_aceptar_devuelve_el_texto_vigente_como_pendiente()
    {
        var terminos = await Servicio().ObtenerAsync();

        Assert.Equal(CteTerminos.VersionVigente, terminos.Version);
        Assert.Contains("no oficial", terminos.Texto);
        Assert.False(terminos.Aceptados);
    }

    [Fact]
    public async Task Aceptar_registra_version_fecha_y_usuario_e_invalida_el_cache()
    {
        var terminos = await Servicio().AceptarAsync(new TerminosAceptarDto { Version = CteTerminos.VersionVigente });

        var tenant = (await _repo.GetByIdAsync(TenantId))!;
        Assert.True(terminos.Aceptados);
        Assert.Equal(CteTerminos.VersionVigente, tenant.TerminosVersionAceptada);
        Assert.NotNull(tenant.TerminosAceptadosEn);
        Assert.Equal(3, tenant.TerminosAceptadosPor);
        Assert.Equal(1, _uow.Guardados);
        Assert.Contains(TenantId, _estado.Invalidados); // si no, el middleware sigue bloqueando
    }

    [Fact]
    public async Task Aceptar_una_version_vieja_da_409_y_no_guarda()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Servicio().AceptarAsync(new TerminosAceptarDto { Version = "2020-01-01" }));

        Assert.Equal(409, ex.StatusCode);
        Assert.Null((await _repo.GetByIdAsync(TenantId))!.TerminosVersionAceptada);
        Assert.Equal(0, _uow.Guardados);
    }

    [Fact]
    public async Task Solo_el_titular_puede_aceptar()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Servicio(rol: CteRoles.Secretaria).AceptarAsync(new TerminosAceptarDto { Version = CteTerminos.VersionVigente }));

        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(0, _uow.Guardados);
    }

    [Fact]
    public async Task Despues_de_aceptar_figuran_como_aceptados()
    {
        await Servicio().AceptarAsync(new TerminosAceptarDto { Version = CteTerminos.VersionVigente });

        Assert.True((await Servicio().ObtenerAsync()).Aceptados);
    }
}
