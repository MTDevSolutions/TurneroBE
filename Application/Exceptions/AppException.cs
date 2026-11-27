namespace Application.Exceptions;

/// <summary>
/// Excepción de dominio/aplicación que mapea a un código HTTP concreto.
/// Se crea siempre vía factories estáticas para mantener mensajes consistentes.
/// El <see cref="ExceptionHandlingMiddleware"/> / AxExceptionFilter la traducen a HTTP.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    private AppException(string message, int statusCode) : base(message)
        => StatusCode = statusCode;

    // 400 — Validación
    public static AppException Validacion(string mensaje)
        => new(mensaje, 400);

    // 401 — Credenciales inválidas
    public static AppException CredencialesInvalidas()
        => new("Las credenciales son incorrectas.", 401);

    // 403 — Acceso denegado
    public static AppException UsuarioInactivo()
        => new("Tu usuario está desactivado. Contactate con el titular de la cuenta.", 403);

    public static AppException TenantSuspendido()
        => new("Tu cuenta está suspendida.", 403);

    public static AppException SinPermiso(string mensaje)
        => new(mensaje, 403);

    // 404 — No encontrado
    public static AppException RegistroNoExiste()
        => new("No se encontró el registro especificado.", 404);

    // 409 — Conflicto
    public static AppException Conflicto(string mensaje)
        => new(mensaje, 409);

    // 503 — Servicio externo no disponible
    public static AppException ServicioNoDisponible(string mensaje)
        => new(mensaje, 503);
}
