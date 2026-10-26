using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace UrlShortener.Api.Tests;

public sealed class PerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreationAndRedirectMeetTheDocumentedLatencyProtocol()
    {
#if DEBUG
        Assert.Fail("Execute os ensaios de desempenho em Release.");
#endif
        Assert.False(Debugger.IsAttached);
        var token = TestContext.Current.CancellationToken;
        var sdk = typeof(PerformanceTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "SdkVersion").Value;
        output.WriteLine($"SDK: {sdk}; runtime: {Environment.Version}; SO: {RuntimeInformation.OSDescription}");
        output.WriteLine($"CPU: {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? RuntimeInformation.ProcessArchitecture.ToString()}; núcleos: {Environment.ProcessorCount}; memória disponível ao runtime: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes} bytes");
        using var factory = new ApiFactory(clock: TimeProvider.System);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-IP", "192.0.2.250");
        string? warmCode = null;
        for (var i = 0; i < 10; i++)
        {
            using var response = await client.PostAsJsonAsync("/links", new { destination = "https://example.com/warmup" }, token);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(token);
            warmCode ??= body.GetProperty("code").GetString();
        }
        for (var i = 0; i < 10; i++)
        {
            using var response = await client.GetAsync("/" + warmCode, token);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            await response.Content.ReadAsByteArrayAsync(token);
        }

        var creation = new double[100];
        string? measuredCode = null;
        for (var i = 0; i < creation.Length; i++)
        {
            client.DefaultRequestHeaders.Remove("X-Test-IP");
            client.DefaultRequestHeaders.Add("X-Test-IP", $"192.0.2.{i / 10 + 1}");
            var started = Stopwatch.GetTimestamp();
            using var response = await client.PostAsJsonAsync("/links", new { destination = "https://example.com/performance" }, token);
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            creation[i] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var body = JsonDocument.Parse(bytes);
            measuredCode ??= body.RootElement.GetProperty("code").GetString();
        }
        Assert.Equal(0, factory.ReadLink(measuredCode!)?.Count);

        var redirect = new double[1000];
        await Parallel.ForEachAsync(Enumerable.Range(0, redirect.Length), new ParallelOptions { MaxDegreeOfParallelism = 50, CancellationToken = token }, async (index, cancellation) =>
        {
            var started = Stopwatch.GetTimestamp();
            using var response = await client.GetAsync("/" + measuredCode, cancellation);
            await response.Content.ReadAsByteArrayAsync(cancellation);
            redirect[index] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal("https://example.com/performance", response.Headers.Location?.OriginalString);
        });
        var stats = await client.GetFromJsonAsync<JsonElement>($"/links/{measuredCode}/stats", token);
        Assert.Equal(1000, stats.GetProperty("accessCount").GetInt64());
        Array.Sort(creation);
        Array.Sort(redirect);
        var creationP95 = creation[(int)Math.Ceiling(0.95 * creation.Length) - 1];
        var redirectP95 = redirect[(int)Math.Ceiling(0.95 * redirect.Length) - 1];
        output.WriteLine($"Criação: N=100, p95={creationP95:F3} ms, 100 respostas 201; redirecionamento: N=1000, concorrência=50, p95={redirectP95:F3} ms, 1000 respostas 302 e total=1000.");
        Assert.True(creationP95 <= 2000, $"p95 de criação: {creationP95:F3} ms.");
        Assert.True(redirectP95 <= 1000, $"p95 de redirecionamento: {redirectP95:F3} ms.");
    }
}
