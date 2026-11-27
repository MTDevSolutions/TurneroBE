using Application.Exceptions;
using Application.Interfaces.Auth;
using Domain.Entities;

namespace Application.UseCases.Auth;

/// <summary>
/// Valida las credenciales de un superadmin (plano plataforma).
/// </summary>
public class AutenticacionSuperadmin(ISuperadminRepository repository, IPasswordService passwordService)
{
    public async Task<Superadmin> ExecuteAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        // Mismo error para "no existe" y "password incorrecta": no filtra usuarios.
        var superadmin = await repository.GetByEmailAsync((email ?? string.Empty).Trim().ToLowerInvariant(), cancellationToken);
        if (superadmin == null)
        {
            passwordService.VerificarDummy(password);
            throw AppException.CredencialesInvalidas();
        }

        if (!passwordService.Verificar(password, superadmin.PasswordHash))
            throw AppException.CredencialesInvalidas();

        return superadmin;
    }
}
