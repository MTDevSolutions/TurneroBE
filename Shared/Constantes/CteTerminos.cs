namespace Shared.Constantes;

/// <summary>
/// Términos de uso vigentes. Al cambiar el texto se sube la versión: toda cuenta que aceptó una
/// versión anterior queda bloqueada (403 TERMINOS_PENDIENTES) hasta aceptar la nueva.
/// <para>BORRADOR: falta revisión legal antes del piloto.</para>
/// </summary>
public static class CteTerminos
{
    public const string VersionVigente = "2026-11-25";

    /// <summary>Código que el front usa para mostrar la pantalla de aceptación.</summary>
    public const string CodigoPendientes = "TERMINOS_PENDIENTES";

    public const string Texto = """
        Términos de uso (borrador)

        1. Canal de WhatsApp no oficial. Los avisos y recordatorios salen desde un número de WhatsApp
           propio del profesional, vinculado mediante un servicio que no es la API oficial de Meta.
           WhatsApp puede limitar o bloquear ese número en cualquier momento, sin aviso previo.
           El servicio no garantiza la entrega de los mensajes ni responde por el bloqueo del número.

        2. El número. El profesional usa un número nuevo, distinto de su número personal, y es su
           titular. No debe usarlo para mensajes masivos ni para contactar a personas que no sean
           sus pacientes.

        3. Consentimiento de los pacientes. El profesional declara que sus pacientes aceptaron recibir
           avisos y recordatorios de turnos por WhatsApp en el número que le dieron.

        4. Datos. El servicio guarda nombre, teléfono y turnos de los pacientes, y las respuestas a los
           avisos. No guarda historia clínica ni datos de salud, y el profesional no debe cargarlos en
           campos libres.

        5. Suspensión. La falta de pago suspende el acceso sin borrar datos; al regularizar, todo vuelve.
        """;
}
