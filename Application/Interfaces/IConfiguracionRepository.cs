using Domain.Entities;

namespace Application.Interfaces;

public interface IConfiguracionRepository
{
    /// <summary>Devuelve la configuración del tenant actual (o null si aún no existe).</summary>
    Task<Configuracion?> ObtenerAsync(CancellationToken cancellationToken = default);

    void Agregar(Configuracion configuracion);
}
