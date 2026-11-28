using Shared.DTOs.Configuracion;

namespace Application.Interfaces;

public interface IConfiguracionService
{
    Task<ConfiguracionDto> ObtenerAsync(CancellationToken cancellationToken = default);
    Task<ConfiguracionDto> ActualizarAsync(ConfiguracionActualizarDto dto, CancellationToken cancellationToken = default);

    /// <summary>Cambia solo el color principal. 400 si no es una de las paletas de CteColores.</summary>
    Task<ConfiguracionDto> ActualizarColorAsync(string color, CancellationToken cancellationToken = default);

    /// <summary>Color principal de un tenant para el login (el default si no tiene configuración).</summary>
    Task<string> ObtenerColorAsync(int tenantId, CancellationToken cancellationToken = default);
}
