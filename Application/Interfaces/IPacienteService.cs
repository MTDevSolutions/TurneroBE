using Shared.DTOs.Paciente;
using Shared.DTOs.Paginacion;

namespace Application.Interfaces;

public interface IPacienteService
{
    Task<PaginaDto<PacienteDto>> ListarAsync(PaginacionQuery query, string? buscar, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<PacienteDto> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<PacienteDto> CrearAsync(PacienteGuardarDto dto, CancellationToken cancellationToken = default);
    Task<PacienteDto> ActualizarAsync(int id, PacienteGuardarDto dto, CancellationToken cancellationToken = default);
    Task<PacienteDto> CambiarActivoAsync(int id, bool activo, CancellationToken cancellationToken = default);
}
