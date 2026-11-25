using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Plataforma;
using Tests.Fakes;

namespace Tests.Services;

/// <summary>
/// Alta y ciclo de vida de una cuenta. Lo que se cuida: que el celular personal (destino de los
/// avisos de cancelación) quede bien normalizado, que el email siga siendo único global y que una
/// suspensión corte las sesiones abiertas.
/// </summary>
public class TenantAdminServiceTests
{
    private readonly TenantRepositoryFake _repo = new();
    private readonly TenantEstadoServiceFake _estado = new();
    private readonly UnitOfWorkFake _uow = new();

    private TenantAdminService Servicio() => new(
        _repo, new PasswordServiceFake(), new SecurityContextServiceFake(id: 99), _estado, _uow);

    private static TenantCrearDto Alta(string especialidad = "psicologia", string celular = "11 2345-6789") => new()
    {
        Nombre = "Lic. Ana Pérez",
        Especialidad = especialidad,
        ConsultorioNombre = "Consultorio Palermo",
        ConsultorioDireccion = "  Av. Santa Fe 1234  ",
        AdminNombre = "Ana Pérez",
        AdminEmail = " ana@consultorio.com ",
        AdminPassword = "Secreta123!",
        AdminCelular = celular
    };

    [Fact]
    public async Task Alta_crea_cuenta_activa_con_consultorio_admin_y_configuracion()
    {
        var dto = await Servicio().CrearAsync(Alta());

        var alta = _repo.UltimoAlta!.Value;
        Assert.Equal(CteEstadosTenant.Activo, dto.Estado);
        Assert.Equal(CteEspecialidades.Psicologia, dto.Especialidad);
        Assert.Null(dto.TerminosVersionAceptada); // los acepta el profesional, no el superadmin
        Assert.Equal("Av. Santa Fe 1234", alta.Sucursal.Direccion);
        Assert.Equal(CteRoles.Admin, alta.Admin.Rol);
        Assert.Equal("ana@consultorio.com", alta.Admin.Email);
        Assert.Equal(PasswordServiceFake.Hash("Secreta123!"), alta.Admin.PasswordHash);
        Assert.NotNull(alta.Configuracion);
    }

    [Fact]
    public async Task Alta_normaliza_el_celular_al_formato_de_WhatsApp()
    {
        await Servicio().CrearAsync(Alta(celular: "+54 9 11 2345 6789"));

        Assert.Equal("5491123456789", _repo.UltimoAlta!.Value.Admin.Celular);
    }

    [Theory]
    [InlineData("011 15 2345 6789")] // con 0 y 15: no se adivina
    [InlineData("4123456")]          // sin código de área
    public async Task Alta_con_celular_ambiguo_da_400_y_no_crea_nada(string celular)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Alta(celular: celular)));

        Assert.Equal(400, ex.StatusCode);
        Assert.Null(_repo.UltimoAlta);
    }

    [Fact]
    public async Task Alta_con_especialidad_desconocida_da_400()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Alta(especialidad: "odontologia")));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Especialidad_se_acepta_sin_importar_mayusculas()
    {
        var dto = await Servicio().CrearAsync(Alta(especialidad: " Medicina_General "));

        Assert.Equal(CteEspecialidades.MedicinaGeneral, dto.Especialidad);
    }

    [Fact]
    public async Task Alta_con_email_en_uso_da_409()
    {
        _repo.EmailsEnUso.Add("ana@consultorio.com");

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().CrearAsync(Alta()));

        Assert.Equal(409, ex.StatusCode);
        Assert.Null(_repo.UltimoAlta);
    }

    [Fact]
    public async Task Suspender_guarda_e_invalida_el_cache_para_cortar_sesiones_abiertas()
    {
        _repo.Agregar(new Tenant { Id = 5, Nombre = "X", Especialidad = "otra", Estado = CteEstadosTenant.Activo });

        await Servicio().SuspenderAsync(5);

        Assert.Equal(CteEstadosTenant.Suspendido, (await _repo.GetByIdAsync(5))!.Estado);
        Assert.Equal(1, _uow.Guardados);
        Assert.Contains(5, _estado.Invalidados);
    }

    [Fact]
    public async Task Reactivar_vuelve_a_activo()
    {
        _repo.Agregar(new Tenant { Id = 5, Nombre = "X", Especialidad = "otra", Estado = CteEstadosTenant.Suspendido });

        await Servicio().ReactivarAsync(5);

        Assert.Equal(CteEstadosTenant.Activo, (await _repo.GetByIdAsync(5))!.Estado);
        Assert.Contains(5, _estado.Invalidados);
    }

    [Fact]
    public async Task Suspender_una_cuenta_inexistente_da_404()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().SuspenderAsync(42));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Registrar_pago_lo_asigna_a_la_cuenta_y_al_superadmin()
    {
        _repo.Agregar(new Tenant { Id = 5, Nombre = "X", Especialidad = "otra" });

        var pago = await Servicio().RegistrarPagoAsync(5, new RegistrarPagoDto
        {
            Monto = 15000m, PeriodoDesde = new DateTime(2026, 12, 1), PeriodoHasta = new DateTime(2026, 12, 31)
        });

        Assert.Equal(5, pago.TenantId);
        Assert.Equal(99, pago.RegistradoPor);
        Assert.Single(_repo.Pagos);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Registrar_pago_con_periodo_invertido_da_400()
    {
        _repo.Agregar(new Tenant { Id = 5, Nombre = "X", Especialidad = "otra" });

        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().RegistrarPagoAsync(5, new RegistrarPagoDto
        {
            Monto = 1m, PeriodoDesde = new DateTime(2026, 12, 31), PeriodoHasta = new DateTime(2026, 12, 1)
        }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(_repo.Pagos);
    }
}
