namespace Application.UseCases.Security_Context;

/// <summary>
/// Acceso a la información del usuario autenticado en el contexto actual,
/// sin acoplar los casos de uso a la infraestructura (JWT/HttpContext).
/// </summary>
public interface ISecurityContextService
{
    /// <summary>Id del usuario autenticado (0 si no hay).</summary>
    int Id { get; }

    /// <summary>Nombre de usuario autenticado.</summary>
    string? UserName { get; }
}
