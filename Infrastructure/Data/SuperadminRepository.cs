using Application.Interfaces.Auth;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class SuperadminRepository(TurneroContext context) : ISuperadminRepository
{
    public async Task<Superadmin?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await context.Superadmins.FirstOrDefaultAsync(s => s.Email == email, cancellationToken);
}
