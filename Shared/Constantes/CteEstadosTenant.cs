namespace Shared.Constantes;

/// <summary>Estados de un tenant. Texto (no enum de PG) para poder evolucionarlos.</summary>
public static class CteEstadosTenant
{
    public const string Activo = "activo";
    public const string Suspendido = "suspendido";
    public const string Cancelado = "cancelado";
}
