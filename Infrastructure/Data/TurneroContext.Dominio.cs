using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Constantes;

namespace Infrastructure.Data;

/// <summary>
/// Configuración de las entidades de dominio (con tenant_id). Se filtran por el
/// global query filter aplicado en el contexto principal (implementan ITenantEntity).
/// </summary>
public partial class TurneroContext
{
    public DbSet<Sucursal> Sucursales { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Configuracion> Configuraciones { get; set; } = null!;
    public DbSet<Paciente> Pacientes { get; set; } = null!;
    public DbSet<Turno> Turnos { get; set; } = null!;

    private void ConfigurarDominio(ModelBuilder modelBuilder)
    {
        // btree_gist: permite el exclusion constraint de turnos (igualdad de TenantId + rango).
        modelBuilder.HasPostgresExtension("btree_gist");
        // unaccent: buscar "gomez" encuentra "Gómez".
        modelBuilder.HasPostgresExtension("unaccent");

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Direccion).HasMaxLength(250);
            entity.Property(e => e.Activa).HasDefaultValue(true);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Sucursales_Tenants");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_Sucursales_TenantId");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Rol).IsRequired().HasMaxLength(30);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Celular).HasMaxLength(20);

            // Email único GLOBAL (identificador del login multi-tenant).
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName(CteIndices.UsuarioEmail);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Usuarios_Tenants");

            entity.HasOne(e => e.Sucursal)
                .WithMany()
                .HasForeignKey(e => e.SucursalId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Usuarios_Sucursales");
        });

        modelBuilder.Entity<Configuracion>(entity =>
        {
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Configuraciones_Tenants");

            // Una sola configuración por cuenta.
            entity.HasIndex(e => e.TenantId).IsUnique().HasDatabaseName("UX_Configuraciones_TenantId");
        });

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Celular).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Notas).HasMaxLength(500);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("now()");

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Pacientes_Tenants");

            // El celular identifica al paciente cuando responde por WhatsApp: único en la cuenta.
            entity.HasIndex(e => new { e.TenantId, e.Celular }).IsUnique()
                .HasDatabaseName(CteIndices.PacienteCelular);

            // Concurrencia optimista con la columna de sistema xmin de PostgreSQL.
            entity.Property<uint>("xmin").IsRowVersion();
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(20).HasDefaultValue(CteEstadosTurno.Pendiente);
            entity.Property(e => e.CanceladoPor).HasMaxLength(20);
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(250);
            entity.Property(e => e.Notas).HasMaxLength(500);
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("now()");

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Turnos_Tenants");

            entity.HasOne(e => e.Paciente)
                .WithMany()
                .HasForeignKey(e => e.PacienteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Turnos_Pacientes");

            entity.HasOne(e => e.Sucursal)
                .WithMany()
                .HasForeignKey(e => e.SucursalId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Turnos_Sucursales");

            // El calendario consulta por rango de fechas dentro de la cuenta.
            entity.HasIndex(e => new { e.TenantId, e.Inicio }).HasDatabaseName("IX_Turnos_TenantId_Inicio");

            // Fin > Inicio. El no-solapamiento es un EXCLUDE de PostgreSQL que EF no modela:
            // está en la migración AgendaPacientesTurnos (CteIndices.TurnoSinSolapamiento).
            entity.ToTable(t => t.HasCheckConstraint("CK_Turnos_FinPosteriorAInicio", "\"Fin\" > \"Inicio\""));

            entity.Property<uint>("xmin").IsRowVersion();
        });
    }
}
