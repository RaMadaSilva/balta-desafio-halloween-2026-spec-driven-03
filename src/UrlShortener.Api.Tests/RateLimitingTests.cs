using System.Threading.RateLimiting;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Tests;

public sealed class RateLimitingTests
{
    [Fact]
    public void PermitsExpireExactlySixtySecondsAfterAdmission()
    {
        var clock = new TestClock();
        clock.Advance(TimeSpan.FromSeconds(9));
        using var limiter = new RateLimitingExtensions.ExactWindowLimiter(clock);
        for (var i = 0; i < 10; i++)
            Assert.True(limiter.AttemptAcquire().IsAcquired);
        var rejected = limiter.AttemptAcquire();
        Assert.False(rejected.IsAcquired);
        Assert.True(rejected.TryGetMetadata(MetadataName.RetryAfter, out var retry));
        Assert.Equal(TimeSpan.FromSeconds(60), retry);
        clock.Advance(TimeSpan.FromMilliseconds(59999));
        Assert.False(limiter.AttemptAcquire().IsAcquired);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(limiter.AttemptAcquire().IsAcquired);
    }

    [Fact]
    public void ConcurrentRequestsCannotExceedTenPermits()
    {
        using var limiter = new RateLimitingExtensions.ExactWindowLimiter(new TestClock());
        var admitted = 0;
        Parallel.For(0, 100, _ =>
        {
            using var lease = limiter.AttemptAcquire();
            if (lease.IsAcquired)
                Interlocked.Increment(ref admitted);
        });
        Assert.Equal(10, admitted);
    }
}
