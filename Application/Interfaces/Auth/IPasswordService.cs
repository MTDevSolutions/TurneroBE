namespace Application.Interfaces.Auth;

public interface IPasswordService
{
    string Encriptar(string password);

    bool Verificar(string password, string? hash);

    /// <summary>
    /// Verifica contra un hash ficticio precalculado, para que el costo de cómputo
    /// sea equivalente al de un login real y no revelar por timing si el email existe.
    /// </summary>
    void VerificarDummy(string password);
}
