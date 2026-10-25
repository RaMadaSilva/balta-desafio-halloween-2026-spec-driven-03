using System.Globalization;
using Microsoft.Data.Sqlite;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class GetLinkStats
{
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
