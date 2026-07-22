using System;
using System.Linq;
using NAudio.Loudness;
using Xunit;

namespace NAudio.Loudness.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="ChannelWeights"/> class that verify channel weight calculations.
    /// </summary>
    public class ChannelWeightsTests
    {
        /// <summary>
        /// Tests that <see cref="ChannelWeights.ForChannelCount"/> returns correct weights for mono (1 channel) audio.
        /// </summary>
        [Fact]
        public void ForChannelCount_Mono_ReturnsCorrectWeights()
        {
            var weights = ChannelWeights.ForChannelCount(1);
            Assert.Equal(new[] { 1.0 }, weights);
        }

        /// <summary>
        /// Tests that <see cref="ChannelWeights.ForChannelCount"/> returns correct weights for stereo (2 channel) audio.
        /// </summary>
        [Fact]
        public void ForChannelCount_Stereo_ReturnsCorrectWeights()
        {
            var weights = ChannelWeights.ForChannelCount(2);
            Assert.Equal(new[] { 1.0, 1.0 }, weights);
        }

        /// <summary>
        /// Tests that <see cref="ChannelWeights.ForChannelCount"/> returns correct weights for 5.1 surround sound (6 channel) audio.
        /// </summary>
        [Fact]
        public void ForChannelCount_5_1_ReturnsCorrectWeights()
        {
            var weights = ChannelWeights.ForChannelCount(6);
            var expected = new[] { 1.0, 1.0, 1.0, 0.0, 1.41, 1.41 };
            Assert.Equal(expected, weights);
        }

        /// <summary>
        /// Tests that <see cref="ChannelWeights.ForChannelCount"/> returns array of ones for unknown channel counts.
        /// </summary>
        [Fact]
        public void ForChannelCount_UnknownCount_ReturnsAllOnes()
        {
            var weights = ChannelWeights.ForChannelCount(4);
            Assert.Equal(new[] { 1.0, 1.0, 1.0, 1.0 }, weights);
        }

        /// <summary>
        /// Tests that <see cref="ChannelWeights.ForChannelCount"/> throws <see cref="ArgumentException"/> for invalid channel counts (0 or negative).
        /// </summary>
        [Fact]
        public void ForChannelCount_InvalidChannelCount_Throws()
        {
            Assert.Throws<ArgumentException>(() => ChannelWeights.ForChannelCount(0));
            Assert.Throws<ArgumentException>(() => ChannelWeights.ForChannelCount(-1));
        }

        /// <summary>
        /// Tests that <see cref="LoudnessMeter"/> constructor accepts custom channel weights without throwing exceptions.
        /// </summary>
        [Fact]
        public void LoudnessMeter_WithCustomWeights_DoesNotThrow()
        {
            var customWeights = new[] { 0.5, 0.5 };
            var meter = new LoudnessMeter(44100, 2, customWeights);
            Assert.NotNull(meter);
        }

        /// <summary>
        /// Tests that <see cref="LoudnessMeter"/> constructor throws <see cref="ArgumentException"/> when channel weights array length doesn't match channel count.
        /// </summary>
        [Fact]
        public void LoudnessMeter_WithMismatchedWeightLength_Throws()
        {
            var customWeights = new[] { 1.0 };
            Assert.Throws<ArgumentException>(() => new LoudnessMeter(44100, 2, customWeights));
        }
    }
}
