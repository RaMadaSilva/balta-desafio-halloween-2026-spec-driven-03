using System.Globalization;
using Microsoft.Data.Sqlite;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class RedirectLink
{
    internal enum Status { Redirected, Missing, Expired, Unavailable }
    internal readonly record struct Outcome(Status Status, string? Destination = null);
    internal static Outcome Handle(string code, SqliteConnectionFactory factory, TimeProvider clock)
    {
        try
        {
            using var connection = factory.OpenConnection();
            using var update = connection.CreateCommand();
            update.CommandText = """
                UPDATE links SET access_count = access_count + 1
                WHERE code = $code COLLATE BINARY
                  AND (expires_at_utc IS NULL OR expires_at_utc > $now_utc)
                RETURNING destination;
                """;
            update.Parameters.AddWithValue("$code", code);
            update.Parameters.AddWithValue("$now_utc", clock.GetUtcNow().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));
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
