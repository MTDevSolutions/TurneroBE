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

    private void ConfigurarDominio(ModelBuilder modelBuilder)
    {
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
    }
}
