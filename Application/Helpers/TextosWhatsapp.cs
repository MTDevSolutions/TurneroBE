using Domain.Entities;

namespace Application.Helpers;

/// <summary>
/// Textos de los mensajes de WhatsApp. Cortos, cálidos y en voseo; siempre dicen qué responder.
/// No incluyen datos de salud: solo nombre, fecha, hora y lugar.
/// </summary>
public static class TextosWhatsapp
{
    private const string PedidoRespuesta = "Respondé *SÍ* para confirmar o *NO* si no podés venir.";

    public static string Aviso(Turno turno, string profesional)
        => $"¡Hola {turno.Paciente.Nombre}! Te agendamos un turno con {profesional} el "
           + $"{HoraArgentina.FechaYHora(turno.Inicio)}{Lugar(turno)}.\n\n{PedidoRespuesta}";

    public static string Reprogramado(Turno turno, string profesional)
        => $"¡Hola {turno.Paciente.Nombre}! Tu turno con {profesional} cambió de horario: ahora es el "
           + $"{HoraArgentina.FechaYHora(turno.Inicio)}{Lugar(turno)}.\n\n{PedidoRespuesta}";

    public static string Cancelado(Turno turno, string profesional)
        => $"Hola {turno.Paciente.Nombre}, te avisamos que se canceló tu turno con {profesional} del "
           + $"{HoraArgentina.FechaYHora(turno.Inicio)}. Para coordinar otro, comunicate con el consultorio.";

    public static string Recordatorio(Turno turno, string profesional)
        => turno.Estado == Shared.Constantes.CteEstadosTurno.Confirmado
            ? $"¡Hola {turno.Paciente.Nombre}! Te recordamos tu turno con {profesional} el "
              + $"{HoraArgentina.FechaYHora(turno.Inicio)}{Lugar(turno)}. ¡Te esperamos!\n\n"
              + "Si al final no podés venir, respondé *NO* y liberamos el horario."
            : $"¡Hola {turno.Paciente.Nombre}! Te recordamos tu turno con {profesional} el "
              + $"{HoraArgentina.FechaYHora(turno.Inicio)}{Lugar(turno)}.\n\n{PedidoRespuesta}";

    public static string ConfirmacionRecibida(Turno turno)
        => $"¡Gracias, {turno.Paciente.Nombre}! Tu turno del {HoraArgentina.FechaYHora(turno.Inicio)} quedó confirmado. 💗";

    public static string CancelacionRecibida(Turno turno)
        => $"Listo, {turno.Paciente.Nombre}: cancelamos tu turno del {HoraArgentina.FechaYHora(turno.Inicio)}. "
           + "Cuando quieras otro, comunicate con el consultorio.";

    public static string AvisoProfesional(Turno turno)
        => $"{turno.Paciente.Nombre} {turno.Paciente.Apellido} canceló por WhatsApp su turno del "
           + $"{HoraArgentina.FechaYHora(turno.Inicio)}. El horario quedó libre en tu agenda.";

    private static string Lugar(Turno turno)
    {
        var sucursal = turno.Sucursal;
        if (sucursal is null)
            return "";
        return string.IsNullOrWhiteSpace(sucursal.Direccion)
            ? $" en {sucursal.Nombre}"
            : $" en {sucursal.Nombre} ({sucursal.Direccion})";
    }
}
