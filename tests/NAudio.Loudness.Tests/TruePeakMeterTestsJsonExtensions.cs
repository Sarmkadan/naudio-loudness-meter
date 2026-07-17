using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="TruePeakMeterTests"/>.
/// </summary>
public static class TruePeakMeterTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes a <see cref="TruePeakMeterTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToJson(this TruePeakMeterTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="TruePeakMeterTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="TruePeakMeterTests"/> instance, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized.</exception>
    public static TruePeakMeterTests? FromJson(string? json)
    {
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<TruePeakMeterTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="TruePeakMeterTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized value if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string? json, out TruePeakMeterTests? value)
    {
        value = null;

        return !string.IsNullOrEmpty(json)
            && TryDeserialize(json, out value);

        static bool TryDeserialize(string jsonToParse, out TruePeakMeterTests? result)
        {
            try
            {
                result = JsonSerializer.Deserialize<TruePeakMeterTests>(jsonToParse, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                result = null;
                return false;
            }
        }
    }
}