using Application.Interfaces.Auth;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Constantes;

namespace Infrastructure.Data.Seed;

/// <summary>
/// Siembra datos iniciales. Crea el superadmin si todavía no existe ninguno;
/// las credenciales se leen de la config (sección "Superadmin") con defaults de dev.
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeedAsync(
        TurneroContext context,
        IPasswordService passwordService,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (await context.Superadmins.AnyAsync(cancellationToken))
            return;

        var nombre = configuration["Superadmin:Nombre"] ?? "Superadmin";
        var email = configuration["Superadmin:Email"] ?? "admin@turnero.local";
        var password = configuration["Superadmin:Password"] ?? "Admin1234!";

        context.Superadmins.Add(new Superadmin
        {
            Nombre = nombre,
            Email = email,
            PasswordHash = passwordService.Encriptar(password)
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seed SOLO para Development: dos cuentas demo, cada una con un consultorio, su configuración
    /// y un admin, para probar el aislamiento multi-tenant end-to-end. La de psicología ya aceptó
    /// los términos; la de fonoaudiología no, para probar el bloqueo.
    /// Credenciales: psico@demo.local / fono@demo.local — password "Demo1234!".
    /// </summary>
    public static async Task EnsureDevSeedAsync(
        TurneroContext context,
        IPasswordService passwordService,
        CancellationToken cancellationToken = default)
    {
        // Tenant es plataforma (sin query filter). Si ya hay tenants, no re-sembrar.
        if (await context.Tenants.AnyAsync(cancellationToken))
            return;

        var psico = new Tenant
        {
            Nombre = "Lic. Demo Psicología", Especialidad = CteEspecialidades.Psicologia,
            Estado = CteEstadosTenant.Activo, Plan = "demo", FechaAlta = DateTime.UtcNow,
            TerminosVersionAceptada = CteTerminos.VersionVigente, TerminosAceptadosEn = DateTime.UtcNow
        };
        var fono = new Tenant
        {
            Nombre = "Lic. Demo Fonoaudiología", Especialidad = CteEspecialidades.Fonoaudiologia,
            Estado = CteEstadosTenant.Activo, Plan = "demo", FechaAlta = DateTime.UtcNow
        };
        context.Tenants.AddRange(psico, fono);
        await context.SaveChangesAsync(cancellationToken);

        var consPsico = new Sucursal { TenantId = psico.Id, Nombre = "Consultorio Palermo", Direccion = "Av. Santa Fe 1234, CABA", Activa = true };
        var consFono = new Sucursal { TenantId = fono.Id, Nombre = "Consultorio Centro", Direccion = "San Martín 456, Córdoba", Activa = true };
        context.Sucursales.AddRange(consPsico, consFono);
        context.Configuraciones.AddRange(
            new Configuracion { TenantId = psico.Id },
            new Configuracion { TenantId = fono.Id });
        await context.SaveChangesAsync(cancellationToken);

        context.Usuarios.AddRange(
            new Usuario
            {
                TenantId = psico.Id, SucursalId = consPsico.Id, Nombre = "Demo Psicología",
                Email = "psico@demo.local", Rol = CteRoles.Admin, Celular = "5491100000001",
                PasswordHash = passwordService.Encriptar("Demo1234!"), Activo = true
            },
            new Usuario
            {
                TenantId = fono.Id, SucursalId = consFono.Id, Nombre = "Demo Fonoaudiología",
                Email = "fono@demo.local", Rol = CteRoles.Admin, Celular = "5493510000002",
                PasswordHash = passwordService.Encriptar("Demo1234!"), Activo = true
            });
        await context.SaveChangesAsync(cancellationToken);
    }
}
