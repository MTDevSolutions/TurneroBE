using Shared.DTOs.NotaConsulta;

namespace Application.Interfaces;

public interface INotaConsultaService
{
    Task<List<NotaConsultaDto>> ListarAsync(int pacienteId, bool incluirAnuladas, CancellationToken cancellationToken = default);
    Task<NotaConsultaDto> CrearAsync(int pacienteId, NotaConsultaCrearDto dto, CancellationToken cancellationToken = default);
    Task<NotaConsultaDto> EditarAsync(int id, NotaConsultaEditarDto dto, CancellationToken cancellationToken = default);
    Task<NotaConsultaDto> AnularAsync(int id, NotaConsultaAnularDto dto, CancellationToken cancellationToken = default);
    Task<List<NotaConsultaVersionDto>> VersionesAsync(int id, CancellationToken cancellationToken = default);
}
