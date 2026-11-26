using Application.UseCases.Security_Context;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Security;

/// <summary>
/// Implementación de <see cref="ISecurityContextService"/> que lee los claims
/// del usuario autenticado desde el HttpContext de ASP.NET Core.
/// </summary>
public class CurrentUserService : ISecurityContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int Id
    {
        get
        {
            var userIdValue = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdValue, out int userId) ? userId : 0;
        }
    }

    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
}
