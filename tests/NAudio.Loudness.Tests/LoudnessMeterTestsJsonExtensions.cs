using System;
using System.Text.Json;

/// <summary>
/// JSON (de)serialization helpers for <see cref="LoudnessMeterTests"/>.
/// </summary>
namespace NAudio.Loudness.Tests
{
	/// <summary>
	/// Provides extension methods to serialize and deserialize <see cref="LoudnessMeterTests"/> instances
	/// using <see cref="System.Text.Json"/>.
	/// </summary>
	public static class LoudnessMeterTestsJsonExtensions
	{
		// Cached options: camel‑case property names, case‑insensitive, no indentation by default.
		private static readonly JsonSerializerOptions _options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true
		};

		/// <summary>
		/// Serializes the specified <paramref name="value"/> to a JSON string.
		/// </summary>
		/// <param name="value">The <see cref="LoudnessMeterTests"/> instance to serialize.</param>
		/// <param name="indented">If <c>true</c>, the output JSON will be formatted with indentation.</param>
		/// <returns>A JSON representation of <paramref name="value"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
		public static string ToJson(this LoudnessMeterTests value, bool indented = false)
		{
			ArgumentNullException.ThrowIfNull(value);
			return JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options);
		}

		/// <summary>
		/// Deserializes a JSON string to a <see cref="LoudnessMeterTests"/> instance.
		/// </summary>
		/// <param name="json">The JSON string representing a <see cref="LoudnessMeterTests"/>.
		/// Must not be <see langword="null"/> and must contain valid JSON for a <see cref="LoudnessMeterTests"/> object.</param>
		/// <returns>The deserialized <see cref="LoudnessMeterTests"/> object, or <c>null</c> if the JSON represents <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
		/// <exception cref="JsonException">The JSON is invalid or cannot be deserialized to <see cref="LoudnessMeterTests"/>.</exception>
		public static LoudnessMeterTests? FromJson(string json)
		{
			ArgumentNullException.ThrowIfNull(json);
			return JsonSerializer.Deserialize<LoudnessMeterTests>(json, _options);
		}

		/// <summary>
		/// Attempts to deserialize a JSON string to a <see cref="LoudnessMeterTests"/> instance.
		/// </summary>
		/// <param name="json">The JSON string representing a <see cref="LoudnessMeterTests"/>.
		/// Must not be <see langword="null"/> and must contain valid JSON for a <see cref="LoudnessMeterTests"/> object.</param>
		/// <param name="value">When this method returns, contains the deserialized object if the operation succeeded; otherwise <c>null</c>.</param>
		/// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
		public static bool TryFromJson(string json, out LoudnessMeterTests? value)
		{
			ArgumentNullException.ThrowIfNull(json);
			try
			{
				value = JsonSerializer.Deserialize<LoudnessMeterTests>(json, _options);
				return true;
			}
			catch (JsonException)
			{
				value = null;
				return false;
			}
		}
	}
}
