using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON converter for <see cref="double"/> that round‑trips special values
/// (<c>NaN</c>, <c>Infinity</c>, <c>-Infinity</c>) as strings.
/// </summary>
/// <remarks>
/// Reading accepts a JSON number that fits in a finite <see cref="double"/>, the exact strings
/// <c>"NaN"</c>, <c>"Infinity"</c> and <c>"-Infinity"</c>, or any other string that parses to a
/// finite number. Strings and numbers that overflow to infinity (for example <c>"1e400"</c>) are
/// rejected instead of silently producing <c>Infinity</c> that would corrupt loudness calculations.
/// Malformed input raises <see cref="JsonException"/>.
/// </remarks>
public sealed class DoubleJsonConverter : JsonConverter<double>
{
    /// <inheritdoc/>
    /// <exception cref="JsonException">
    /// The token is neither a number nor a string, the number is outside the finite
    /// <see cref="double"/> range, or the string is neither a special value nor a finite number.
    /// </exception>
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => ReadNumber(ref reader),
            JsonTokenType.String => ParseString(reader.GetString()),
            _ => throw new JsonException(
                $"Expected a JSON number or string when reading a double, but found {reader.TokenType}.")
        };
    }

    private static double ReadNumber(ref Utf8JsonReader reader) =>
        reader.TryGetDouble(out double value) && double.IsFinite(value)
            ? value
            : throw new JsonException(
                $"Number \"{Encoding.UTF8.GetString(reader.ValueSpan)}\" is outside the finite range of a double.");

    private static double ParseString(string? text) => text switch
    {
        "NaN" => double.NaN,
        "Infinity" => double.PositiveInfinity,
        "-Infinity" => double.NegativeInfinity,
        not null when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                      && double.IsFinite(value) => value,
        _ => throw new JsonException(
            $"String \"{text}\" is not a finite number or one of \"NaN\", \"Infinity\", \"-Infinity\" when reading a double.")
    };

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (double.IsFinite(value))
        {
            writer.WriteNumberValue(value);
            return;
        }

        writer.WriteStringValue(value switch
        {
            double.PositiveInfinity => "Infinity",
            double.NegativeInfinity => "-Infinity",
            _ => "NaN"
        });
    }
}
