using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NAudio.Loudness;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="LoudnessAnalysis"/>.
/// </summary>
public static class LoudnessAnalysisJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DoubleJsonConverter() }
    };

    private static readonly JsonSerializerOptions _indentedJsonOptions = new(_jsonSerializerOptions) { WriteIndented = true };
    private static readonly JsonSerializerOptions _nonIndentedJsonOptions = new(_jsonSerializerOptions) { WriteIndented = false };

    /// <summary>
    /// Serializes a <see cref="LoudnessAnalysis"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The loudness analysis to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the loudness analysis.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this LoudnessAnalysis value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented ? _indentedJsonOptions : _nonIndentedJsonOptions;
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="LoudnessAnalysis"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized loudness analysis, or null if the JSON is null, empty, or invalid.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static LoudnessAnalysis? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentException("Json string cannot be null or empty.", nameof(json));
        }

        try
        {
            return JsonSerializer.Deserialize<LoudnessAnalysis>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="LoudnessAnalysis"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized loudness analysis, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out LoudnessAnalysis? value)
    {
        if (string.IsNullOrEmpty(json))
        {
            value = null;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<LoudnessAnalysis>(json, _jsonSerializerOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}