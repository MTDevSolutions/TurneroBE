using Application.UseCases.Security_Context;

namespace Tests.Fakes;

public class SecurityContextServiceFake(int id = 1, string? userName = "test") : ISecurityContextService
{
    public int Id { get; } = id;
    public string? UserName { get; } = userName;
}
