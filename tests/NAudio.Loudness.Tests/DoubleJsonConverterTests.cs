using System.Text.Json;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Unit tests for <see cref="DoubleJsonConverter"/>: round-tripping of finite and special values,
/// rejection of malformed or hostile tokens with <see cref="JsonException"/>, and edge cases such as
/// overflowing exponents and negative zero.
/// </summary>
public class DoubleJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new DoubleJsonConverter() }
    };

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("\"\"")]
    [InlineData("\"1,5\"")]
    [InlineData("\"-14.5 LUFS\"")]
    public void Read_MalformedString_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>(json, Options));
    }

    [Theory]
    [InlineData("\"infinity\"")]
    [InlineData("\"1e400\"")]
    [InlineData("\"-1e400\"")]
    public void Read_StringThatOverflowsToInfinity_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>(json, Options));
    }

    [Fact]
    public void Read_ObjectToken_ThrowsJsonExceptionNamingToken()
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>("{}", Options));

        Assert.Contains("StartObject", ex.Message);
    }

    [Fact]
    public void Read_ArrayToken_ThrowsJsonExceptionNamingToken()
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>("[1]", Options));

        Assert.Contains("StartArray", ex.Message);
    }

    [Fact]
    public void Read_BooleanToken_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>("true", Options));
    }

    [Fact]
    public void Read_NullToken_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>("null", Options));
    }

    [Fact]
    public void Read_NumberAtDoubleMaxValue_ReturnsFiniteValue()
    {
        var value = JsonSerializer.Deserialize<double>("1e308", Options);

        Assert.True(double.IsFinite(value));
        Assert.Equal(1e308, value);
    }

    [Fact]
    public void Read_NumberBeyondDoubleRange_ThrowsJsonException()
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>("1e309", Options));

        Assert.Contains("1e309", ex.Message);
    }

    [Fact]
    public void Read_NegativeZeroNumber_PreservesSign()
    {
        var value = JsonSerializer.Deserialize<double>("-0", Options);

        Assert.Equal(0.0, value);
        Assert.True(double.IsNegative(value));
    }

    [Fact]
    public void Read_NegativeZeroString_PreservesSign()
    {
        var value = JsonSerializer.Deserialize<double>("\"-0\"", Options);

        Assert.True(double.IsNegative(value));
    }

    [Fact]
    public void Read_FiniteNumericString_ReturnsValue()
    {
        var value = JsonSerializer.Deserialize<double>("\"-14.5\"", Options);

        Assert.Equal(-14.5, value);
    }

    [Theory]
    [InlineData("\"NaN\"", double.NaN)]
    [InlineData("\"Infinity\"", double.PositiveInfinity)]
    [InlineData("\"-Infinity\"", double.NegativeInfinity)]
    public void Read_SpecialStrings_ReturnSpecialValues(string json, double expected)
    {
        var value = JsonSerializer.Deserialize<double>(json, Options);

        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(-23.0)]
    [InlineData(-70.0)]
    [InlineData(-0.0)]
    [InlineData(1e308)]
    [InlineData(double.Epsilon)]
    [InlineData(-1.2345678901234567)]
    public void RoundTrip_FiniteValue_ReturnsSameValueAndSign(double original)
    {
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<double>(json, Options);

        Assert.Equal(original, restored);
        Assert.Equal(double.IsNegative(original), double.IsNegative(restored));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RoundTrip_SpecialValue_IsWrittenAsStringAndRestored(double original)
    {
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<double>(json, Options);

        Assert.StartsWith("\"", json);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void Write_NegativeZero_EmitsSignedZeroNumber()
    {
        var json = JsonSerializer.Serialize(-0.0, Options);

        Assert.Equal("-0", json);
    }

    [Fact]
    public void Write_NullWriter_ThrowsArgumentNullException()
    {
        var converter = new DoubleJsonConverter();

        Assert.Throws<ArgumentNullException>(() => converter.Write(null!, 1.0, Options));
    }
}
