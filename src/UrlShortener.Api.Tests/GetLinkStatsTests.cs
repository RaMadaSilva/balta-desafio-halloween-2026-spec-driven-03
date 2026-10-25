using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UrlShortener.Api.Features;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Tests;

public sealed class GetLinkStatsTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0, false)]
    [InlineData(27, false)]
    [InlineData(1000, true)]
    public async Task StatisticsExposeKnownTotalsAndExpirationWithoutWriting(long count, bool expired)
    {
        using var factory = new ApiFactory();
        var expires = expired ? factory.Clock.GetUtcNow().AddSeconds(-1) : (DateTimeOffset?)null;
        factory.Seed("Known01", expiresAt: expires, count: count);
        var original = factory.ReadLink("Known01");
        using var client = factory.CreateClient();
        for (var i = 0; i < 2; i++)
        {
            using var response = await client.GetAsync("/links/Known01/stats", Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
            Assert.Equal("Known01", json.GetProperty("code").GetString());
            Assert.Equal(count, json.GetProperty("accessCount").GetInt64());
            Assert.Equal(expired, json.GetProperty("expired").GetBoolean());
            if (expires is null)
                Assert.Equal(JsonValueKind.Null, json.GetProperty("expiresAtUtc").ValueKind);
            else
                Assert.Equal(expires, json.GetProperty("expiresAtUtc").GetDateTimeOffset());
        }
        Assert.Equal(original, factory.ReadLink("Known01"));
        Assert.Equal(1, factory.RowCount());
    }

    [Fact]
    public async Task CodesAreCaseSensitiveAndUnknownCodeDoesNotCreateData()
    {
        using var factory = new ApiFactory();
        factory.Seed("Case01", count: 7);
        factory.Seed("case01", count: 9);
        using var client = factory.CreateClient();
        foreach (var pair in new[] { ("Case01", 7L), ("case01", 9L) })
        {
            var json = await client.GetFromJsonAsync<JsonElement>($"/links/{pair.Item1}/stats", Token);
            Assert.Equal(pair.Item1, json.GetProperty("code").GetString());
            Assert.Equal(pair.Item2, json.GetProperty("accessCount").GetInt64());
        }
        using var missing = await client.GetAsync("/links/Missing/stats", Token);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
        Assert.Equal(2, factory.RowCount());
    }

    [Fact]
    public async Task DatabaseFailureReturns503AndPreservesData()
    {
        using var factory = new ApiFactory();
        factory.Seed("Stable1", count: 21);
        var original = factory.ReadLink("Stable1");
        using var client = factory.CreateClient();
        factory.FailConnections = true;
        using var response = await client.GetAsync("/links/Stable1/stats", Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        factory.FailConnections = false;
        Assert.Equal(original, factory.ReadLink("Stable1"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandlerReadsWithoutChangingCounters(bool exists)
    {
        using var factory = new ApiFactory();
        factory.Seed("Known01", expiresAt: factory.Clock.GetUtcNow(), count: 11);
        var result = GetLinkStats.Handle(exists ? "Known01" : "Missing", factory.Services.GetRequiredService<SqliteConnectionFactory>(), factory.Clock);
        Assert.Equal(exists ? GetLinkStats.Status.Found : GetLinkStats.Status.Missing, result.Status);
        if (exists)
        {
            Assert.Equal(11, result.Value?.AccessCount);
            Assert.True(result.Value?.Expired);
        }
        Assert.Equal(11, factory.ReadLink("Known01")?.Count);
        Assert.Equal(1, factory.RowCount());
    }
}
