using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Constantes;

namespace Infrastructure.Data;

public class MensajeWhatsappRepository(TurneroContext context) : IMensajeWhatsappRepository
{
    public void Agregar(MensajeWhatsapp mensaje) => context.MensajesWhatsapp.Add(mensaje);

    public async Task<List<MensajeWhatsapp>> ListarPorTurnoAsync(int turnoId, CancellationToken cancellationToken = default)
        => await context.MensajesWhatsapp.AsNoTracking()
            .Where(m => m.TurnoId == turnoId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

    public async Task DescartarPendientesDelTurnoAsync(int turnoId, string motivo, CancellationToken cancellationToken = default)
    {
        var tipos = CteMensajesWhatsapp.TiposSobreHorario.ToList();
        var pendientes = await context.MensajesWhatsapp.IgnoreQueryFilters()
            .Where(m => m.TurnoId == turnoId && m.Estado == CteMensajesWhatsapp.Pendiente && tipos.Contains(m.Tipo))
            .ToListAsync(cancellationToken);

        foreach (var m in pendientes)
        {
            m.Estado = CteMensajesWhatsapp.Descartado;
            m.UltimoError = motivo;
        }
    }

    // Reserva con lease en un solo UPDATE (igual que CellFix): SKIP LOCKED evita que dos pasadas tomen
    // el mismo mensaje, y no queda una transacción abierta mientras se habla HTTP con el gateway.
    // El SQL crudo no pasa por el query filter: a propósito, el worker atiende a todas las cuentas.
    public async Task<List<MensajeWhatsapp>> ReclamarPendientesAsync(DateTime ahora, int maximo, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        var hasta = ahora + lease;
        var pendiente = CteMensajesWhatsapp.Pendiente;

        var ids = await context.Database
            .SqlQuery<long>($@"
                UPDATE ""MensajesWhatsapp"" SET ""ProximoIntentoEn"" = {hasta}
                WHERE ""Id"" IN (
                    SELECT ""Id"" FROM ""MensajesWhatsapp""
                    WHERE ""Estado"" = {pendiente} AND ""ProximoIntentoEn"" <= {ahora}
                    ORDER BY ""Id""
                    LIMIT {maximo}
                    FOR UPDATE SKIP LOCKED)
                RETURNING ""Id"" AS ""Value""")
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
            return [];

        // Con el turno y su paciente: el worker revisa que el mensaje siga siendo cierto antes de mandarlo.
        return await context.MensajesWhatsapp.IgnoreQueryFilters()
            .Include(m => m.Turno).ThenInclude(t => t!.Paciente)
            .Where(m => ids.Contains(m.Id))
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }
}
