using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class SucursalRepository(TurneroContext context) : ISucursalRepository
{
    private readonly TurneroContext _context = context;

    // El global query filter limita automáticamente al tenant del request.
    public async Task<List<Sucursal>> ListarAsync(CancellationToken cancellationToken = default)
        => await _context.Sucursales
            .OrderBy(s => s.Nombre)
            .ToListAsync(cancellationToken);
}
