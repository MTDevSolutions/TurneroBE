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

    public async Task<Sucursal?> GetActivaAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Sucursales.FirstOrDefaultAsync(s => s.Id == id && s.Activa, cancellationToken);

    public async Task<List<Sucursal>> ListarActivasAsync(CancellationToken cancellationToken = default)
        => await _context.Sucursales.Where(s => s.Activa).OrderBy(s => s.Nombre).ToListAsync(cancellationToken);
}
