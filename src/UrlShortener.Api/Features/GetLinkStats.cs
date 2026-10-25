using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class GetLinkStats
{
    public sealed record Response(string Code, long AccessCount, DateTimeOffset? ExpiresAtUtc, bool Expired);
    internal enum Status { Found, Missing, Unavailable }
    internal readonly record struct Outcome(Status Status, Response? Value = null);
    internal static Outcome Handle(string code, SqliteConnectionFactory factory, TimeProvider clock) => throw new NotImplementedException();
}
