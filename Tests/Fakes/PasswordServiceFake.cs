using Application.Interfaces.Auth;

namespace Tests.Fakes;

/// <summary>
/// Fake de IPasswordService. El "hash" de una password es <c>hash:{password}</c>,
/// así que verificar es comparar strings. Registra si se llamó a VerificarDummy,
/// que es la defensa contra el oráculo de timing del login.
/// </summary>
public class PasswordServiceFake : IPasswordService
{
    /// <summary>True si alguien llamó a VerificarDummy (nivelación de timing).</summary>
    public bool VerificarDummyLlamado { get; private set; }

    public static string Hash(string password) => $"hash:{password}";

    public string Encriptar(string password) => Hash(password);

    public bool Verificar(string password, string? hash) => hash == Hash(password);

    public void VerificarDummy(string password) => VerificarDummyLlamado = true;
}
