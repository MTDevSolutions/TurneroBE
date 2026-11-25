using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constantes;
using Tests.Fakes;

namespace Tests.Data;

/// <summary>
/// El aislamiento multi-tenant vive en el modelo de EF: toda entidad de dominio lleva el query
/// filter por TenantId y las de plataforma no. Si alguien agrega una entidad y se olvida de
/// ITenantEntity, estos tests no lo detectan solos: sumala a la lista de dominio.
/// </summary>
public class AislamientoTenantTests
{
    private static TurneroContext NuevoContexto(int? tenantId = 7)
        => new(new DbContextOptionsBuilder<TurneroContext>()
                .UseNpgsql("Host=localhost;Database=turnero_fake;Username=x;Password=x")
                .Options,
            new TenantContextFake(tenantId));

    [Theory]
    [InlineData(typeof(Sucursal))]
    [InlineData(typeof(Usuario))]
    [InlineData(typeof(Configuracion))]
    public void Las_entidades_de_dominio_tienen_query_filter_de_tenant(Type tipo)
    {
        using var context = NuevoContexto();
        Assert.NotNull(context.Model.FindEntityType(tipo)!.GetQueryFilter());
    }

    [Theory]
    [InlineData(typeof(Tenant))]
    [InlineData(typeof(PagoSuscripcion))]
    [InlineData(typeof(Superadmin))]
    public void Las_entidades_de_plataforma_no_se_filtran(Type tipo)
    {
        using var context = NuevoContexto();
        Assert.Null(context.Model.FindEntityType(tipo)!.GetQueryFilter());
    }

    [Fact]
    public void La_consulta_de_dominio_filtra_por_el_tenant_del_request()
    {
        using var context = NuevoContexto(tenantId: 7);

        var sql = context.Configuraciones.ToQueryString();

        Assert.Contains("\"TenantId\"", sql);
    }

    [Fact]
    public void Un_telefono_del_gateway_se_vincula_a_una_sola_cuenta()
    {
        using var context = NuevoContexto();
        var indice = context.Model.FindEntityType(typeof(Tenant))!.GetIndexes()
            .Single(i => i.GetDatabaseName() == CteIndices.TenantWhatsappTelefono);
        Assert.True(indice.IsUnique);
        Assert.Equal("\"WhatsappTelefonoId\" IS NOT NULL", indice.GetFilter());
    }

    [Fact]
    public void Una_sola_configuracion_por_cuenta()
    {
        using var context = NuevoContexto();
        var indice = context.Model.FindEntityType(typeof(Configuracion))!.GetIndexes()
            .Single(i => i.GetDatabaseName() == "UX_Configuraciones_TenantId");
        Assert.True(indice.IsUnique);
    }
}
