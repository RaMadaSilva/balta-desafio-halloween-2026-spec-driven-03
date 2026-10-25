namespace UrlShortener.Api.Tests;

public sealed class TestClock : TimeProvider
{
    private long elapsed;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref elapsed);
    public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero).AddTicks(GetTimestamp());
    public void Advance(TimeSpan duration) => Interlocked.Add(ref elapsed, duration.Ticks);
}
