using Domain.Entities;

namespace Application.Interfaces;

public interface IMensajeWhatsappRepository
{
    /// <summary>Encola un mensaje. Fuera de un request de cuenta (workers, webhook) el TenantId lo
    /// pone quien llama.</summary>
    void Agregar(MensajeWhatsapp mensaje);

    /// <summary>Mensajes salientes de un turno de la cuenta del request, el más viejo primero.</summary>
    Task<List<MensajeWhatsapp>> ListarPorTurnoAsync(int turnoId, CancellationToken cancellationToken = default);

    /// <summary>Marca como descartados los pendientes del turno que hablan de su horario (aviso,
    /// cambio de horario, recordatorio): un aviso nuevo los reemplaza. Busca en todas las cuentas
    /// (el id del turno es único).</summary>
    Task DescartarPendientesDelTurnoAsync(int turnoId, string motivo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserva hasta <paramref name="maximo"/> pendientes vencidos de TODAS las cuentas, corriendo su
    /// próximo intento a ahora + <paramref name="lease"/> para que otra pasada no los tome, y los
    /// devuelve trackeados. Si el proceso muere antes de guardar, el lease vence y vuelven a la cola.
    /// </summary>
    Task<List<MensajeWhatsapp>> ReclamarPendientesAsync(DateTime ahora, int maximo, TimeSpan lease, CancellationToken cancellationToken = default);
}
