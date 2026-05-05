using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Tests;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
