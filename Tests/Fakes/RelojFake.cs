namespace Tests.Fakes;

/// <summary>TimeProvider con la hora fija que se le indique.</summary>
public class RelojFake(DateTimeOffset ahora) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = ahora;
    public override DateTimeOffset GetUtcNow() => Ahora;
}
