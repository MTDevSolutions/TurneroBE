namespace Shared.Constantes;

/// <summary>Reglas de las notas de consulta.</summary>
public static class CteNotasConsulta
{
    /// <summary>Horas desde el alta durante las que la nota se puede editar. Después solo se anula
    /// o se agrega otra nota como aclaración.</summary>
    public const int HorasEdicion = 24;

    public const int MaxTexto = 10_000;
    public const int MaxMotivoAnulacion = 250;
}
