using Domain.Entities;
using Shared.Constantes;

namespace Application.Helpers;

/// <summary>
/// Arma los mensajes de WhatsApp de un turno con el texto de la cuenta (si lo reescribió) o el de
/// fábrica (CtePlantillasMensaje). Los de fábrica son cortos, cálidos y en voseo, siempre dicen qué
/// responder y no incluyen datos de salud: solo nombre, fecha, hora y lugar.
/// </summary>
public static class TextosWhatsapp
{
    /// <param name="propios">Textos reescritos por la cuenta, por clave. Vacío = todos de fábrica.</param>
    public static string Armar(string clave, Turno turno, string profesional, IReadOnlyDictionary<string, string> propios)
    {
        var definicion = CtePlantillasMensaje.Buscar(clave)
            ?? throw new ArgumentOutOfRangeException(nameof(clave), clave, "Mensaje de WhatsApp desconocido.");
        return PlantillaMensajeRenderer.Armar(definicion, propios.GetValueOrDefault(clave), VariablesMensaje.De(turno, profesional));
    }

    /// <summary>El recordatorio a un turno ya confirmado no vuelve a pedir confirmación.</summary>
    public static string ClaveRecordatorio(Turno turno)
        => turno.Estado == CteEstadosTurno.Confirmado
            ? CtePlantillasMensaje.RecordatorioConfirmado
            : CtePlantillasMensaje.Recordatorio;
}
