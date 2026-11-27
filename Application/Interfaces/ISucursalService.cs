using Shared.DTOs.Sucursal;

namespace Application.Interfaces;

public interface ISucursalService
{
    Task<List<SucursalDto>> ListarAsync(CancellationToken cancellationToken = default);
}
