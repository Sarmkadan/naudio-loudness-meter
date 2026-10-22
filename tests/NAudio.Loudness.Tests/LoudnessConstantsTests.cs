using System;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Pins the BS.1770-4 / EBU R 128 constant values and checks the validation helpers on <see cref="LoudnessConstants"/>.
/// </summary>
public class LoudnessConstantsTests
{
    [Fact]
    public void Timing_MatchesBs1770Blocks()
    {
        Assert.Equal(0.1, LoudnessConstants.SubBlockSeconds);
        Assert.Equal(4, LoudnessConstants.MomentaryBlocks);
        Assert.Equal(30, LoudnessConstants.ShortTermBlocks);
        Assert.Equal(0.4, LoudnessConstants.MomentaryBlockSeconds);
        Assert.Equal(3.0, LoudnessConstants.ShortTermBlockSeconds);
    }

    [Fact]
    public void GatingOverlap_Is75PercentWithOneSubBlockHop()
    {
        Assert.Equal(0.75, LoudnessConstants.GatingOverlapFactor);
        Assert.Equal(1, LoudnessConstants.GatingHopBlocks);
        Assert.Equal(LoudnessConstants.MomentaryBlocks * (1.0 - LoudnessConstants.GatingOverlapFactor),
            LoudnessConstants.GatingHopBlocks, precision: 12);
    }

    [Fact]
    public void Gates_MatchBs1770AndTech3342()
    {
        Assert.Equal(-70.0, LoudnessConstants.AbsoluteGateLufs);
        Assert.Equal(-10.0, LoudnessConstants.RelativeGateLu);
        Assert.Equal(-70.0, LoudnessConstants.LraAbsoluteGateThreshold);
        Assert.Equal(-20.0, LoudnessConstants.LraRelativeGateOffset);
        Assert.Equal(0.10, LoudnessConstants.LraLowPercentile);
        Assert.Equal(0.95, LoudnessConstants.LraHighPercentile);
    }

    [Fact]
    public void LoudnessOffset_IsMinus0691()
    {
        Assert.Equal(-0.691, LoudnessConstants.LoudnessOffsetLkfs);
    }

    [Fact]
    public void ChannelWeightConstants_MatchBs1770Table4()
    {
        Assert.Equal(1.0, LoudnessConstants.FrontChannelWeight);
        Assert.Equal(1.41, LoudnessConstants.SurroundChannelWeight);
        Assert.Equal(0.0, LoudnessConstants.LfeChannelWeight);
        Assert.Equal(1, LoudnessConstants.MinChannels);
        Assert.Equal(5, LoudnessConstants.MaxChannels);
    }

    [Fact]
    public void KWeightingPrototype_MatchesBs1770Annex1()
    {
        Assert.Equal(1681.974450955533, LoudnessConstants.KShelfFrequencyHz);
        Assert.Equal(3.999843853973347, LoudnessConstants.KShelfGainDb);
        Assert.Equal(0.7071752369554196, LoudnessConstants.KShelfQ);
        Assert.Equal(0.4996667741545416, LoudnessConstants.KShelfVbExponent);
        Assert.Equal(38.13547087602444, LoudnessConstants.KHighPassFrequencyHz);
        Assert.Equal(0.5003270373238773, LoudnessConstants.KHighPassQ);
    }

    [Fact]
    public void SampleRateLimits_AndDefault_AreStable()
    {
        Assert.Equal(8000, LoudnessConstants.MinSampleRate);
        Assert.Equal(384000, LoudnessConstants.MaxSampleRate);
        Assert.Equal(48000, LoudnessConstants.DefaultSampleRate);
    }

    [Fact]
    public void NormalizationDefaults_AreStable()
    {
        Assert.Equal(20.0, LoudnessConstants.DbPerAmplitudeDecade);
        Assert.Equal(-1.0, LoudnessConstants.DefaultTruePeakCeilingDbtp);
        Assert.Equal(1.1, LoudnessConstants.NormalizationSafetyMarginDb);
        Assert.Equal(0.95, LoudnessConstants.ConservativeSamplePeak);
        Assert.Equal(1.0, LoudnessConstants.AdaptiveGainSmoothingSeconds);
    }

    [Theory]
    [InlineData(1, new[] { 1.0 })]
    [InlineData(2, new[] { 1.0, 1.0 })]
    [InlineData(3, new[] { 1.0, 1.0, 1.0 })]
    [InlineData(4, new[] { 1.0, 1.0, 1.0, 1.41 })]
    [InlineData(5, new[] { 1.0, 1.0, 1.0, 1.41, 1.41 })]
    public void ChannelWeights_ReturnsCanonicalWeightsForEachValidCount(int channels, double[] expected)
    {
        Assert.Equal(expected, LoudnessConstants.ChannelWeights(channels));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void ChannelWeights_OutsideOneToFive_ThrowsDomainException(int channels)
    {
        var ex = Assert.Throws<UnsupportedChannelCountException>(() => LoudnessConstants.ChannelWeights(channels));
        Assert.Equal(channels, ex.ActualValue);
        Assert.Equal("channels", ex.ParamName);
    }

    [Fact]
    public void ChannelWeights_ReturnsNewArrayEachCall()
    {
        var first = LoudnessConstants.ChannelWeights(2);
        first[0] = 99.0;

        Assert.Equal(new[] { 1.0, 1.0 }, LoudnessConstants.ChannelWeights(2));
    }

    [Fact]
    public void ForChannelCount_MatchesConstantsForOneTwoAndFiveChannels()
    {
        // ForChannelCount keeps its legacy all-ones fallback for 4 channels, so it is compared for 1, 2 and 5 only.
        foreach (var channels in new[] { 1, 2, 5 })
        {
            Assert.Equal(LoudnessConstants.ChannelWeights(channels), ChannelWeights.ForChannelCount(channels));
        }
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(44100)]
    [InlineData(48000)]
    [InlineData(384000)]
    public void IsSupportedSampleRate_AcceptsRateInsideRange(int sampleRate)
    {
        Assert.True(LoudnessConstants.IsSupportedSampleRate(sampleRate));
        LoudnessConstants.EnsureSupportedSampleRate(sampleRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7999)]
    [InlineData(384001)]
    [InlineData(-48000)]
    public void IsSupportedSampleRate_RejectsRateOutsideRange(int sampleRate)
    {
        Assert.False(LoudnessConstants.IsSupportedSampleRate(sampleRate));

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => LoudnessConstants.EnsureSupportedSampleRate(sampleRate, "rate"));
        Assert.Equal("rate", ex.ParamName);
        Assert.Equal(sampleRate, ex.ActualValue);
    }
}
