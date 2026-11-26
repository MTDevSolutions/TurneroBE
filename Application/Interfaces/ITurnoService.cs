using Shared.DTOs.Turno;

namespace Application.Interfaces;

public interface ITurnoService
{
    Task<List<TurnoDto>> ListarAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken cancellationToken = default);
    Task<TurnoDto> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<TurnoDto> CrearAsync(TurnoGuardarDto dto, CancellationToken cancellationToken = default);
    Task<TurnoDto> ReprogramarAsync(int id, TurnoGuardarDto dto, CancellationToken cancellationToken = default);
    Task<TurnoDto> CambiarEstadoAsync(int id, TurnoEstadoDto dto, CancellationToken cancellationToken = default);
}
