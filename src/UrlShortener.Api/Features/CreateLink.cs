using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class CreateLink
{
    public sealed record Response(string Code, string ShortUrl, string Destination, DateTimeOffset? ExpiresAtUtc);

    public static RouteHandlerBuilder MapCreateLink(this IEndpointRouteBuilder endpoints) => endpoints
        .MapPost("/links", Endpoint)
        .WithName("createLink")
        .WithSummary("Criar um link curto")
        .Accepts<Request>("application/json")
        .Produces<Response>(201)
        .ProducesProblem(400)
        .ProducesProblem(409)
        .ProducesProblem(429)
        .ProducesProblem(503)
        .RequireRateLimiting("create-links")
        .AddOpenApiOperationTransformer((operation, _, _) =>
        {
            var created = (OpenApiResponse)operation.Responses!["201"];
            created.Headers ??= new Dictionary<string, IOpenApiHeader>();
            created.Headers["Location"] = new OpenApiHeader
            {
                Description = "URL relativa das estatísticas do link criado.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            };
            var limited = (OpenApiResponse)operation.Responses["429"];
            limited.Headers ??= new Dictionary<string, IOpenApiHeader>();
            limited.Headers["Retry-After"] = new OpenApiHeader
            {
                Description = "Segundos até libertar a permissão mais antiga, arredondados para cima.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Minimum = "1" }
            };
            return Task.CompletedTask;
        });

    private static async Task<IResult> Endpoint(HttpContext context, SqliteConnectionFactory factory, TimeProvider clock)
    {
        if (!context.Request.HasJsonContentType())
            return Results.Problem(statusCode: 400, title: "O pedido deve conter JSON.");
        Request? request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<Request>(context.RequestAborted);
        }
        catch (JsonException exception)
        {
            return Results.Problem(statusCode: 400, title: "Pedido JSON inválido.", detail: exception.Message);
        }
        if (request is null)
            return Results.Problem(statusCode: 400, title: "O corpo do pedido é obrigatório.");
        var outcome = Handle(request, factory, clock);
        return outcome.Status switch
        {
            Status.Created => Results.Created($"{context.Request.PathBase}/links/{outcome.Code}/stats",
                new Response(outcome.Code!, $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/{outcome.Code}", request.Destination, request.ExpiresAtUtc)),
            Status.Invalid => Results.Problem(statusCode: 400, title: "Dados inválidos.", detail: outcome.Error),
            Status.Conflict => Results.Problem(statusCode: 409, title: "Código indisponível.", detail: outcome.Error),
            _ => Results.Problem(statusCode: 503, title: "Serviço indisponível.", detail: outcome.Error)
        };
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record Request(
        [property: JsonRequired] string Destination,
        string? Code = null,
        [property: JsonConverter(typeof(UtcDateTimeOffsetConverter))] DateTimeOffset? ExpiresAtUtc = null);
    internal enum Status { Created, Invalid, Conflict, Unavailable }
    internal readonly record struct Outcome(Status Status, string? Code = null, string? Error = null);
    internal static string? Validate(Request request, TimeProvider clock)
    {
        if (string.IsNullOrEmpty(request.Destination) || request.Destination.Length > 2048 ||
            !Uri.TryCreate(request.Destination, UriKind.Absolute, out var destination) ||
            destination.Scheme is not ("http" or "https") || string.IsNullOrEmpty(destination.Host) ||
            !destination.IsWellFormedOriginalString())
            return "O destino deve ser uma URL absoluta http ou https com 1 a 2048 caracteres.";
        if (request.Code is not null && (request.Code.Length is < 4 or > 20 || request.Code.Any(character => !char.IsAsciiLetterOrDigit(character))))
            return "O código deve conter 4 a 20 letras ASCII ou algarismos.";
        if (request.ExpiresAtUtc is { } expires && (expires.Offset != TimeSpan.Zero || expires <= clock.GetUtcNow()))
            return "A expiração deve ser um instante futuro em UTC.";
        return null;
    }
    internal static Outcome Handle(Request request, SqliteConnectionFactory factory, TimeProvider clock, Func<string>? generate = null)
    {
        var error = Validate(request, clock);
        if (error is not null)
            return new(Status.Invalid, Error: error);
        generate ??= GenerateCode;
        try
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO links(code, destination, expires_at_utc, access_count) VALUES ($code, $destination, $expires, 0)";
            var codeParameter = command.Parameters.AddWithValue("$code", "");
            command.Parameters.AddWithValue("$destination", request.Destination);
            command.Parameters.AddWithValue("$expires", (object?)request.ExpiresAtUtc?.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture) ?? DBNull.Value);
            for (var attempt = 0; attempt < (request.Code is null ? 32 : 1); attempt++)
            {
                var code = request.Code ?? generate();
                codeParameter.Value = code;
                try
                {
                    command.ExecuteNonQuery();
                    return new(Status.Created, code);
                }
                catch (SqliteException exception) when (exception.SqliteErrorCode == 19 && exception.SqliteExtendedErrorCode is 1555 or 2067)
                {
                    if (request.Code is not null)
                        return new(Status.Conflict, Error: "O código personalizado já existe.");
                }
            }
            return new(Status.Unavailable, Error: "Não foi possível gerar um código único.");
        }
        catch (SqliteException)
        {
            return new(Status.Unavailable, Error: "A base de dados está temporariamente indisponível.");
        }
    }

    private static string GenerateCode() => string.Create(7, 0, static (characters, _) =>
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        for (var i = 0; i < characters.Length; i++)
            characters[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
    });

    public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("A expiração deve ser uma data em UTC.");
            var text = reader.GetString()!;
            if (!(text.EndsWith('Z') || text.EndsWith("+00:00", StringComparison.Ordinal)) ||
                !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) || value.Offset != TimeSpan.Zero)
                throw new JsonException("A expiração deve indicar explicitamente UTC.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }
}
