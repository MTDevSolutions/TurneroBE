using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Auth;
using Domain.Entities;
using Shared.Constantes;

namespace Application.UseCases.Auth;

/// <summary>
/// Valida las credenciales de un usuario de una cuenta. El email es único global,
/// así que se busca sin contexto de tenant (IgnoreQueryFilters en el repo) y de él
/// se deriva el tenant. Rechaza si el usuario está inactivo o el tenant no está activo.
/// </summary>
public class AutenticacionUsuario(
    IUsuarioRepository usuarioRepository,
    IPasswordService passwordService,
    ITenantEstadoService tenantEstadoService)
{
    public async Task<Usuario> ExecuteAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarioRepository.GetByEmailParaLoginAsync((email ?? string.Empty).Trim().ToLowerInvariant(), cancellationToken);
        if (usuario == null)
        {
            passwordService.VerificarDummy(password);
            throw AppException.CredencialesInvalidas();
        }

        // La password se verifica ANTES que cualquier otra condición del usuario: si el
        // estado de la cuenta se chequeara primero, un 403 sin password válida delataría
        // qué emails existen en el sistema (y salteando BCrypt también lo delataría el timing).
        if (!passwordService.Verificar(password, usuario.PasswordHash))
            throw AppException.CredencialesInvalidas();

        if (!usuario.Activo)
            throw AppException.UsuarioInactivo();

        var estado = await tenantEstadoService.ObtenerEstadoAsync(usuario.TenantId, cancellationToken);
        if (!string.Equals(estado, CteEstadosTenant.Activo, StringComparison.OrdinalIgnoreCase))
            throw AppException.TenantSuspendido();

        return usuario;
    }
}
