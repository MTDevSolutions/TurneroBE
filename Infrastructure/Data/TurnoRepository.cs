using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Constantes;

namespace Infrastructure.Data;

public class TurnoRepository(TurneroContext context) : ITurnoRepository
{
    public async Task<List<Turno>> ListarRangoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => await context.Turnos.AsNoTracking()
            .Include(t => t.Paciente)
            .Include(t => t.Sucursal)
            .Where(t => t.Inicio < hastaUtc && t.Fin > desdeUtc)
            .OrderBy(t => t.Inicio)
            .ToListAsync(cancellationToken);

    public async Task<Turno?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Sucursal)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    // Rangos semiabiertos [inicio, fin): un turno que termina 15:00 no choca con uno que empieza 15:00.
    public async Task<bool> HaySolapamientoAsync(DateTime inicioUtc, DateTime finUtc, int? excluirId, CancellationToken cancellationToken = default)
        => await context.Turnos.AnyAsync(t =>
            t.Estado != CteEstadosTurno.Cancelado
            && t.Id != excluirId
            && t.Inicio < finUtc
            && t.Fin > inicioUtc, cancellationToken);

    public void Agregar(Turno turno)
        => context.Turnos.Add(turno); // TenantId lo completa SaveChanges
}
