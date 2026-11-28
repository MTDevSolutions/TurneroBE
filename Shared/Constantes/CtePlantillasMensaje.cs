namespace Shared.Constantes;

/// <summary>
/// Mensajes de WhatsApp que cada cuenta puede reescribir, con su texto de fábrica. Igual que en
/// CellFix: los de fábrica viven en código (se mejoran sin migrar ni sembrar filas) y en la base solo
/// está lo que la cuenta cambió. La clave coincide con el tipo del mensaje, salvo el recordatorio,
/// que tiene dos textos según el turno ya esté confirmado o no.
/// </summary>
public static class CtePlantillasMensaje
{
    public const string Aviso = CteMensajesWhatsapp.TipoAviso;
    public const string Reprogramado = CteMensajesWhatsapp.TipoReprogramado;
    public const string Cancelado = CteMensajesWhatsapp.TipoCancelado;
    public const string Recordatorio = CteMensajesWhatsapp.TipoRecordatorio;
    public const string RecordatorioConfirmado = "recordatorio_confirmado";
    public const string ConfirmacionRecibida = CteMensajesWhatsapp.TipoConfirmacionRecibida;
    public const string CancelacionRecibida = CteMensajesWhatsapp.TipoCancelacionRecibida;
    public const string AvisoProfesional = CteMensajesWhatsapp.TipoAvisoProfesional;

    public const string DestinatarioPaciente = "paciente";
    public const string DestinatarioProfesional = "profesional";

    /// <summary>Largo máximo del texto editable (el pie fijo va aparte).</summary>
    public const int MaxLargoTexto = 800;

    /// <summary>
    /// Pies fijos: dicen qué responder y de eso depende el Sí/No, así que no se editan. Si la cuenta
    /// los borrara, el paciente no sabría qué contestar.
    /// </summary>
    public const string PiePedidoConfirmacion = "Respondé *SÍ* para confirmar o *NO* si no podés venir.";
    public const string PiePedidoCancelacion = "Si al final no podés venir, respondé *NO* y liberamos el horario.";

    public sealed record Definicion(
        string Clave, string Nombre, string Descripcion, string Destinatario, string TextoPorDefecto, string? Pie);

    public static readonly IReadOnlyList<Definicion> Todas =
    [
        new(Aviso, "Aviso de turno nuevo", "Le llega al paciente cuando agendás un turno.", DestinatarioPaciente,
            "¡Hola {paciente}! Te agendamos un turno con {profesional} el {fecha} a las {hora} en {lugar}.",
            PiePedidoConfirmacion),
        new(Reprogramado, "Cambio de horario", "Le llega al paciente cuando movés su turno a otro horario.", DestinatarioPaciente,
            "¡Hola {paciente}! Tu turno con {profesional} cambió de horario: ahora es el {fecha} a las {hora} en {lugar}.",
            PiePedidoConfirmacion),
        new(Recordatorio, "Recordatorio (sin confirmar)", "Sale antes del turno si el paciente todavía no confirmó.", DestinatarioPaciente,
            "¡Hola {paciente}! Te recordamos tu turno con {profesional} el {fecha} a las {hora} en {lugar}.",
            PiePedidoConfirmacion),
        new(RecordatorioConfirmado, "Recordatorio (ya confirmado)", "Sale antes del turno si el paciente ya confirmó.", DestinatarioPaciente,
            "¡Hola {paciente}! Te recordamos tu turno con {profesional} el {fecha} a las {hora} en {lugar}. ¡Te esperamos!",
            PiePedidoCancelacion),
        new(ConfirmacionRecibida, "Gracias por confirmar", "Respuesta automática cuando el paciente contesta Sí.", DestinatarioPaciente,
            "¡Gracias, {paciente}! Tu turno del {fecha} a las {hora} quedó confirmado. 💗",
            null),
        new(CancelacionRecibida, "Turno cancelado por el paciente", "Respuesta automática cuando el paciente contesta No.", DestinatarioPaciente,
            "Listo, {paciente}: cancelamos tu turno del {fecha} a las {hora}. Cuando quieras otro, comunicate con el consultorio.",
            null),
        new(Cancelado, "Cancelación de tu parte", "Le llega al paciente cuando cancelás su turno.", DestinatarioPaciente,
            "Hola {paciente}, te avisamos que se canceló tu turno con {profesional} del {fecha} a las {hora}. Para coordinar otro, comunicate con el consultorio.",
            null),
        new(AvisoProfesional, "Aviso a tu celular", "Te llega a vos cuando un paciente cancela por WhatsApp.", DestinatarioProfesional,
            "{paciente} {apellido} canceló por WhatsApp su turno del {fecha} a las {hora}. El horario quedó libre en tu agenda.",
            null),
    ];

    private static readonly Dictionary<string, Definicion> PorClave = Todas.ToDictionary(d => d.Clave);

    public static Definicion? Buscar(string? clave)
        => clave is not null && PorClave.TryGetValue(clave, out var d) ? d : null;
}
