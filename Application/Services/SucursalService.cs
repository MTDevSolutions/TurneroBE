using Application.Interfaces;
using Shared.DTOs.Sucursal;

namespace Application.Services;

public class SucursalService(ISucursalRepository repository) : ISucursalService
{
    private readonly ISucursalRepository _repository = repository;

    public async Task<List<SucursalDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var sucursales = await _repository.ListarAsync(cancellationToken);
        return sucursales.Select(s => new SucursalDto
        {
            Id = s.Id,
            Nombre = s.Nombre,
            Direccion = s.Direccion,
            Activa = s.Activa
        }).ToList();
    }
}
