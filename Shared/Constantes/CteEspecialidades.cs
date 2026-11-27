namespace Shared.Constantes;

/// <summary>Especialidades del público inicial. "otra" cubre el resto sin bloquear el alta.</summary>
public static class CteEspecialidades
{
    public const string Fonoaudiologia = "fonoaudiologia";
    public const string Psicologia = "psicologia";
    public const string MedicinaGeneral = "medicina_general";
    public const string Otra = "otra";

    public static readonly string[] Todas = [Fonoaudiologia, Psicologia, MedicinaGeneral, Otra];
}
