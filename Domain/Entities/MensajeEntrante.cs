using Domain.Entities.Abstractions;

namespace Domain.Entities;

/// <summary>
/// Mensaje que un paciente mandó al número de la cuenta, tal como lo entregó el gateway, con lo que
/// se hizo con él. Sirve de historial y de deduplicación (el gateway reintenta el webhook).
/// </summary>
public class MensajeEntrante : ITenantEntity
{
    public long Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>Id del mensaje en el gateway. Único.</summary>
    public long GatewayMensajeId { get; set; }

    /// <summary>Número del remitente, normalizado.</summary>
    public string Numero { get; set; } = null!;

    public string Texto { get; set; } = "";

    public DateTime RecibidoEn { get; set; }

    public int? PacienteId { get; set; }

    /// <summary>Turno que se confirmó o canceló con este mensaje, si alguno.</summary>
    public int? TurnoId { get; set; }

    /// <summary>Un CteRespuestasWhatsapp.*</summary>
    public string Resultado { get; set; } = null!;

    public DateTime ProcesadoEn { get; set; }
}
