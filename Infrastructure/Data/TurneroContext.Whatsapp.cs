using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Constantes;

namespace Infrastructure.Data;

/// <summary>Mensajes de WhatsApp: cola de salida (outbox) y respuestas de los pacientes.</summary>
public partial class TurneroContext
{
    public DbSet<MensajeWhatsapp> MensajesWhatsapp { get; set; } = null!;
    public DbSet<MensajeEntrante> MensajesEntrantes { get; set; } = null!;

    private static void ConfigurarWhatsapp(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MensajeWhatsapp>(entity =>
        {
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Destino).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Texto).IsRequired().HasColumnType("text");
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);
            entity.Property(e => e.UltimoError).HasMaxLength(500);
            entity.Property(e => e.GatewayMessageId).HasMaxLength(100);
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()");

            entity.HasOne(e => e.Turno).WithMany().HasForeignKey(e => e.TurnoId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_MensajesWhatsapp_Turnos");
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_MensajesWhatsapp_Tenants");

            // El worker busca por estado y vencimiento; el historial, por turno.
            entity.HasIndex(e => new { e.Estado, e.ProximoIntentoEn })
                .HasDatabaseName("IX_MensajesWhatsapp_Estado_ProximoIntentoEn");
            entity.HasIndex(e => new { e.TenantId, e.TurnoId })
                .HasDatabaseName("IX_MensajesWhatsapp_TenantId_TurnoId");
        });

        modelBuilder.Entity<MensajeEntrante>(entity =>
        {
            entity.Property(e => e.Numero).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Texto).IsRequired().HasColumnType("text");
            entity.Property(e => e.Resultado).IsRequired().HasMaxLength(30);

            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_MensajesEntrantes_Tenants");
            entity.HasOne<Paciente>().WithMany().HasForeignKey(e => e.PacienteId)
                .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_MensajesEntrantes_Pacientes");
            entity.HasOne<Turno>().WithMany().HasForeignKey(e => e.TurnoId)
                .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_MensajesEntrantes_Turnos");

            entity.HasIndex(e => e.GatewayMensajeId).IsUnique()
                .HasDatabaseName(CteIndices.MensajeEntranteGateway);
            entity.HasIndex(e => new { e.TenantId, e.TurnoId })
                .HasDatabaseName("IX_MensajesEntrantes_TenantId_TurnoId");
        });
    }
}
