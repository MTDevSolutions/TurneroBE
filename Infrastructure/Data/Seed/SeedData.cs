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

        await SembrarAgendaDemoAsync(context, psico.Id, consPsico.Id, cancellationToken);
    }

    /// <summary>
    /// Pacientes y una semana de turnos de ejemplo para la cuenta de psicología, alrededor de la
    /// fecha en que se siembra: pasados (atendido / no vino) y futuros (sin confirmar / confirmado /
    /// cancelado). Los celulares son de mentira (11 5555-01xx).
    /// </summary>
    private static async Task SembrarAgendaDemoAsync(TurneroContext context, int tenantId, int sucursalId, CancellationToken cancellationToken)
    {
        var ahora = DateTime.UtcNow;
        string[][] datos =
        [
            ["Lucía", "Gómez"], ["Martina", "Fernández"], ["Sofía", "Rodríguez"], ["Valentina", "López"],
            ["Camila", "Martínez"], ["Julieta", "Pérez"], ["Tomás", "García"], ["Agustina", "Romero"],
        ];
        var pacientes = datos.Select((d, i) => new Paciente
        {
            TenantId = tenantId, Nombre = d[0], Apellido = d[1], Celular = $"54911555501{i:00}",
            Activo = true, FechaAlta = ahora,
        }).ToList();
        context.Pacientes.AddRange(pacientes);
        await context.SaveChangesAsync(cancellationToken);

        // Argentina es UTC-3 todo el año: "hoy" y las horas se arman en hora local.
        var hoyAr = ahora.AddHours(-3).Date;
        (int Dia, int Hora, int Paciente, string Estado)[] agenda =
        [
            (-2, 10, 0, CteEstadosTurno.Atendido), (-2, 11, 1, CteEstadosTurno.Atendido), (-2, 15, 2, CteEstadosTurno.Ausente),
            (-1, 9, 3, CteEstadosTurno.Atendido), (-1, 16, 4, CteEstadosTurno.Atendido),
            (1, 10, 5, CteEstadosTurno.Confirmado), (1, 11, 6, CteEstadosTurno.Pendiente), (1, 17, 7, CteEstadosTurno.Cancelado),
            (2, 9, 0, CteEstadosTurno.Pendiente), (2, 14, 1, CteEstadosTurno.Confirmado),
            (3, 10, 2, CteEstadosTurno.Pendiente), (3, 18, 3, CteEstadosTurno.Confirmado),
            (4, 12, 4, CteEstadosTurno.Pendiente),
        ];
        foreach (var t in agenda)
        {
            var inicio = DateTime.SpecifyKind(hoyAr.AddDays(t.Dia).AddHours(t.Hora + 3), DateTimeKind.Utc);
            context.Turnos.Add(new Turno
            {
                TenantId = tenantId, PacienteId = pacientes[t.Paciente].Id, SucursalId = sucursalId,
                Inicio = inicio, Fin = inicio.AddMinutes(45), Estado = t.Estado, FechaAlta = ahora,
                CanceladoPor = t.Estado == CteEstadosTurno.Cancelado ? CteEstadosTurno.CanceladoPorPaciente : null,
                // Sin recordatorios automáticos para los datos de ejemplo.
                RecordatorioEncoladoEn = ahora,
            });
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
