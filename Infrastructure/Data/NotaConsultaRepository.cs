using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

// El global query filter limita todo al tenant del request.
public class NotaConsultaRepository(TurneroContext context) : INotaConsultaRepository
{
    public async Task<List<NotaConsulta>> ListarPorPacienteAsync(int pacienteId, bool incluirAnuladas, CancellationToken cancellationToken = default)
        => await context.NotasConsulta.AsNoTracking()
            .Include(n => n.Turno)
            .Where(n => n.PacienteId == pacienteId && (incluirAnuladas || !n.Anulada))
            .OrderByDescending(n => n.CreadaEn)
            .ThenByDescending(n => n.Id)
            .ToListAsync(cancellationToken);

    public async Task<NotaConsulta?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await context.NotasConsulta
            .Include(n => n.Turno)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<List<NotaConsultaVersion>> ListarVersionesAsync(int notaId, CancellationToken cancellationToken = default)
        => await context.NotasConsultaVersiones.AsNoTracking()
            .Where(v => v.NotaConsultaId == notaId)
            .OrderByDescending(v => v.Version)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<int, int>> ContarPorTurnoAsync(IReadOnlyCollection<int> turnoIds, CancellationToken cancellationToken = default)
    {
        if (turnoIds.Count == 0)
            return [];

        return await context.NotasConsulta.AsNoTracking()
            .Where(n => n.TurnoId != null && turnoIds.Contains(n.TurnoId.Value) && !n.Anulada)
            .GroupBy(n => n.TurnoId!.Value)
            .Select(g => new { TurnoId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.TurnoId, x => x.Cantidad, cancellationToken);
    }

    public void Agregar(NotaConsulta nota)
        => context.NotasConsulta.Add(nota); // TenantId lo completa SaveChanges

    public void AgregarVersion(NotaConsultaVersion version)
        => context.NotasConsultaVersiones.Add(version);
}
