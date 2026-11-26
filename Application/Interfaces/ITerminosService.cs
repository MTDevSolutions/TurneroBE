using Shared.DTOs.Terminos;

namespace Application.Interfaces;

/// <summary>Términos de uso de la cuenta del tenant autenticado.</summary>
public interface ITerminosService
{
    Task<TerminosDto> ObtenerAsync(CancellationToken cancellationToken = default);
    Task<TerminosDto> AceptarAsync(TerminosAceptarDto dto, CancellationToken cancellationToken = default);
}
