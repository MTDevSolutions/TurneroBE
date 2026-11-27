using Application.UseCases.Security_Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data;

/// <summary>
/// Factory de diseño para que las herramientas de EF Core (migraciones) puedan
/// instanciar el contexto sin el contenedor DI, usando un <see cref="ITenantContext"/> no-op.
/// Se ejecuta con el directorio actual = proyecto de arranque (Web).
/// </summary>
public class TurneroContextFactory : IDesignTimeDbContextFactory<TurneroContext>
{
    public TurneroContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("TurneroEntities")
            ?? "Host=localhost;Port=5436;Database=turnero;Username=turnero;Password=turnero_dev_pw";

        var options = new DbContextOptionsBuilder<TurneroContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TurneroContext(options, new DesignTimeTenantContext());
    }

    /// <summary>ITenantContext no-op para diseño/migraciones: sin tenant ni superadmin.</summary>
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public int? TenantId => null;
        public string? Rol => null;
        public bool EsSuperadmin => false;
    }
}
