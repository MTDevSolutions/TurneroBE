using Domain.Entities;

namespace Application.Interfaces.Auth;

public interface ISuperadminRepository
{
    Task<Superadmin?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
