using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Constantes;

namespace Infrastructure.Data;

/// <summary>
/// Notas de consulta y sus versiones anteriores. Todas las claves foráneas son RESTRICT y la
/// migración NotasConsulta agrega triggers que impiden borrar notas y modificar o borrar versiones:
/// lo escrito sobre un paciente no se pierde ni se pisa.
/// </summary>
public partial class TurneroContext
{
    public DbSet<NotaConsulta> NotasConsulta { get; set; } = null!;
    public DbSet<NotaConsultaVersion> NotasConsultaVersiones { get; set; } = null!;

    private static void ConfigurarNotas(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotaConsulta>(entity =>
        {
            entity.ToTable("NotasConsulta");
            entity.Property(e => e.Texto).IsRequired().HasMaxLength(CteNotasConsulta.MaxTexto);
            entity.Property(e => e.MotivoAnulacion).HasMaxLength(CteNotasConsulta.MaxMotivoAnulacion);

            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_NotasConsulta_Tenants");
            entity.HasOne<Paciente>().WithMany().HasForeignKey(e => e.PacienteId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_NotasConsulta_Pacientes");
            entity.HasOne(e => e.Turno).WithMany().HasForeignKey(e => e.TurnoId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_NotasConsulta_Turnos");

            // La ficha lista las notas del paciente de la más nueva a la más vieja; la agenda cuenta por turno.
            entity.HasIndex(e => new { e.TenantId, e.PacienteId, e.CreadaEn })
                .IsDescending(false, false, true)
                .HasDatabaseName("IX_NotasConsulta_TenantId_PacienteId_CreadaEn");
            entity.HasIndex(e => e.TurnoId).HasDatabaseName("IX_NotasConsulta_TurnoId");

            entity.Property<uint>("xmin").IsRowVersion();
        });

        modelBuilder.Entity<NotaConsultaVersion>(entity =>
        {
            entity.ToTable("NotasConsultaVersiones");
            entity.Property(e => e.Texto).IsRequired().HasMaxLength(CteNotasConsulta.MaxTexto);

            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_NotasConsultaVersiones_Tenants");
            entity.HasOne<NotaConsulta>().WithMany().HasForeignKey(e => e.NotaConsultaId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_NotasConsultaVersiones_NotasConsulta");

            entity.HasIndex(e => new { e.NotaConsultaId, e.Version }).IsUnique()
                .HasDatabaseName("UX_NotasConsultaVersiones_NotaConsultaId_Version");
        });
    }
}
