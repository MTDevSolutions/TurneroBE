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
}
