using Domain.Entities;

namespace Application.Interfaces;

public interface ISucursalRepository
{
    /// <summary>Lista las sucursales del tenant actual (el global query filter lo limita solo).</summary>
    Task<List<Sucursal>> ListarAsync(CancellationToken cancellationToken = default);
}
