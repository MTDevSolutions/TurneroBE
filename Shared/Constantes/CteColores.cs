namespace Shared.Constantes;

/// <summary>
/// Paletas del color principal de la app (se elige en Configuración). El front tiene la escala
/// de cada una; acá solo se valida el nombre. Agregar una = sumarla acá y en el front.
/// </summary>
public static class CteColores
{
    public const string Rosa = "rosa";
    public const string Lavanda = "lavanda";
    public const string Coral = "coral";
    public const string Salvia = "salvia";
    public const string Celeste = "celeste";

    public const string PorDefecto = Rosa;

    public static readonly string[] Todos = [Rosa, Lavanda, Coral, Salvia, Celeste];
}
