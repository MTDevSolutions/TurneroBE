namespace Shared.DTOs.Whatsapp;

/// <summary>Un mensaje de WhatsApp de un turno, para el historial de la ficha.</summary>
public class MensajeWhatsappDto
{
    public long Id { get; set; }

    /// <summary>saliente | entrante</summary>
    public string Direccion { get; set; } = null!;

    /// <summary>Saliente: CteMensajesWhatsapp.Tipo*. Entrante: CteRespuestasWhatsapp.*</summary>
    public string Tipo { get; set; } = null!;

    public string Texto { get; set; } = null!;

    /// <summary>Saliente: pendiente | enviado | fallido | descartado. Entrante: recibido.</summary>
    public string Estado { get; set; } = null!;

    public string? Error { get; set; }

    public DateTimeOffset Fecha { get; set; }
}
