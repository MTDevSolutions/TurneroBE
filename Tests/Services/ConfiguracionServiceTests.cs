using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Shared.DTOs.Configuracion;
using Tests.Fakes;

namespace Tests.Services;

public class ConfiguracionServiceTests
{
    [Fact]
    public async Task Sin_configuracion_la_crea_con_los_valores_por_defecto()
    {
        var repo = new ConfiguracionRepositoryFake();
        var uow = new UnitOfWorkFake();

        var dto = await new ConfiguracionService(repo, uow).ObtenerAsync();

        Assert.Equal(45, dto.DuracionTurnoMinutos);
        Assert.Equal(24, dto.HorasRecordatorio);
        Assert.True(dto.AvisosWhatsappActivos);
        Assert.Equal("rosa", dto.ColorPrincipal);
        Assert.NotNull(repo.Actual);
        Assert.Equal(1, uow.Guardados);
    }

    [Fact]
    public async Task Actualizar_pisa_los_tres_valores_y_guarda_una_vez()
    {
        var repo = new ConfiguracionRepositoryFake(new Configuracion { TenantId = 7 });
        var uow = new UnitOfWorkFake();

        var dto = await new ConfiguracionService(repo, uow).ActualizarAsync(new ConfiguracionActualizarDto
        {
            DuracionTurnoMinutos = 30, HorasRecordatorio = 0, AvisosWhatsappActivos = false
        });

        Assert.Equal(30, dto.DuracionTurnoMinutos);
        Assert.Equal(0, dto.HorasRecordatorio);
        Assert.False(dto.AvisosWhatsappActivos);
        Assert.Equal(1, uow.Guardados);
    }

    [Theory]
    [InlineData("lavanda", "lavanda")]
    [InlineData(" Salvia ", "salvia")]
    public async Task Actualizar_color_guarda_la_paleta_normalizada(string enviado, string guardado)
    {
        var repo = new ConfiguracionRepositoryFake(new Configuracion { TenantId = 7 });
        var uow = new UnitOfWorkFake();

        var dto = await new ConfiguracionService(repo, uow).ActualizarColorAsync(enviado);

        Assert.Equal(guardado, dto.ColorPrincipal);
        Assert.Equal(guardado, repo.Actual!.ColorPrincipal);
        Assert.Equal(1, uow.Guardados);
    }

    [Theory]
    [InlineData("violeta")]
    [InlineData("#ff0000")]
    [InlineData("")]
    public async Task Actualizar_color_fuera_de_las_opciones_es_400_y_no_guarda(string color)
    {
        var repo = new ConfiguracionRepositoryFake(new Configuracion { TenantId = 7 });
        var uow = new UnitOfWorkFake();

        var ex = await Assert.ThrowsAsync<AppException>(() => new ConfiguracionService(repo, uow).ActualizarColorAsync(color));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("rosa", repo.Actual!.ColorPrincipal);
        Assert.Equal(0, uow.Guardados);
    }

    [Fact]
    public async Task Color_del_login_es_el_de_la_cuenta_o_rosa_si_no_tiene_configuracion()
    {
        var uow = new UnitOfWorkFake();
        var conColor = new ConfiguracionRepositoryFake(new Configuracion { TenantId = 7, ColorPrincipal = "celeste" });

        Assert.Equal("celeste", await new ConfiguracionService(conColor, uow).ObtenerColorAsync(7));
        Assert.Equal("rosa", await new ConfiguracionService(new ConfiguracionRepositoryFake(), uow).ObtenerColorAsync(7));
    }
}
