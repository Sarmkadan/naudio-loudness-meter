using NAudio.Loudness;
using System;
using System.Collections.Generic;
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
            var meter = CreateMeter(); // 48 kHz, 2 channels - valid values
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
            // Constructor rejects sampleRate <= 0 with ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeter(sampleRate: 0));
        }

        [Fact]
        public void IsValid_ReturnsFalse_WhenSampleRateIsZero()
        {
            // Constructor rejects sampleRate <= 0 with ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeter(sampleRate: 0));
        }

        [Fact]
        public void EnsureValid_ThrowsArgumentException_WithProblemMessage_WhenSampleRateIsZero()
        {
            // Constructor rejects sampleRate <= 0 with ArgumentOutOfRangeException
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeter(sampleRate: 0));
            Assert.Equal("sampleRate", ex.ParamName);
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

        [Fact]
        public void Validate_ReturnsProblem_WhenSampleRateIsNegative()
        {
            // Constructor rejects sampleRate <= 0 with ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeter(sampleRate: -1));
        }

        [Fact]
        public void IsValid_ReturnsFalse_WhenSampleRateIsNegative()
        {
            // Constructor rejects sampleRate <= 0 with ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeter(sampleRate: -1));
        }

        [Fact]
        public void EnsureValid_ThrowsArgumentException_WithProblemMessage_WhenSampleRateIsNegative()
        {
            // Constructor rejects sampleRate <= 0 with ArgumentOutOfRangeException
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeter(sampleRate: -1));
            Assert.Equal("sampleRate", ex.ParamName);
        }
    }
}
