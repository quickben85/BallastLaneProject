namespace TaskManager.IntegrationTests;

internal sealed class FixedTaskClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
