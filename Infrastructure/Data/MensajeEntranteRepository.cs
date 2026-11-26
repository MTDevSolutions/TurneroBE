using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class MensajeEntranteRepository(TurneroContext context) : IMensajeEntranteRepository
{
    // Sin query filter: el webhook no tiene cuenta en contexto, y el id del gateway es único global.
    public async Task<bool> ExisteAsync(long gatewayMensajeId, CancellationToken cancellationToken = default)
        => await context.MensajesEntrantes.IgnoreQueryFilters()
            .AnyAsync(m => m.GatewayMensajeId == gatewayMensajeId, cancellationToken);

    public void Agregar(MensajeEntrante mensaje) => context.MensajesEntrantes.Add(mensaje);

    public async Task<List<MensajeEntrante>> ListarPorTurnoAsync(int turnoId, CancellationToken cancellationToken = default)
        => await context.MensajesEntrantes.AsNoTracking()
            .Where(m => m.TurnoId == turnoId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);
}
