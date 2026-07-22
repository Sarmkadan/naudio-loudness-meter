using System;
using System.Text.Json;
using NAudio.Loudness;
using Xunit;

namespace NAudio.Loudness.Tests
{
    public class LoudnessAnalysisJsonExtensionsTests
    {
        private static readonly LoudnessAnalysis SampleAnalysis = new(
            IntegratedLufs: -23.5,
            LoudnessRange: 7.2,
            TruePeakDb: -1.0,
            SamplePeakDb: -0.5,
            MomentaryMax: -22.0,
            ShortTermMax: -21.5,
            TotalBlockCount: 123,
            GatedBlockCount: 45);

        [Fact]
        public void ToJson_WithValidValue_ReturnsCamelCaseJson()
        {
            // Act
            string json = SampleAnalysis.ToJson();

            // Assert
            // Property names should be camelCase according to the serializer options
            Assert.Contains("\"integratedLufs\":", json);
            Assert.Contains("\"loudnessRange\":", json);
            Assert.Contains("\"truePeakDb\":", json);
            Assert.Contains("\"samplePeakDb\":", json);
            Assert.Contains("\"momentaryMax\":", json);
            Assert.Contains("\"shortTermMax\":", json);
            Assert.Contains("\"totalBlockCount\":", json);
            Assert.Contains("\"gatedBlockCount\":", json);
        }

        [Fact]
        public void ToJson_WithIndentation_ProducesMultilineJson()
        {
            // Act
            string json = SampleAnalysis.ToJson(indented: true);

            // Assert
            // Indented JSON should contain line breaks (environment newline)
            Assert.Contains(Environment.NewLine, json);
            // Still must be valid JSON
            var deserialized = JsonSerializer.Deserialize<LoudnessAnalysis>(json);
            Assert.Equal(SampleAnalysis, deserialized);
        }

        [Fact]
        public void ToJson_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            LoudnessAnalysis? nullValue = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullValue!.ToJson());
        }

        [Fact]
        public void FromJson_ValidJson_ReturnsCorrectObject()
        {
            // Arrange
            string json = SampleAnalysis.ToJson();

            // Act
            var result = LoudnessAnalysisJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(SampleAnalysis, result);
        }

        [Fact]
        public void FromJson_NullOrEmpty_ReturnsNull()
        {
            // Null input
            Assert.Null(LoudnessAnalysisJsonExtensions.FromJson(null!));

            // Empty string
            Assert.Null(LoudnessAnalysisJsonExtensions.FromJson(string.Empty));
        }

        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrueAndValue()
        {
            // Arrange
            string json = SampleAnalysis.ToJson();

            // Act
            bool success = LoudnessAnalysisJsonExtensions.TryFromJson(json, out var result);

            // Assert
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Equal(SampleAnalysis, result);
        }

        [Fact]
        public void TryFromJson_InvalidOrEmptyJson_ReturnsFalse()
        {
            // Invalid JSON
            bool invalid = LoudnessAnalysisJsonExtensions.TryFromJson("{ invalid json }", out var resultInvalid);
            Assert.False(invalid);
            Assert.Null(resultInvalid);

            // Null input
            bool nullInput = LoudnessAnalysisJsonExtensions.TryFromJson(null!, out var resultNull);
            Assert.False(nullInput);
            Assert.Null(resultNull);

            // Empty string
            bool emptyInput = LoudnessAnalysisJsonExtensions.TryFromJson(string.Empty, out var resultEmpty);
            Assert.False(emptyInput);
            Assert.Null(resultEmpty);
        }
    }
}
