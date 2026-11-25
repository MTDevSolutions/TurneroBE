using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Mensaje de WhatsApp que sale (al paciente o al profesional): es la cola de salida (outbox) y el
/// historial a la vez, igual que en CellFix. Se encola en la misma transacción que el cambio que lo
/// origina y el texto queda congelado, así el historial muestra lo que efectivamente se mandó.
/// </summary>
public class MensajeWhatsapp : ITenantEntity
{
    public long Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>Turno al que se refiere; null en mensajes sin turno.</summary>
    public int? TurnoId { get; set; }

    /// <summary>Un CteMensajesWhatsapp.Tipo*.</summary>
    public string Tipo { get; set; } = null!;

    /// <summary>Número normalizado (549...).</summary>
    public string Destino { get; set; } = "";

    public string Texto { get; set; } = null!;

    /// <summary>pendiente | enviado | fallido | descartado</summary>
    public string Estado { get; set; } = null!;

    public int Intentos { get; set; }
    public DateTime ProximoIntentoEn { get; set; }
    public string? UltimoError { get; set; }
    public string? GatewayMessageId { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? EnviadoEn { get; set; }

    public Turno? Turno { get; set; }
}
