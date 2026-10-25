using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class RedirectLink
{
    internal enum Status { Redirected, Missing, Expired, Unavailable }
    internal readonly record struct Outcome(Status Status, string? Destination = null);
    internal static Outcome Handle(string code, SqliteConnectionFactory factory, TimeProvider clock) => throw new NotImplementedException();
}
