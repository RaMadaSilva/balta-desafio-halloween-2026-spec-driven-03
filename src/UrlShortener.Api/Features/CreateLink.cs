using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using UrlShortener.Api.Shared;

namespace UrlShortener.Api.Features;

public static class CreateLink
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record Request(
        [property: JsonRequired] string? Destination,
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
    internal static Outcome Handle(Request request, SqliteConnectionFactory factory, TimeProvider clock, Func<string>? generate = null) => throw new NotImplementedException();

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
