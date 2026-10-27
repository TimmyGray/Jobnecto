namespace JobNecto.Tests.TestDoubles;

/// <summary><see cref="TimeProvider"/> frozen at a given instant.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
