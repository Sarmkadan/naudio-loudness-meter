using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON converter for <see cref="double"/> that round‑trips special values
/// (<c>NaN</c>, <c>Infinity</c>, <c>-Infinity</c>) as strings.
/// </summary>
public sealed class DoubleJsonConverter : JsonConverter<double>
{
    /// <inheritdoc/>
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String => ParseSpecialString(reader.GetString()!),
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing a double.")
        };
    }

    private static double ParseSpecialString(string s) => s switch
    {
        "NaN" => double.NaN,
        "Infinity" => double.PositiveInfinity,
        "-Infinity" => double.NegativeInfinity,
        _ => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)
    };

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNaN(value))
        {
            writer.WriteStringValue("NaN");
        }
        else if (double.IsPositiveInfinity(value))
        {
            writer.WriteStringValue("Infinity");
        }
        else if (double.IsNegativeInfinity(value))
        {
            writer.WriteStringValue("-Infinity");
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}
