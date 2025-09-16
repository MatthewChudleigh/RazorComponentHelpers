using System.Text.Json;
using System.Text.Json.Serialization;

namespace RazorComponentHelpers;

public class StringBooleanConverter : JsonConverter<bool>
{
    public override bool Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String when bool.TryParse(reader.GetString(), out var value) => value,
            JsonTokenType.True or JsonTokenType.False => reader.GetBoolean(),
            _ => throw new JsonException("Cannot convert token to boolean.")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        bool value,
        JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

public class StringNullIntConverter : JsonConverter<int?>
{
    public override int? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt32(out var value) => value,
            JsonTokenType.String when int.TryParse(reader.GetString(), out var value) => value,
            JsonTokenType.String when string.IsNullOrWhiteSpace(reader.GetString()) => null,
            _ => throw new JsonException("Cannot convert token to int?")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        int? value,
        JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); }
        else { writer.WriteNumberValue(value.Value); }
    }
}
