using System;

namespace OpenCompote.SGA.Tests;

/// <summary>
/// Custom Time provider that always returns the same Date and time. It is used for testing.
/// </summary>
public sealed class MockTimeProvider: TimeProvider
{
    private DateTimeOffset _now;

    public MockTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _now.ToUniversalTime();
    }
}
