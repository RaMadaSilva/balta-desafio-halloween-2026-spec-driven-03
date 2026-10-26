using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Tests;

public sealed class ExpirationContentionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpirationDuringWriterContentionPreventsWriting(bool create)
    {
        var token = TestContext.Current.CancellationToken;
        var clock = new ObservedClock();
        using var factory = new ApiFactory(clock: clock);
        using var client = factory.CreateClient();
        var expires = clock.GetUtcNow().AddSeconds(1);
        if (!create)
            factory.Seed("Blocked", expiresAt: expires, count: 3);
        using var connection = factory.Services.GetRequiredService<SqliteConnectionFactory>().OpenConnection();
        using var writer = connection.BeginTransaction();
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.ConnectionOpened = () => opened.TrySetResult();
        clock.Observe = true;
        var request = create
            ? client.PostAsJsonAsync("/links", new { destination = "https://example.com", code = "Blocked", expiresAtUtc = expires }, token)
            : client.GetAsync("/Blocked", token);
        try
        {
            await opened.Task.WaitAsync(TimeSpan.FromSeconds(3), token);
            // O relógio SQL só é lido após obter a escrita; o código antigo lia-o antes do bloqueio.
            await Task.WhenAny(clock.Read.Task, Task.Delay(200, token));
            Assert.False(request.IsCompleted);
            clock.Advance(TimeSpan.FromSeconds(1));
        }
        finally
        {
            writer.Rollback();
        }
        using var response = await request;
        Assert.Equal(create ? HttpStatusCode.BadRequest : HttpStatusCode.Gone, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal(create ? 0 : 1, factory.RowCount());
        if (!create)
            Assert.Equal(3, factory.ReadLink("Blocked")?.Count);
    }

    private sealed class ObservedClock : TimeProvider
    {
        private readonly TestClock clock = new();
        public bool Observe { get; set; }
        public TaskCompletionSource Read { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override DateTimeOffset GetUtcNow()
        {
            var now = clock.GetUtcNow();
            if (Observe)
                Read.TrySetResult();
            return now;
        }
        public override long TimestampFrequency => clock.TimestampFrequency;
        public override long GetTimestamp() => clock.GetTimestamp();
        public void Advance(TimeSpan duration) => clock.Advance(duration);
    }
}
