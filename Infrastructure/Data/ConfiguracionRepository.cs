using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ConfiguracionRepository(TurneroContext context) : IConfiguracionRepository
{
    // El global query filter limita al tenant del request; hay una sola fila por tenant.
    public async Task<Configuracion?> ObtenerAsync(CancellationToken cancellationToken = default)
        => await context.Configuraciones.FirstOrDefaultAsync(cancellationToken);

    public void Agregar(Configuracion configuracion)
        => context.Configuraciones.Add(configuracion); // TenantId lo completa SaveChanges
}
