using System.Net;
using UrlShortener.Api.Features;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Tests;

public sealed class RedirectLinkTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EachActiveAccessRedirectsAndIncrementsOnceWithoutCache(bool expires)
    {
        using var factory = new ApiFactory();
        factory.Seed("Active1", "https://example.com/destination", expires ? factory.Clock.GetUtcNow().AddHours(1) : null, 7);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Active1", Token);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://example.com/destination", response.Headers.Location?.OriginalString);
        Assert.Equal("no-store, no-cache", Assert.Single(response.Headers.GetValues("Cache-Control")));
        Assert.Equal("no-cache", Assert.Single(response.Headers.GetValues("Pragma")));
        Assert.Equal("0", Assert.Single(response.Content.Headers.GetValues("Expires")));
        Assert.Equal(8, factory.ReadLink("Active1")?.Count);
        using var repeated = await client.GetAsync("/Active1", Token);
        Assert.Equal(HttpStatusCode.Found, repeated.StatusCode);
        Assert.Equal(9, factory.ReadLink("Active1")?.Count);
    }

    [Fact]
    public async Task ExpirationBoundaryAndUnknownCodesDoNotIncrement()
    {
        var clock = new TestClock();
        using var factory = new ApiFactory(clock: clock);
        factory.Seed("Expired", expiresAt: clock.GetUtcNow().AddSeconds(1), count: 10);
        var original = factory.ReadLink("Expired");
        clock.Advance(TimeSpan.FromSeconds(1));
        using var client = factory.CreateClient();
        using var expired = await client.GetAsync("/Expired", Token);
        Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);
        Assert.Null(expired.Headers.Location);
        Assert.Equal("application/problem+json", expired.Content.Headers.ContentType?.MediaType);
        using var missing = await client.GetAsync("/Missing", Token);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(original, factory.ReadLink("Expired"));
        Assert.Equal(1, factory.RowCount());
    }

    [Fact]
    public async Task CodesKeepTheirCaseAndScalarRemainsAUsableCode()
    {
        using var factory = new ApiFactory();
        factory.Seed("Mixed01", "https://first.example/path");
        factory.Seed("mixed01", "https://second.example/path");
        factory.Seed("scalar", "https://third.example/path");
        using var client = factory.CreateClient();
        foreach (var pair in new[] { ("Mixed01", "https://first.example/path"), ("mixed01", "https://second.example/path"), ("scalar", "https://third.example/path") })
        {
            using var response = await client.GetAsync("/" + pair.Item1, Token);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(pair.Item2, response.Headers.Location?.OriginalString);
            Assert.Equal(1, factory.ReadLink(pair.Item1)?.Count);
        }
    }

    [Fact]
    public async Task UnicodeDestinationsAreEncodedForTheLocationHeader()
    {
        using var factory = new ApiFactory();
        factory.Seed("Unicode", "https://example.com/ação");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Unicode", Token);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://example.com/a%C3%A7%C3%A3o", response.Headers.Location?.OriginalString);
        Assert.Equal(1, factory.ReadLink("Unicode")?.Count);
    }

    [Fact]
    public async Task OneThousandConcurrentAccessesAreCountedExactly()
    {
        using var factory = new ApiFactory();
        factory.Seed("Popular");
        using var client = factory.CreateClient();
        await Parallel.ForEachAsync(Enumerable.Range(0, 1000), new ParallelOptions { MaxDegreeOfParallelism = 50, CancellationToken = Token }, async (_, cancellation) =>
        {
            using var response = await client.GetAsync("/Popular", cancellation);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        });
        Assert.Equal(1000, factory.ReadLink("Popular")?.Count);
    }

    [Fact]
    public async Task DatabaseFailureReturns503WithoutLocationOrIncrement()
    {
        using var factory = new ApiFactory();
        factory.Seed("Stable1", count: 5);
        var original = factory.ReadLink("Stable1");
        using var client = factory.CreateClient();
        factory.FailConnections = true;
        using var response = await client.GetAsync("/Stable1", Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(response.Headers.Location);
        factory.FailConnections = false;
        Assert.Equal(original, factory.ReadLink("Stable1"));
    }

    [Theory]
    [InlineData("Active1", RedirectLink.Status.Redirected, 4)]
    [InlineData("Expired", RedirectLink.Status.Expired, 3)]
    [InlineData("Missing", RedirectLink.Status.Missing, 0)]
    internal void HandlerChangesOnlyActiveRows(string code, RedirectLink.Status expected, long count)
    {
        using var factory = new ApiFactory();
        factory.Seed("Active1", count: 3);
        factory.Seed("Expired", expiresAt: factory.Clock.GetUtcNow(), count: 3);
        var result = RedirectLink.Handle(code, factory.Services.GetRequiredService<SqliteConnectionFactory>(), factory.Clock);
        Assert.Equal(expected, result.Status);
        Assert.Equal(count, factory.ReadLink(code)?.Count ?? 0);
        Assert.Equal(2, factory.RowCount());
    }
}
