using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NAudio.Loudness;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="TruePeakMeter"/>.
/// </summary>
public static class TruePeakMeterJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    /// <summary>
    /// Serializes a <see cref="TruePeakMeter"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The true-peak meter to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the true-peak meter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this TruePeakMeter value, bool indented = false)
        => JsonSerializer.Serialize(value, indented ? CreateIndentedOptions() : _jsonSerializerOptions);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="TruePeakMeter"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized true-peak meter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="json"/> is empty or consists only of white-space characters.</exception>
    public static TruePeakMeter? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<TruePeakMeter>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="TruePeakMeter"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized true-peak meter, or <see langword="null"/> if deserialization fails.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="json"/> is empty or consists only of white-space characters.</exception>
    public static bool TryFromJson(string json, out TruePeakMeter? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<TruePeakMeter>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static JsonSerializerOptions CreateIndentedOptions()
    {
        var options = new JsonSerializerOptions(_jsonSerializerOptions);
        options.WriteIndented = true;
        return options;
    }
}