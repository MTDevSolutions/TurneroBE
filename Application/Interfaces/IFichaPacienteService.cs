using Shared.DTOs.Paciente;

namespace Application.Interfaces;

public interface IFichaPacienteService
{
    Task<FichaPacienteDto> ObtenerAsync(int pacienteId, CancellationToken cancellationToken = default);
}
