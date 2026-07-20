using System;
using System.Linq;
using NAudio.Loudness;
using Xunit;

namespace NAudio.Loudness.Tests
{
    public class ChannelWeightsTests
    {
        [Fact]
        public void ForChannelCount_Mono_ReturnsCorrectWeights()
        {
            var weights = ChannelWeights.ForChannelCount(1);
            Assert.Equal(new[] { 1.0 }, weights);
        }

        [Fact]
        public void ForChannelCount_Stereo_ReturnsCorrectWeights()
        {
            var weights = ChannelWeights.ForChannelCount(2);
            Assert.Equal(new[] { 1.0, 1.0 }, weights);
        }

        [Fact]
        public void ForChannelCount_5_1_ReturnsCorrectWeights()
        {
            var weights = ChannelWeights.ForChannelCount(6);
            var expected = new[] { 1.0, 1.0, 1.0, 0.0, 1.41, 1.41 };
            Assert.Equal(expected, weights);
        }

        [Fact]
        public void ForChannelCount_UnknownCount_ReturnsAllOnes()
        {
            var weights = ChannelWeights.ForChannelCount(4);
            Assert.Equal(new[] { 1.0, 1.0, 1.0, 1.0 }, weights);
        }

        [Fact]
        public void ForChannelCount_InvalidChannelCount_Throws()
        {
            Assert.Throws<ArgumentException>(() => ChannelWeights.ForChannelCount(0));
            Assert.Throws<ArgumentException>(() => ChannelWeights.ForChannelCount(-1));
        }

        [Fact]
        public void LoudnessMeter_WithCustomWeights_DoesNotThrow()
        {
            var customWeights = new[] { 0.5, 0.5 };
            var meter = new LoudnessMeter(44100, 2, customWeights);
            Assert.NotNull(meter);
        }

        [Fact]
        public void LoudnessMeter_WithMismatchedWeightLength_Throws()
        {
            var customWeights = new[] { 1.0 };
            Assert.Throws<ArgumentException>(() => new LoudnessMeter(44100, 2, customWeights));
        }
    }
}
