using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class UsuarioRepository(TurneroContext context) : IUsuarioRepository
{
    public async Task<Usuario?> GetByEmailParaLoginAsync(string email, CancellationToken cancellationToken = default)
        => await context.Usuarios
            .IgnoreQueryFilters() // login pre-auth: sin tenant en contexto
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken); // email ya viene en minúsculas

    public async Task<bool> ExisteActivoAsync(int id, CancellationToken cancellationToken = default)
        => await context.Usuarios.AnyAsync(u => u.Id == id && u.Activo, cancellationToken);

    public async Task<Dictionary<int, string>> NombresAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        return await context.Usuarios
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nombre, cancellationToken);
    }
}
