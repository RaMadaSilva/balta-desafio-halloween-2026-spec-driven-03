using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UrlShortener.Api.Tests;

public sealed class PersistenceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestartPreservesAllFieldsAndContinuesFromPersistedTotal(bool? expire)
    {
        var path = Path.Combine(Path.GetTempPath(), "restart-" + Guid.NewGuid().ToString("N") + ".db");
        var clock = new TestClock();
        var expires = expire is null ? (DateTimeOffset?)null : clock.GetUtcNow().AddSeconds(5);
        (string Destination, string? Expires, long Count)? saved;
        try
        {
            using (var first = new ApiFactory(path, clock))
            using (var client = first.CreateClient())
            {
                using var created = await client.PostAsJsonAsync("/links", new { destination = "https://example.com/persisted", code = "Persist1", expiresAtUtc = expires }, Token);
                Assert.Equal(HttpStatusCode.Created, created.StatusCode);
                using var access = await client.GetAsync("/Persist1", Token);
                Assert.Equal(HttpStatusCode.Found, access.StatusCode);
                saved = first.ReadLink("Persist1");
                Assert.Equal(1, saved?.Count);
            }
            if (expire == true)
                clock.Advance(TimeSpan.FromSeconds(5));
            using (var second = new ApiFactory(path, clock))
            using (var client = second.CreateClient())
            {
                Assert.Equal(saved, second.ReadLink("Persist1"));
                var stats = await client.GetFromJsonAsync<JsonElement>("/links/Persist1/stats", Token);
                Assert.Equal(1, stats.GetProperty("accessCount").GetInt64());
                Assert.Equal(expire == true, stats.GetProperty("expired").GetBoolean());
                using var access = await client.GetAsync("/Persist1", Token);
                Assert.Equal(expire == true ? HttpStatusCode.Gone : HttpStatusCode.Found, access.StatusCode);
                Assert.Equal(expire == true ? 1 : 2, second.ReadLink("Persist1")?.Count);
            }
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                File.Delete(path + suffix);
        }
    }

    [Fact]
    public async Task CreationLimitDoesNotBlockRedirectOrStatistics()
    {
        using var factory = new ApiFactory();
        factory.Seed("Stable1");
        using var client = factory.CreateClient();
        for (var i = 0; i < 10; i++)
        {
            using var accepted = await client.PostAsJsonAsync("/links", new { destination = "https://example.com" }, Token);
            Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        }
        using var refused = await client.PostAsJsonAsync("/links", new { destination = "https://example.com" }, Token);
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        using var redirect = await client.GetAsync("/Stable1", Token);
        Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);
        var stats = await client.GetFromJsonAsync<JsonElement>("/links/Stable1/stats", Token);
        Assert.Equal(1, stats.GetProperty("accessCount").GetInt64());
        Assert.Equal(11, factory.RowCount());
    }
}
