using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UrlShortener.Api.Features;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Tests;

public sealed class CreateLinkTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void ValidationAcceptsValidInput()
    {
        var clock = new TestClock();
        Assert.Null(CreateLink.Validate(new("https://example.com"), clock));
        Assert.Null(CreateLink.Validate(new("http://example.com", "Abcd", clock.GetUtcNow().AddHours(1)), clock));
    }

    [Fact]
    public async Task AutomaticCreationReturnsCodeLocationAndPersistedData()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/links", new { destination = "https://example.com/article" }, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var code = json.GetProperty("code").GetString()!;
        Assert.Matches("^[A-Za-z0-9]{7}$", code);
        Assert.Equal($"/links/{code}/stats", response.Headers.Location?.OriginalString);
        Assert.Equal($"http://localhost/{code}", json.GetProperty("shortUrl").GetString());
        Assert.Equal("https://example.com/article", json.GetProperty("destination").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("expiresAtUtc").ValueKind);
        Assert.Equal(("https://example.com/article", (string?)null, 0L), factory.ReadLink(code));
    }

    [Theory]
    [InlineData("Abcd")]
    [InlineData("abcd")]
    [InlineData("01234567890123456789")]
    public async Task CustomCodeAndFutureExpirationArePreserved(string code)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var expires = factory.Clock.GetUtcNow().AddHours(1);
        using var response = await client.PostAsJsonAsync("/links", new { destination = "http://example.com/", code, expiresAtUtc = expires }, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(code, json.GetProperty("code").GetString());
        Assert.Equal(expires, json.GetProperty("expiresAtUtc").GetDateTimeOffset());
        Assert.Equal("http://example.com/", factory.ReadLink(code)?.Destination);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("012345678901234567890")]
    [InlineData("bad_")]
    [InlineData("ação")]
    public async Task InvalidCodesReturnProblemWithoutWriting(string code)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/links", new { destination = "https://example.com", code }, Token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, factory.RowCount());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/relative")]
    [InlineData("ftp://example.com")]
    [InlineData("https://")]
    [InlineData("https://example.com/\r\nInjected: true")]
    public async Task InvalidDestinationsAreRejected(string? destination)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/links", new { destination }, Token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.RowCount());
    }

    [Theory]
    [InlineData(2048, HttpStatusCode.Created)]
    [InlineData(2049, HttpStatusCode.BadRequest)]
    public async Task DestinationLengthBoundaryIsEnforced(int length, HttpStatusCode expected)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var destination = "https://example.com/".PadRight(length, 'a');
        using var response = await client.PostAsJsonAsync("/links", new { destination }, Token);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(expected == HttpStatusCode.Created ? 1 : 0, factory.RowCount());
    }

    [Theory]
    [InlineData("2026-10-25T11:00:00Z")]
    [InlineData("2026-10-25T12:00:00Z")]
    [InlineData("2027-01-01T00:00:00+01:00")]
    [InlineData("2027-01-01T00:00:00")]
    [InlineData("01/02/2027 00:00:00+00:00")]
    public async Task InvalidExpirationIsRejected(string expiresAtUtc)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/links", new { destination = "https://example.com", expiresAtUtc }, Token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.RowCount());
    }

    [Fact]
    public async Task DuplicateCodeDoesNotChangeOriginalAndCaseIsSignificant()
    {
        using var factory = new ApiFactory();
        factory.Seed("Taken01", count: 9);
        var original = factory.ReadLink("Taken01");
        using var client = factory.CreateClient();
        using var duplicate = await client.PostAsJsonAsync("/links", new { destination = "https://other.example", code = "Taken01" }, Token);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(original, factory.ReadLink("Taken01"));
        using var distinct = await client.PostAsJsonAsync("/links", new { destination = "https://other.example", code = "taken01" }, Token);
        Assert.Equal(HttpStatusCode.Created, distinct.StatusCode);
        Assert.Equal(2, factory.RowCount());
    }

    [Fact]
    public void HandlerGeneratesOneThousandUniqueCodes()
    {
        using var factory = new ApiFactory();
        var connections = factory.Services.GetRequiredService<SqliteConnectionFactory>();
        var codes = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 1000; i++)
        {
            var result = CreateLink.Handle(new("https://example.com"), connections, factory.Clock);
            Assert.Equal(CreateLink.Status.Created, result.Status);
            Assert.Matches("^[A-Za-z0-9]{7}$", result.Code!);
            Assert.True(codes.Add(result.Code!));
        }
        Assert.Equal(1000, factory.RowCount());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandlerRetriesCollisionsAndStopsAfterThirtyTwoAttempts(bool exhaust)
    {
        using var factory = new ApiFactory();
        factory.Seed("Taken01", count: 7);
        var original = factory.ReadLink("Taken01");
        var attempts = 0;
        var result = CreateLink.Handle(new("https://new.example"), factory.Services.GetRequiredService<SqliteConnectionFactory>(), factory.Clock,
            () => ++attempts == 1 || exhaust ? "Taken01" : "Fresh01");
        Assert.Equal(exhaust ? CreateLink.Status.Unavailable : CreateLink.Status.Created, result.Status);
        Assert.Equal(exhaust ? 32 : 2, attempts);
        Assert.Equal(exhaust ? 1 : 2, factory.RowCount());
        Assert.Equal(original, factory.ReadLink("Taken01"));
        if (!exhaust)
            Assert.Equal("https://new.example", factory.ReadLink("Fresh01")?.Destination);
    }

    [Fact]
    public async Task UnavailableDatabaseReturnsProblemWithoutWriting()
    {
        using var factory = new ApiFactory();
        factory.Seed("Stable1", count: 4);
        var original = factory.ReadLink("Stable1");
        using var client = factory.CreateClient();
        factory.FailConnections = true;
        using var response = await client.PostAsJsonAsync("/links", new { destination = "https://example.com", code = "Fresh01" }, Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        factory.FailConnections = false;
        Assert.Equal(1, factory.RowCount());
        Assert.Equal(original, factory.ReadLink("Stable1"));
    }

    [Fact]
    public async Task ExactWindowAndIpIsolationAreEnforcedThroughHttp()
    {
        var clock = new TestClock();
        clock.Advance(TimeSpan.FromSeconds(9));
        using var factory = new ApiFactory(clock: clock);
        using var client = factory.CreateClient();
        for (var i = 0; i < 10; i++)
        {
            using var accepted = await client.PostAsJsonAsync("/links", new { destination = "https://example.com" }, Token);
            Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        }
        using var rejected = await client.PostAsJsonAsync("/links", new { destination = "https://example.com" }, Token);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("60", Assert.Single(rejected.Headers.GetValues("Retry-After")));
        clock.Advance(TimeSpan.FromMilliseconds(59999));
        using var stillRejected = await client.PostAsJsonAsync("/links", new { destination = "https://example.com" }, Token);
        Assert.Equal(HttpStatusCode.TooManyRequests, stillRejected.StatusCode);
        Assert.Equal("1", Assert.Single(stillRejected.Headers.GetValues("Retry-After")));
        Assert.Equal(10, factory.RowCount());
        client.DefaultRequestHeaders.Add("X-Test-IP", "192.0.2.2");
        using var otherIp = await client.PostAsJsonAsync("/links", new { destination = "https://example.com" }, Token);
        Assert.Equal(HttpStatusCode.Created, otherIp.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-IP");
        clock.Advance(TimeSpan.FromMilliseconds(1));
        using var restored = await client.PostAsJsonAsync("/links", new { destination = "https://example.com" }, Token);
        Assert.Equal(HttpStatusCode.Created, restored.StatusCode);
        Assert.Equal(12, factory.RowCount());
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"destination\":\"https://example.com\",\"unknown\":true}")]
    public async Task InvalidJsonContractReturnsProblem(string body)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/links", new StringContent(body, Encoding.UTF8, "application/json"), Token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, factory.RowCount());
    }

    [Theory]
    [InlineData("ftp://example.com", null)]
    [InlineData("https://example.com", "bad!")]
    public void ValidationRejectsInvalidValues(string destination, string? code) =>
        Assert.NotNull(CreateLink.Validate(new(destination, code), new TestClock()));
}
