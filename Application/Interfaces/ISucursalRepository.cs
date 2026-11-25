using Domain.Entities;

namespace Application.Interfaces;

public interface ISucursalRepository
{
    /// <summary>Lista las sucursales del tenant actual (el global query filter lo limita solo).</summary>
    Task<List<Sucursal>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Consultorio activo de la cuenta; null si no existe, está inactivo o es de otra cuenta.</summary>
    Task<Sucursal?> GetActivaAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Consultorios activos de la cuenta.</summary>
    Task<List<Sucursal>> ListarActivasAsync(CancellationToken cancellationToken = default);
}
