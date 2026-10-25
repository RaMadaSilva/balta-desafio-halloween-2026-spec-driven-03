using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class CreateLink
{
    public sealed record Request(string? Destination, string? Code = null, DateTimeOffset? ExpiresAtUtc = null);
    internal enum Status { Created, Invalid, Conflict, Unavailable }
    internal readonly record struct Outcome(Status Status, string? Code = null, string? Error = null);
    internal static string? Validate(Request request, TimeProvider clock) => throw new NotImplementedException();
    internal static Outcome Handle(Request request, SqliteConnectionFactory factory, TimeProvider clock, Func<string>? generate = null) => throw new NotImplementedException();
}
