using Application.Interfaces;
using Domain.Entities;

namespace Tests.Fakes;

/// <summary>
/// Fake in-memory de IUsuarioRepository. Guarda un solo usuario: alcanza para
/// los casos de login (el email es único global, así que la búsqueda devuelve 0 o 1).
/// </summary>
public class UsuarioRepositoryFake : IUsuarioRepository
{
    private readonly Usuario? _usuario;

    public UsuarioRepositoryFake(Usuario? usuario = null) => _usuario = usuario;

    public Task<Usuario?> GetByEmailParaLoginAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(
            string.Equals(_usuario?.Email, email, StringComparison.OrdinalIgnoreCase) ? _usuario : null);

    /// <summary>Nombres por id para NombresAsync (aparte del usuario único del constructor).</summary>
    public Dictionary<int, string> Nombres { get; } = [];

    public Task<Dictionary<int, string>> NombresAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
        => Task.FromResult(Nombres.Where(kv => ids.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

    public Task<bool> ExisteActivoAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_usuario is { Activo: true } && _usuario.Id == id);
}
