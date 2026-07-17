using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NAudio.Loudness;

/// <summary>
/// Provides JSON serialization and deserialization for <see cref="LoudnessMeter"/> instances.
/// </summary>
public static class LoudnessMeterJsonExtensions
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Converts a <see cref="LoudnessMeter"/> to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="LoudnessMeter"/> to serialize.</param>
    /// <param name="indented">Whether to indent the JSON output.</param>
    /// <returns>A JSON string representing the <see cref="LoudnessMeter"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this LoudnessMeter value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? s_jsonOptions
            : new JsonSerializerOptions(s_jsonOptions) { WriteIndented = false };
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Creates a <see cref="LoudnessMeter"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="LoudnessMeter"/> deserialized from the JSON string, or <c>null</c> if the JSON is invalid.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is <c>null</c> or empty.</exception>
    public static LoudnessMeter? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<LoudnessMeter>(json, s_jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to create a <see cref="LoudnessMeter"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">When this method returns, contains the deserialized <see cref="LoudnessMeter"/> if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the JSON was successfully deserialized; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is <c>null</c> or empty.</exception>
    public static bool TryFromJson(string json, out LoudnessMeter? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<LoudnessMeter>(json, s_jsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
