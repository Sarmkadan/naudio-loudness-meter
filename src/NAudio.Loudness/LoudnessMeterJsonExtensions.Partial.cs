using System.Text.Json;

/// <summary>
/// JSON (de)serialization helpers for <see cref="LoudnessAnalysis"/>.
/// </summary>
public static partial class LoudnessMeterJsonExtensions
{
    /// <summary>
    /// Serializes a <see cref="LoudnessAnalysis"/> to JSON, preserving special double values.
    /// </summary>
    /// <param name="analysis">The analysis to serialize.</param>
    /// <returns>JSON string.</returns>
    public static string ToJson(this LoudnessAnalysis analysis)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DoubleJsonConverter());
        return JsonSerializer.Serialize(analysis, options);
    }

    /// <summary>
    /// Deserializes a JSON string back to a <see cref="LoudnessAnalysis"/>, handling special double values.
    /// </summary>
    /// <param name="json">JSON representation of a <see cref="LoudnessAnalysis"/>.</param>
    /// <returns>The deserialized analysis.</returns>
    public static LoudnessAnalysis FromJson(string json)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DoubleJsonConverter());
        return JsonSerializer.Deserialize<LoudnessAnalysis>(json, options)!;
    }
}
