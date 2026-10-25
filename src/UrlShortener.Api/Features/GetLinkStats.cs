using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class GetLinkStats
{
    public static RouteHandlerBuilder MapGetLinkStats(this IEndpointRouteBuilder endpoints) => endpoints
        .MapGet("/links/{code}/stats", Endpoint)
        .WithName("getLinkStats")
        .WithSummary("Consultar o total acumulado de acessos")
        .Produces<Response>()
        .ProducesProblem(404)
        .ProducesProblem(503)
        .AddOpenApiOperationTransformer((operation, _, _) =>
        {
            var code = (OpenApiParameter)operation.Parameters!.Single(parameter => parameter.Name == "code");
            code.Schema = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 4, MaxLength = 20, Pattern = "^[A-Za-z0-9]+$" };
            return Task.CompletedTask;
        });

    private static IResult Endpoint(string code, SqliteConnectionFactory factory, TimeProvider clock)
    {
        var outcome = Handle(code, factory, clock);
        return outcome.Status switch
        {
            Status.Found => Results.Ok(outcome.Value),
            Status.Missing => Results.Problem(statusCode: 404, title: "Link inexistente."),
            _ => Results.Problem(statusCode: 503, title: "A base de dados está temporariamente indisponível.")
        };
    }

    public sealed record Response(string Code, long AccessCount, DateTimeOffset? ExpiresAtUtc, bool Expired);
    internal enum Status { Found, Missing, Unavailable }
    internal readonly record struct Outcome(Status Status, Response? Value = null);
    internal static Outcome Handle(string code, SqliteConnectionFactory factory, TimeProvider clock)
    {
        try
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT code, access_count, expires_at_utc FROM links WHERE code = $code COLLATE BINARY";
            command.Parameters.AddWithValue("$code", code);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return new(Status.Missing);
            var expires = reader.IsDBNull(2) ? (DateTimeOffset?)null : DateTimeOffset.ParseExact(
                reader.GetString(2), "yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return new(Status.Found, new Response(reader.GetString(0), reader.GetInt64(1), expires,
                expires is not null && expires <= clock.GetUtcNow()));
        }
        catch (SqliteException)
        {
            return new(Status.Unavailable);
        }
    }
}
