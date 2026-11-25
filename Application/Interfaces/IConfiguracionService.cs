using Shared.DTOs.Configuracion;

namespace Application.Interfaces;

public interface IConfiguracionService
{
    Task<ConfiguracionDto> ObtenerAsync(CancellationToken cancellationToken = default);
    Task<ConfiguracionDto> ActualizarAsync(ConfiguracionActualizarDto dto, CancellationToken cancellationToken = default);
}
