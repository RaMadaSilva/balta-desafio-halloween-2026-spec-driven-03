using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace UrlShortener.Api.Tests;

public sealed class ApiDocumentationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GeneratedOpenApiMatchesOperationsSchemasResponsesAndHeaders()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var contract = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Contracts/openapi.json"), Token));
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json", Token));
        var expected = contract.RootElement;
        var actual = document.RootElement;
        Assert.StartsWith("3.1", actual.GetProperty("openapi").GetString());
        Assert.Equal(expected.GetProperty("paths").EnumerateObject().Select(item => item.Name).Order(), actual.GetProperty("paths").EnumerateObject().Select(item => item.Name).Order());
        foreach (var path in expected.GetProperty("paths").EnumerateObject())
        foreach (var operation in path.Value.EnumerateObject())
        {
            var live = actual.GetProperty("paths").GetProperty(path.Name).GetProperty(operation.Name);
            Assert.Equal(operation.Value.GetProperty("operationId").GetString(), live.GetProperty("operationId").GetString());
            if (operation.Value.TryGetProperty("requestBody", out var body))
            {
                Assert.True(live.GetProperty("requestBody").GetProperty("required").GetBoolean());
                CompareContent(expected, actual, body.GetProperty("content"), live.GetProperty("requestBody").GetProperty("content"));
            }
            if (operation.Value.TryGetProperty("parameters", out var parameters))
                foreach (var parameter in parameters.EnumerateArray())
                {
                    var wanted = Resolve(expected, parameter);
                    var given = live.GetProperty("parameters").EnumerateArray().Select(item => Resolve(actual, item)).Single(item => item.GetProperty("name").GetString() == wanted.GetProperty("name").GetString());
                    Assert.Equal("path", given.GetProperty("in").GetString());
                    Assert.True(given.GetProperty("required").GetBoolean());
                    CompareSchema(expected, actual, wanted.GetProperty("schema"), given.GetProperty("schema"));
                }
            foreach (var response in operation.Value.GetProperty("responses").EnumerateObject())
            {
                var wanted = Resolve(expected, response.Value);
                Assert.True(live.GetProperty("responses").TryGetProperty(response.Name, out var given), $"Resposta {response.Name} ausente em {path.Name}.");
                given = Resolve(actual, given);
                if (wanted.TryGetProperty("content", out var content))
                    CompareContent(expected, actual, content, given.GetProperty("content"));
                if (wanted.TryGetProperty("headers", out var headers))
                    foreach (var header in headers.EnumerateObject())
                    {
                        Assert.True(given.GetProperty("headers").TryGetProperty(header.Name, out var liveHeader), $"Cabeçalho {header.Name} ausente.");
                        if (header.Value.TryGetProperty("required", out var required) && required.GetBoolean())
                            Assert.True(liveHeader.GetProperty("required").GetBoolean());
                        CompareSchema(expected, actual, header.Value.GetProperty("schema"), liveHeader.GetProperty("schema"));
                    }
            }
        }
    }

    [Fact]
    public async Task ScalarLoadsTheDocumentAndDoesNotConsumeTheScalarCode()
    {
        using var factory = new ApiFactory();
        factory.Seed("scalar", "https://example.com/scalar-code");
        using var client = factory.CreateClient();
        using var redirect = await client.GetAsync("/scalar", Token);
        Assert.Equal("https://example.com/scalar-code", redirect.Headers.Location?.OriginalString);
        using var landing = await client.GetAsync("/docs/scalar", Token);
        var location = new Uri(client.BaseAddress!, "/docs/scalar/");
        if (landing.StatusCode == HttpStatusCode.Found)
        {
            location = new Uri(landing.RequestMessage!.RequestUri!, landing.Headers.Location!);
            Assert.StartsWith("/docs/scalar/", location.AbsolutePath);
        }
        else
            Assert.Equal(HttpStatusCode.OK, landing.StatusCode);
        using var page = await client.GetAsync(location, Token);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("text/html", page.Content.Headers.ContentType?.MediaType);
        Assert.Contains("scalar", await page.Content.ReadAsStringAsync(Token), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DocumentationIsHiddenInProduction()
    {
        using var owner = new ApiFactory();
        using var factory = owner.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/docs/scalar", Token)).StatusCode);
    }

    private static JsonElement Resolve(JsonElement root, JsonElement value)
    {
        while (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("$ref", out var reference))
        {
            value = root;
            foreach (var part in reference.GetString()![2..].Split('/'))
                value = value.GetProperty(part.Replace("~1", "/").Replace("~0", "~"));
        }
        return value;
    }

    private static void CompareContent(JsonElement expectedRoot, JsonElement actualRoot, JsonElement expected, JsonElement actual)
    {
        foreach (var media in expected.EnumerateObject())
            CompareSchema(expectedRoot, actualRoot, media.Value.GetProperty("schema"), actual.GetProperty(media.Name).GetProperty("schema"));
    }

    private static void CompareSchema(JsonElement expectedRoot, JsonElement actualRoot, JsonElement expected, JsonElement actual)
    {
        expected = Resolve(expectedRoot, expected);
        actual = Resolve(actualRoot, actual);
        Assert.Equal(JsonValueKind.Object, actual.ValueKind);
        foreach (var keyword in new[] { "type", "format", "minLength", "maxLength", "pattern", "minimum", "const", "additionalProperties" })
        {
            if (!expected.TryGetProperty(keyword, out var value))
                continue;
            Assert.True(actual.TryGetProperty(keyword, out var live), $"Metadado {keyword} ausente: {actual.GetRawText()}");
            if (keyword == "type")
            {
                var wantedTypes = value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().Select(item => item.GetString()).Order().ToArray() : [value.GetString()];
                var givenTypes = live.ValueKind == JsonValueKind.Array ? live.EnumerateArray().Select(item => item.GetString()).Order().ToArray() : [live.GetString()];
                Assert.Equal(wantedTypes, givenTypes);
            }
            else if (value.ValueKind == JsonValueKind.Number)
                Assert.Equal(value.GetDecimal(), live.GetDecimal());
            else
                Assert.Equal(value.ToString(), live.ToString());
        }
        if (expected.TryGetProperty("required", out var required))
            Assert.Equal(required.EnumerateArray().Select(item => item.GetString()).Order(), actual.GetProperty("required").EnumerateArray().Select(item => item.GetString()).Order());
        if (expected.TryGetProperty("properties", out var properties))
            foreach (var property in properties.EnumerateObject())
                CompareSchema(expectedRoot, actualRoot, property.Value, actual.GetProperty("properties").GetProperty(property.Name));
    }
}
