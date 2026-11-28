using Shared.DTOs.Whatsapp;

namespace Application.Interfaces;

public interface IPlantillaMensajeService
{
    Task<PlantillasMensajeDto> ListarAsync(CancellationToken cancellationToken = default);
    Task<PlantillaMensajeDto> GuardarAsync(string clave, PlantillaMensajeGuardarDto dto, CancellationToken cancellationToken = default);
    Task<PlantillaMensajeDto> RestablecerAsync(string clave, CancellationToken cancellationToken = default);
    Task<VistaPreviaMensajeResultadoDto> VistaPreviaAsync(VistaPreviaMensajeDto dto, CancellationToken cancellationToken = default);
}
