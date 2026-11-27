using Application.Interfaces.Auth;

namespace Application.Helpers;

public class BcryptPasswordService : IPasswordService
{
    // Hash BCrypt fijo de una password ficticia, solo para igualar tiempos de cómputo.
    private const string HashDummy = "$2a$11$ZSYiBlH50Ww3ODG20trG0ed3anoP/G/3PSPPPHXwX8m4dFbHlc79u";

    public string Encriptar(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verificar(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public void VerificarDummy(string password)
        => BCrypt.Net.BCrypt.Verify(password, HashDummy);
}
