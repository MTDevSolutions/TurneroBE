using Domain.Entities;

namespace Application.Interfaces;

public interface IUsuarioRepository
{
    /// <summary>
    /// Busca un usuario por email para el login. SALTEA el global query filter
    /// porque es una operación pre-auth (todavía no hay tenant en contexto).
    /// </summary>
    Task<Usuario?> GetByEmailParaLoginAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Existe, está activo y pertenece al tenant actual (filtro global aplicado).
    /// Usado para validar referencias cruzadas (ej. quién atiende un turno).
    /// </summary>
    Task<bool> ExisteActivoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nombre de cada usuario pedido (del tenant actual, filtro global aplicado), activo o no.
    /// Ids vacíos: diccionario vacío sin consultar.
    /// </summary>
    Task<Dictionary<int, string>> NombresAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
}
