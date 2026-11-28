using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class PlantillaMensajeRepository(TurneroContext context) : IPlantillaMensajeRepository
{
    public async Task<List<PlantillaMensaje>> ListarAsync(CancellationToken cancellationToken = default)
        => await context.PlantillasMensaje.ToListAsync(cancellationToken);

    public async Task<PlantillaMensaje?> ObtenerAsync(string clave, CancellationToken cancellationToken = default)
        => await context.PlantillasMensaje.FirstOrDefaultAsync(p => p.Clave == clave, cancellationToken);

    public async Task<Dictionary<string, string>> TextosAsync(int tenantId, CancellationToken cancellationToken = default)
        => await context.PlantillasMensaje.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .ToDictionaryAsync(p => p.Clave, p => p.Texto, cancellationToken);

    public async Task<Dictionary<int, Dictionary<string, string>>> TextosAsync(IReadOnlyCollection<int> tenantIds, CancellationToken cancellationToken = default)
    {
        var filas = await context.PlantillasMensaje.IgnoreQueryFilters().AsNoTracking()
            .Where(p => tenantIds.Contains(p.TenantId))
            .Select(p => new { p.TenantId, p.Clave, p.Texto })
            .ToListAsync(cancellationToken);
        return filas.GroupBy(f => f.TenantId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(f => f.Clave, f => f.Texto));
    }

    public void Agregar(PlantillaMensaje plantilla) => context.PlantillasMensaje.Add(plantilla); // TenantId lo completa SaveChanges

    public void Eliminar(PlantillaMensaje plantilla) => context.PlantillasMensaje.Remove(plantilla);
}
