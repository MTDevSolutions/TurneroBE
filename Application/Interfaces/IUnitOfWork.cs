namespace Application.Interfaces;

/// <summary>
/// Punto de commit de la operación. Los repositorios no persisten: acumulan cambios en el
/// scope y el servicio decide cuándo se guardan, en un único SaveChanges.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persiste los cambios pendientes del scope.
    /// Conflicto de concurrencia (xmin) => AppException.Conflicto.
    /// </summary>
    Task GuardarAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Igual que GuardarAsync, pero ante un conflicto de concurrencia desengancha todas las
    /// entradas pendientes (Added, Modified, Deleted) del tracker y devuelve false en vez de
    /// lanzar. Para procesos batch que iteran muchas entidades y toleran perder una (job de abandono).
    /// </summary>
    Task<bool> IntentarGuardarAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Desengancha todas las entradas pendientes (Added, Modified, Deleted) del tracker. Para jobs
    /// batch que, tras fallar con una entidad, deben seguir con la próxima sin arrastrar sus cambios.
    /// </summary>
    void DescartarPendientes();

    /// <summary>
    /// Igual que GuardarAsync, pero si el commit falla por violar el índice único
    /// <paramref name="indiceUnico"/> (nombres en <c>CteIndices</c>) lanza
    /// AppException.Conflicto con <paramref name="mensaje"/>. Cualquier otra falla se propaga
    /// tal cual, para no reportar un duplicado que no existe.
    /// La traducción vive del lado de Infrastructure porque distinguir la violación de unicidad
    /// del índice concreto depende del tipo de excepción del proveedor, que Application no referencia.
    /// </summary>
    Task GuardarTraduciendoDuplicadoAsync(string indiceUnico, string mensaje, CancellationToken cancellationToken = default);
}
