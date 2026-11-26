using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Parámetros de la agenda por tenant (fila única por tenant). Entidad de dominio:
/// se filtra por tenant. El índice único por TenantId fuerza el singleton.
/// </summary>
public class Configuracion : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>Duración por defecto de un turno nuevo, en minutos.</summary>
    public int DuracionTurnoMinutos { get; set; } = 45;

    /// <summary>Horas antes del turno en que sale el recordatorio. 0 = sin recordatorio.</summary>
    public int HorasRecordatorio { get; set; } = 24;

    /// <summary>Llave general de los avisos automáticos por WhatsApp.</summary>
    public bool AvisosWhatsappActivos { get; set; } = true;
}
