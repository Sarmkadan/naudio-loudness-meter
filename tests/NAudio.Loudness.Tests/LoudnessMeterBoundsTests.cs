using System;
using NAudio.Wave;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Checks that the streaming meters and the <see cref="SampleProviderLoudnessExtensions"/> entry points
/// reject channel counts and sample rates outside the supported range with a clear exception, before any
/// per-channel buffer is sized from them. The format can come from an untrusted source.
/// </summary>
public class LoudnessMeterBoundsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(LoudnessConstants.MaxStreamChannels + 1)]
    [InlineData(int.MaxValue)]
    public void LoudnessMeter_ChannelCountOutsideRange_ThrowsArgumentOutOfRange(int channels)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LoudnessMeter(48000, channels));
        Assert.Equal("channels", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(LoudnessConstants.MinSampleRate - 1)]
    [InlineData(LoudnessConstants.MaxSampleRate + 1)]
    [InlineData(int.MaxValue)]
    public void LoudnessMeter_SampleRateOutsideRange_ThrowsArgumentOutOfRange(int sampleRate)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LoudnessMeter(sampleRate, 2));
        Assert.Equal("sampleRate", ex.ParamName);
    }

    [Fact]
    public void LoudnessMeter_HugeChannelCountWithCustomWeights_RejectsCountBeforeCheckingWeights()
    {
        // The count is checked first, so a huge value fails fast instead of reaching the weight-length check.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LoudnessMeter(48000, int.MaxValue, new double[1]));
        Assert.Equal("channels", ex.ParamName);
    }

    [Theory]
    [InlineData(LoudnessConstants.MinSampleRate, 1)]
    [InlineData(LoudnessConstants.MaxSampleRate, LoudnessConstants.MaxStreamChannels)]
    public void LoudnessMeter_BoundaryFormats_AreAccepted(int sampleRate, int channels)
    {
        var meter = new LoudnessMeter(sampleRate, channels);

        Assert.Equal(sampleRate, meter.SampleRate);
        meter.AddSamples(new float[channels]);
        Assert.Equal(double.NegativeInfinity, meter.MomentaryLufs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(LoudnessConstants.MaxStreamChannels + 1)]
    [InlineData(int.MaxValue)]
    public void TruePeakMeter_ChannelCountOutsideRange_ThrowsArgumentOutOfRange(int channels)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new TruePeakMeter(channels));
        Assert.Equal("channels", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(LoudnessConstants.MinSampleRate - 1)]
    [InlineData(LoudnessConstants.MaxSampleRate + 1)]
    [InlineData(int.MaxValue)]
    public void TruePeakMeter_SampleRateOutsideRange_ThrowsArgumentOutOfRange(int sampleRate)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new TruePeakMeter(2, sampleRate));
        Assert.Equal("sampleRate", ex.ParamName);
    }

    [Fact]
    public void TruePeakMeter_MaximumChannelCount_IsAccepted()
    {
        var meter = new TruePeakMeter(LoudnessConstants.MaxStreamChannels, LoudnessConstants.MaxSampleRate);

        Assert.Equal(LoudnessConstants.MaxStreamChannels, meter.ChannelPeaksDbtp.Count);
    }

    [Theory]
    [InlineData(LoudnessConstants.MaxStreamChannels + 1, 48000)]
    [InlineData(1000, 48000)]
    [InlineData(2, LoudnessConstants.MinSampleRate - 1)]
    [InlineData(2, LoudnessConstants.MaxSampleRate + 1)]
    public void MeasureLoudness_UnsupportedFormat_ThrowsWithoutReadingSource(int channels, int sampleRate)
    {
        var source = new FormatOnlyProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));

        Assert.Throws<ArgumentOutOfRangeException>(() => source.MeasureLoudness());
        Assert.Equal(0, source.ReadCalls);
    }

    [Fact]
    public void NormalizeLoudnessAdaptive_UnsupportedChannelCount_Throws()
    {
        var source = new FormatOnlyProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, LoudnessConstants.MaxStreamChannels + 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => source.NormalizeLoudnessAdaptive(-23.0));
    }

    [Fact]
    public void NormalizeLoudnessTwoPass_UnsupportedSampleRate_ThrowsBeforeMeasuring()
    {
        var source = new FormatOnlyProvider(WaveFormat.CreateIeeeFloatWaveFormat(LoudnessConstants.MaxSampleRate + 1, 2));

        Func<ISampleProvider> factory = () => source;

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.NormalizeLoudnessTwoPass(-23.0));
        Assert.Equal(0, source.ReadCalls);
    }

    // Reports a fixed format and counts Read calls, so tests can prove the entry point rejected the format
    // before consuming any samples.
    private sealed class FormatOnlyProvider : ISampleProvider
    {
        public FormatOnlyProvider(WaveFormat format) => WaveFormat = format;

        public WaveFormat WaveFormat { get; }

        public int ReadCalls { get; private set; }

        public int Read(float[] buffer, int offset, int count)
        {
            ReadCalls++;
            return 0;
        }
    }
}
