using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectManagement.Core.Jira;

/// <summary>
/// Handles Jira's ISO 8601 date format, which uses <c>+HHMM</c> (no colon) offsets
/// such as <c>2023-09-15T10:30:00.000+0000</c> that System.Text.Json rejects by default.
/// </summary>
internal sealed class JiraDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var raw = reader.GetString();
        if (string.IsNullOrEmpty(raw))
            return null;

        return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
    }
}
