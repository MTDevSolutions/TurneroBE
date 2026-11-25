using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Tests.Fakes;

namespace Tests.Services;

public class TenantAdminServiceWhatsappTests
{
    private readonly TenantRepositoryFake _tenants = new();
    private readonly WhatsappGatewayFake _gateway = new();
    private readonly UnitOfWorkFake _uow = new();
    private readonly Tenant _tenant;

    public TenantAdminServiceWhatsappTests()
        => _tenant = _tenants.Agregar(new Tenant { Id = 1, Nombre = "Lic. Norte", Especialidad = "psicologia", Estado = "activo" });

    private TenantAdminService Servicio() => new(
        _tenants, new PasswordServiceFake(), new SecurityContextServiceFake(), new TenantEstadoServiceFake(), _gateway, _uow);

    [Fact]
    public async Task Vincula_un_telefono_que_existe_en_el_gateway()
    {
        var dto = await Servicio().VincularWhatsappAsync(1, 7);

        Assert.Equal(7, _tenant.WhatsappTelefonoId);
        Assert.Equal(7, dto.WhatsappTelefonoId);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Telefono_que_el_gateway_no_conoce_da_404()
    {
        _gateway.Existe = false;

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().VincularWhatsappAsync(1, 7));

        Assert.Equal(404, ex.StatusCode);
        Assert.Null(_tenant.WhatsappTelefonoId);
    }

    [Fact]
    public async Task Gateway_sin_respuesta_da_503()
    {
        _gateway.Existe = null;
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().VincularWhatsappAsync(1, 7));
        Assert.Equal(503, ex.StatusCode);
    }

    [Fact]
    public async Task Telefono_ya_vinculado_a_otra_cuenta_da_409()
    {
        _uow.ProximoDuplicadoEnIndice = CteIndices.TenantWhatsappTelefono;
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().VincularWhatsappAsync(1, 7));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Desvincular_no_consulta_al_gateway()
    {
        _tenant.WhatsappTelefonoId = 7;
        _gateway.Existe = null; // si consultara, daría 503

        await Servicio().VincularWhatsappAsync(1, null);

        Assert.Null(_tenant.WhatsappTelefonoId);
    }

    [Fact]
    public async Task Tenant_inexistente_da_404()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().VincularWhatsappAsync(99, 7));
        Assert.Equal(404, ex.StatusCode);
    }
}
