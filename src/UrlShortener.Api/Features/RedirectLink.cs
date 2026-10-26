using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class RedirectLink
{
    public static RouteHandlerBuilder MapRedirectLink(this IEndpointRouteBuilder endpoints) => endpoints
        .MapGet("/{code}", Endpoint)
        .WithName("redirectLink")
        .WithSummary("Redirecionar para o destino e contar o acesso")
        .Produces(302)
        .ProducesProblem(404)
        .ProducesProblem(410)
        .ProducesProblem(503)
        .AddOpenApiOperationTransformer((operation, _, _) =>
        {
            var code = (OpenApiParameter)operation.Parameters!.Single(parameter => parameter.Name == "code");
            code.Schema = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 4, MaxLength = 20, Pattern = "^[A-Za-z0-9]+$" };
            var response = (OpenApiResponse)operation.Responses!["302"];
            response.Headers = new Dictionary<string, IOpenApiHeader>
            {
                ["Location"] = new OpenApiHeader { Required = true, Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" } },
                ["Cache-Control"] = new OpenApiHeader { Required = true, Schema = new OpenApiSchema { Type = JsonSchemaType.String, Const = "no-store, no-cache" } },
                ["Pragma"] = new OpenApiHeader { Required = true, Schema = new OpenApiSchema { Type = JsonSchemaType.String, Const = "no-cache" } },
                ["Expires"] = new OpenApiHeader { Required = true, Schema = new OpenApiSchema { Type = JsonSchemaType.String, Const = "0" } }
            };
            return Task.CompletedTask;
        });

    private static IResult Endpoint(string code, HttpContext context, SqliteConnectionFactory factory, TimeProvider clock)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
        var outcome = Handle(code, factory, clock);
        if (outcome.Status == Status.Redirected)
        {
            var destination = outcome.Destination!;
            if (destination.Any(character => character > 127))
            {
                var uri = new UriBuilder(destination);
                uri.Host = uri.Uri.IdnHost;
                destination = uri.Uri.AbsoluteUri;
            }
            return Results.Redirect(destination);
        }
        return outcome.Status switch
        {
            Status.Missing => Results.Problem(statusCode: 404, title: "Link inexistente."),
            Status.Expired => Results.Problem(statusCode: 410, title: "O link expirou."),
            _ => Results.Problem(statusCode: 503, title: "A base de dados está temporariamente indisponível.")
        };
    }

    internal enum Status { Redirected, Missing, Expired, Unavailable }
    internal readonly record struct Outcome(Status Status, string? Destination = null);
    internal static Outcome Handle(string code, SqliteConnectionFactory factory, TimeProvider clock)
    {
        try
        {
            using var connection = factory.OpenConnection();
            connection.CreateFunction("utc_now", () => clock.GetUtcNow().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture), isDeterministic: false);
            using var update = connection.CreateCommand();
            update.CommandText = """
                UPDATE links SET access_count = access_count + 1
                WHERE code = $code COLLATE BINARY
                  AND (expires_at_utc IS NULL OR expires_at_utc > utc_now())
                RETURNING destination;
                """;
            update.Parameters.AddWithValue("$code", code);
            if (update.ExecuteScalar() is string destination)
                return new(Status.Redirected, destination);

            using var lookup = connection.CreateCommand();
            lookup.CommandText = "SELECT 1 FROM links WHERE code = $code COLLATE BINARY";
            lookup.Parameters.AddWithValue("$code", code);
            return new(lookup.ExecuteScalar() is null ? Status.Missing : Status.Expired);
        }
        catch (SqliteException)
        {
            return new(Status.Unavailable);
        }
    }
}
