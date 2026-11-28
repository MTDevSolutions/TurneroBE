using Domain.Entities;

namespace Application.Interfaces;

public interface IPlantillaMensajeRepository
{
    /// <summary>Plantillas propias de la cuenta del request.</summary>
    Task<List<PlantillaMensaje>> ListarAsync(CancellationToken cancellationToken = default);

    Task<PlantillaMensaje?> ObtenerAsync(string clave, CancellationToken cancellationToken = default);

    /// <summary>
    /// Textos propios de una cuenta, por clave, con TenantId explícito: lo usan el webhook y los
    /// workers, que no tienen cuenta en el request.
    /// </summary>
    Task<Dictionary<string, string>> TextosAsync(int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Igual que <see cref="TextosAsync(int, CancellationToken)"/>, para varias cuentas a la vez.</summary>
    Task<Dictionary<int, Dictionary<string, string>>> TextosAsync(IReadOnlyCollection<int> tenantIds, CancellationToken cancellationToken = default);

    void Agregar(PlantillaMensaje plantilla);

    void Eliminar(PlantillaMensaje plantilla);
}
