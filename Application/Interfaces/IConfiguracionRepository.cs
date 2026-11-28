using Domain.Entities;

namespace Application.Interfaces;

public interface IConfiguracionRepository
{
    /// <summary>Devuelve la configuración del tenant actual (o null si aún no existe).</summary>
    Task<Configuracion?> ObtenerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Color principal de un tenant dado, sin depender del tenant del request (lo usa el login,
    /// que todavía no tiene tenant). Null si la cuenta no tiene configuración.
    /// </summary>
    Task<string?> ObtenerColorAsync(int tenantId, CancellationToken cancellationToken = default);

    void Agregar(Configuracion configuracion);
}
