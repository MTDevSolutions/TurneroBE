using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Shared.Constantes;
using Shared.DTOs.Whatsapp;
using Tests.Fakes;

namespace Tests.Services;

public class PlantillaMensajeServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PlantillaMensajeRepositoryFake _repo = new(tenantActual: 1);
    private readonly TenantRepositoryFake _tenants = new();
    private readonly UnitOfWorkFake _uow = new();

    public PlantillaMensajeServiceTests()
        => _tenants.Agregar(new Tenant { Id = 1, Nombre = "Lic. Laura Pérez", Especialidad = "psicologia", Estado = "activo" });

    private PlantillaMensajeService Servicio(string rol = CteRoles.Admin)
        => new(_repo, new TenantContextFake(1, rol: rol), _tenants, _uow, new RelojFake(Ahora));

    private static PlantillaMensajeGuardarDto Dto(string texto) => new() { Texto = texto };

    [Fact]
    public async Task Listar_devuelve_todos_los_de_fabrica_y_pisa_solo_los_propios()
    {
        _repo.Con(1, CtePlantillasMensaje.Aviso, "Hola {paciente}!").Con(2, CtePlantillasMensaje.Cancelado, "De otra cuenta");

        var resultado = await Servicio().ListarAsync();

        Assert.Equal(CtePlantillasMensaje.Todas.Count, resultado.Plantillas.Count);
        var aviso = resultado.Plantillas.Single(p => p.Clave == CtePlantillasMensaje.Aviso);
        Assert.Equal("Hola {paciente}!", aviso.Texto);
        Assert.False(aviso.EsDefault);
        Assert.Equal(CtePlantillasMensaje.PiePedidoConfirmacion, aviso.Pie);
        var cancelado = resultado.Plantillas.Single(p => p.Clave == CtePlantillasMensaje.Cancelado);
        Assert.True(cancelado.EsDefault);
        Assert.Contains(resultado.Variables, v => v.Nombre == "paciente");
    }

    [Fact]
    public async Task Guardar_crea_la_fila_propia_con_el_texto_recortado()
    {
        var dto = await Servicio().GuardarAsync(CtePlantillasMensaje.Aviso, Dto("  Hola {paciente}, nos vemos el {fecha}.  "));

        Assert.Equal("Hola {paciente}, nos vemos el {fecha}.", dto.Texto);
        Assert.False(dto.EsDefault);
        var fila = Assert.Single(_repo.Filas);
        Assert.Equal(1, fila.TenantId);
        Assert.Equal(Ahora.UtcDateTime, fila.ActualizadoEn);
        Assert.Equal(1, _uow.Guardados);
    }

    [Fact]
    public async Task Guardar_de_nuevo_actualiza_la_misma_fila()
    {
        _repo.Con(1, CtePlantillasMensaje.Aviso, "Viejo");

        await Servicio().GuardarAsync(CtePlantillasMensaje.Aviso, Dto("Nuevo {paciente}"));

        Assert.Equal("Nuevo {paciente}", Assert.Single(_repo.Filas).Texto);
    }

    [Fact]
    public async Task Guardar_el_texto_de_fabrica_es_restablecer()
    {
        _repo.Con(1, CtePlantillasMensaje.Aviso, "Viejo");
        var fabrica = CtePlantillasMensaje.Buscar(CtePlantillasMensaje.Aviso)!.TextoPorDefecto;

        var dto = await Servicio().GuardarAsync(CtePlantillasMensaje.Aviso, Dto(fabrica));

        Assert.True(dto.EsDefault);
        Assert.Empty(_repo.Filas);
    }

    [Theory]
    [InlineData("Hola {nombre}")]
    [InlineData("   ")]
    public async Task Guardar_un_texto_invalido_da_400_y_no_guarda(string texto)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().GuardarAsync(CtePlantillasMensaje.Aviso, Dto(texto)));

        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(_repo.Filas);
        Assert.Equal(0, _uow.Guardados);
    }

    [Fact]
    public async Task Variables_desconocidas_se_nombran_en_el_error()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Servicio().GuardarAsync(CtePlantillasMensaje.Aviso, Dto("Hola {nombre}, {Fecha}")));

        Assert.Contains("{nombre}", ex.Message);
        Assert.Contains("{Fecha}", ex.Message);
    }

    [Fact]
    public async Task Clave_desconocida_da_404()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Servicio().GuardarAsync("inventado", Dto("Hola")));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Solo_el_titular_puede_cambiar_los_mensajes()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Servicio(CteRoles.Secretaria).GuardarAsync(CtePlantillasMensaje.Aviso, Dto("Hola")));

        Assert.Equal(403, ex.StatusCode);
        Assert.Empty(_repo.Filas);
    }

    [Fact]
    public async Task Restablecer_borra_la_fila_propia_y_devuelve_el_de_fabrica()
    {
        _repo.Con(1, CtePlantillasMensaje.Cancelado, "Propio");

        var dto = await Servicio().RestablecerAsync(CtePlantillasMensaje.Cancelado);

        Assert.True(dto.EsDefault);
        Assert.Equal(CtePlantillasMensaje.Buscar(CtePlantillasMensaje.Cancelado)!.TextoPorDefecto, dto.Texto);
        Assert.Empty(_repo.Filas);
    }

    [Fact]
    public async Task Restablecer_no_toca_la_fila_de_otra_cuenta()
    {
        _repo.Con(2, CtePlantillasMensaje.Cancelado, "De otra cuenta");

        await Servicio().RestablecerAsync(CtePlantillasMensaje.Cancelado);

        Assert.Single(_repo.Filas);
        Assert.Equal(0, _uow.Guardados);
    }

    [Fact]
    public async Task Vista_previa_usa_el_nombre_de_la_cuenta_y_agrega_el_pie()
    {
        var resultado = await Servicio().VistaPreviaAsync(new VistaPreviaMensajeDto
        {
            Clave = CtePlantillasMensaje.Aviso,
            Texto = "Hola {paciente}, te espera {profesional} el {fecha} a las {hora}.",
        });

        Assert.Equal($"Hola Ana, te espera Lic. Laura Pérez el jueves 3/12 a las 15:00.\n\n{CtePlantillasMensaje.PiePedidoConfirmacion}",
            resultado.Texto);
        Assert.Empty(_repo.Filas);
    }
}
