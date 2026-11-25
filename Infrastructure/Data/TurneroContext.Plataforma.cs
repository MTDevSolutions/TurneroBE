using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Constantes;

namespace Infrastructure.Data;

/// <summary>
/// Configuración de las entidades de plataforma (sin tenant_id).
/// Implementa el OnModelCreatingPartial que engancha el contexto principal.
/// </summary>
public partial class TurneroContext
{
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<PagoSuscripcion> PagosSuscripcion { get; set; } = null!;
    public DbSet<Superadmin> Superadmins { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Especialidad).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(20).HasDefaultValue(CteEstadosTenant.Activo);
            entity.Property(e => e.Plan).HasMaxLength(50);
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("now()");
            entity.Property(e => e.TerminosVersionAceptada).HasMaxLength(20);

            // Un teléfono del gateway es de una sola cuenta: si dos lo compartieran, una mandaría
            // avisos desde el número de la otra.
            entity.HasIndex(e => e.WhatsappTelefonoId).IsUnique()
                .HasFilter("\"WhatsappTelefonoId\" IS NOT NULL")
                .HasDatabaseName(CteIndices.TenantWhatsappTelefono);
        });

        modelBuilder.Entity<PagoSuscripcion>(entity =>
        {
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Tenant)
                .WithMany(p => p.Pagos)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PagosSuscripcion_Tenants");
        });

        modelBuilder.Entity<Superadmin>(entity =>
        {
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_Superadmins_Email");
        });

        // Entidades de dominio (con tenant_id) — ver TurneroContext.Dominio.cs
        ConfigurarDominio(modelBuilder);
    }
}
