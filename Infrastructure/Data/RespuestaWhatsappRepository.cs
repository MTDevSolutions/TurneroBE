using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Constantes;

namespace Infrastructure.Data;

/// <summary>
/// Lecturas del webhook de respuestas. Ignoran el query filter (no hay cuenta en contexto) y por eso
/// TODAS filtran por el tenantId recibido: es la única garantía de aislamiento de este camino.
/// </summary>
public class RespuestaWhatsappRepository(TurneroContext context) : IRespuestaWhatsappRepository
{
    public async Task<Paciente?> PacientePorCelularAsync(int tenantId, string celular, CancellationToken cancellationToken = default)
        => await context.Pacientes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Celular == celular && p.Activo, cancellationToken);

    public async Task<Turno?> ProximoTurnoVivoAsync(int tenantId, int pacienteId, DateTime ahoraUtc, CancellationToken cancellationToken = default)
        => await context.Turnos.IgnoreQueryFilters()
            .Include(t => t.Paciente)
            .Include(t => t.Sucursal)
            .Where(t => t.TenantId == tenantId
                && t.PacienteId == pacienteId
                && (t.Estado == CteEstadosTurno.Pendiente || t.Estado == CteEstadosTurno.Confirmado)
                && t.Inicio > ahoraUtc)
            .OrderBy(t => t.Inicio)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<string>> CelularesAdminsAsync(int tenantId, CancellationToken cancellationToken = default)
        => await context.Usuarios.IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.Activo && u.Rol == CteRoles.Admin && u.Celular != null)
            .Select(u => u.Celular!)
            .Distinct()
            .ToListAsync(cancellationToken);
}
