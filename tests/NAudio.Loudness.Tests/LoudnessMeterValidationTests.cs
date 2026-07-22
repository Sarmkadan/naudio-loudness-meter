using System;
using System.Collections.Generic;
using NAudio.Loudness;
using Xunit;

namespace NAudio.Loudness.Tests
{
    public class LoudnessMeterValidationTests
    {
        private static LoudnessMeter CreateMeter(int sampleRate = 48000, int channelCount = 2)
        {
            // LoudnessMeter constructor expects sampleRate and channelCount.
            return new LoudnessMeter(sampleRate, channelCount);
        }

        [Fact]
        public void Validate_ReturnsEmpty_WhenMeterIsValid()
        {
            var meter = CreateMeter(); // 48 kHz, 2 channels – valid values
            IReadOnlyList<string> problems = meter.Validate();

            Assert.Empty(problems);
        }

        [Fact]
        public void IsValid_ReturnsTrue_WhenMeterIsValid()
        {
            var meter = CreateMeter();
            bool isValid = meter.IsValid();

            Assert.True(isValid);
        }

        [Fact]
        public void EnsureValid_DoesNotThrow_WhenMeterIsValid()
        {
            var meter = CreateMeter();
            var exception = Record.Exception(() => meter.EnsureValid());

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_ReturnsProblem_WhenSampleRateIsZero()
        {
            var meter = CreateMeter(sampleRate: 0);
            IReadOnlyList<string> problems = meter.Validate();

            Assert.Single(problems);
            Assert.Contains("SampleRate must be greater than zero", problems[0]);
        }

        [Fact]
        public void IsValid_ReturnsFalse_WhenSampleRateIsZero()
        {
            var meter = CreateMeter(sampleRate: 0);
            bool isValid = meter.IsValid();

            Assert.False(isValid);
        }

        [Fact]
        public void EnsureValid_ThrowsArgumentException_WithProblemMessage_WhenSampleRateIsZero()
        {
            var meter = CreateMeter(sampleRate: 0);
            var ex = Assert.Throws<ArgumentException>(() => meter.EnsureValid());

            Assert.Contains("SampleRate must be greater than zero", ex.Message);
            Assert.Equal(nameof(meter), ex.ParamName);
        }

        [Fact]
        public void Validate_ThrowsArgumentNullException_WhenMeterIsNull()
        {
            LoudnessMeter? meter = null;
            var ex = Assert.Throws<ArgumentNullException>(() => LoudnessMeterValidation.Validate(meter!));
            Assert.Equal("value", ex.ParamName);
        }

        [Fact]
        public void IsValid_ThrowsArgumentNullException_WhenMeterIsNull()
        {
            LoudnessMeter? meter = null;
            var ex = Assert.Throws<ArgumentNullException>(() => LoudnessMeterValidation.IsValid(meter!));
            Assert.Equal("value", ex.ParamName);
        }

        [Fact]
        public void EnsureValid_ThrowsArgumentNullException_WhenMeterIsNull()
        {
            LoudnessMeter? meter = null;
            var ex = Assert.Throws<ArgumentNullException>(() => LoudnessMeterValidation.EnsureValid(meter!));
            Assert.Equal("value", ex.ParamName);
        }
    }
}
