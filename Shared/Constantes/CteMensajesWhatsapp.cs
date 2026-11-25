namespace Shared.Constantes;

/// <summary>Estados, tipos y límites de los mensajes de WhatsApp (outbox), igual que CellFix.</summary>
public static class CteMensajesWhatsapp
{
    public const string Pendiente = "pendiente";
    public const string Enviado = "enviado";
    public const string Fallido = "fallido";
    public const string Descartado = "descartado";

    // Tipos de mensaje: a quién va y por qué.
    /// <summary>Al paciente, al agendar: pide confirmar con Sí o No.</summary>
    public const string TipoAviso = "aviso";
    /// <summary>Al paciente, al mover el turno de horario: pide confirmar de nuevo.</summary>
    public const string TipoReprogramado = "reprogramado";
    /// <summary>Al paciente, cuando el profesional cancela.</summary>
    public const string TipoCancelado = "cancelado";
    /// <summary>Al paciente, X horas antes del turno.</summary>
    public const string TipoRecordatorio = "recordatorio";
    /// <summary>Al paciente, respuesta a su "Sí".</summary>
    public const string TipoConfirmacionRecibida = "confirmacion_recibida";
    /// <summary>Al paciente, respuesta a su "No".</summary>
    public const string TipoCancelacionRecibida = "cancelacion_recibida";
    /// <summary>Al celular personal del profesional: un paciente canceló por WhatsApp.</summary>
    public const string TipoAvisoProfesional = "aviso_profesional";

    public const int MaxLargoTexto = 1000;

    /// <summary>Intentos antes de dar el mensaje por fallido.</summary>
    public const int MaxIntentos = 6;

    /// <summary>Mensajes que reclama una pasada del worker.</summary>
    public const int MaxPorPasada = 50;

    /// <summary>Un aviso que no salió en este plazo ya no sirve.</summary>
    public static readonly TimeSpan Vigencia = TimeSpan.FromHours(12);

    /// <summary>
    /// Cuánto queda reservado un mensaje reclamado: si el proceso muere, vuelve a la cola. Cubre un
    /// lote entero con el gateway colgado (MaxPorPasada × timeout de 15 s ≈ 12,5 min).
    /// </summary>
    public static readonly TimeSpan Lease = TimeSpan.FromMinutes(15);

    /// <summary>Espera antes del próximo intento, según los intentos ya hechos.</summary>
    public static TimeSpan Backoff(int intentos) => intentos switch
    {
        <= 1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        3 => TimeSpan.FromMinutes(15),
        4 => TimeSpan.FromHours(1),
        _ => TimeSpan.FromHours(3),
    };
}
