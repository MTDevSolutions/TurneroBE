using Application.Interfaces;
using Domain.Entities;

namespace Tests.Fakes;

/// <summary>
/// Fake in-memory de IPlantillaMensajeRepository. Las consultas sin TenantId explícito ven solo la
/// cuenta del request (<paramref name="tenantActual"/>), como el global query filter; Agregar completa
/// el TenantId como SaveChanges.
/// </summary>
public class PlantillaMensajeRepositoryFake(int tenantActual = 1) : IPlantillaMensajeRepository
{
    public List<PlantillaMensaje> Filas { get; } = [];

    public PlantillaMensajeRepositoryFake Con(int tenantId, string clave, string texto)
    {
        Filas.Add(new PlantillaMensaje { Id = Filas.Count + 1, TenantId = tenantId, Clave = clave, Texto = texto });
        return this;
    }

    public Task<List<PlantillaMensaje>> ListarAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Filas.Where(p => p.TenantId == tenantActual).ToList());

    public Task<PlantillaMensaje?> ObtenerAsync(string clave, CancellationToken cancellationToken = default)
        => Task.FromResult(Filas.FirstOrDefault(p => p.TenantId == tenantActual && p.Clave == clave));

    public Task<Dictionary<string, string>> TextosAsync(int tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(Filas.Where(p => p.TenantId == tenantId).ToDictionary(p => p.Clave, p => p.Texto));

    public Task<Dictionary<int, Dictionary<string, string>>> TextosAsync(IReadOnlyCollection<int> tenantIds, CancellationToken cancellationToken = default)
        => Task.FromResult(Filas.Where(p => tenantIds.Contains(p.TenantId))
            .GroupBy(p => p.TenantId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(p => p.Clave, p => p.Texto)));

    public void Agregar(PlantillaMensaje plantilla)
    {
        if (plantilla.TenantId == 0)
            plantilla.TenantId = tenantActual;
        Filas.Add(plantilla);
    }

    public void Eliminar(PlantillaMensaje plantilla) => Filas.Remove(plantilla);
}
