using Xunit;
using NAudio.Loudness;
using NAudio.Loudness.Filters;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for verifying the reset/reuse lifecycle of stateful DSP types.
/// </summary>
public class LoudnessMeterLifecycleTests
{
    private const int SampleRate = 48000;
    private const float SignalAmplitude = 0.5f;
    private const float SignalDuration = 0.5f; // seconds
    private const int SignalFrequency = 1000; // Hz

    /// <summary>
    /// Verifies that TruePeakMeter produces identical results after reset and reuse as a fresh instance.
    /// </summary>
    [Fact]
    public void TruePeakMeter_ResetAndReuseMatchesFreshInstance()
    {
        // Arrange
        var signal = SignalGenerator.Sine(SignalFrequency, SignalAmplitude, SampleRate, SignalDuration, 2);
        var meterA = new TruePeakMeter(2);
        var meterB = new TruePeakMeter(2);

        // Contaminate meterA with initial processing
        meterA.AddSamples(signal);

        // Reset meterA to initial state
        meterA.Reset();

        // Process signal with fresh meterB
        meterB.AddSamples(signal);
        var bTruePeakDb = meterB.TruePeakDb;
        var bSamplePeakDb = meterB.SamplePeakDb;
        var bChannelPeaks = meterB.ChannelPeaksDbtp.ToArray();

        // Process signal with reset meterA
        meterA.AddSamples(signal);
        var aTruePeakDb = meterA.TruePeakDb;
        var aSamplePeakDb = meterA.SamplePeakDb;
        var aChannelPeaks = meterA.ChannelPeaksDbtp.ToArray();

        // Assert
        Assert.Equal(bTruePeakDb, aTruePeakDb);
        Assert.Equal(bSamplePeakDb, aSamplePeakDb);
        Assert.Equal(bChannelPeaks.Length, aChannelPeaks.Length);
        for (int i = 0; i < bChannelPeaks.Length; i++)
        {
            Assert.Equal(bChannelPeaks[i], aChannelPeaks[i]);
        }
    }

    /// <summary>
    /// Verifies that LoudnessMeter produces identical results after reset and reuse as a fresh instance.
    /// </summary>
    [Fact]
    public void LoudnessMeter_ResetAndReuseMatchesFreshInstance()
    {
        // Arrange
        var signal = SignalGenerator.Sine(SignalFrequency, SignalAmplitude, SampleRate, SignalDuration, 2);
        var meterA = new LoudnessMeter(SampleRate, 2);
        var meterB = new LoudnessMeter(SampleRate, 2);

        // Contaminate meterA with initial processing
        meterA.AddSamples(signal);

        // Reset meterA to initial state
        meterA.Reset();

        // Process signal with fresh meterB
        meterB.AddSamples(signal);
        var bIntegrated = meterB.IntegratedLufs;
        var bMomentary = meterB.MomentaryLufs;
        var bShortTerm = meterB.ShortTermLufs;
        var bLoudnessRange = meterB.LoudnessRange;

        // Process signal with reset meterA
        meterA.AddSamples(signal);
        var aIntegrated = meterA.IntegratedLufs;
        var aMomentary = meterA.MomentaryLufs;
        var aShortTerm = meterA.ShortTermLufs;
        var aLoudnessRange = meterA.LoudnessRange;

        // Assert
        Assert.Equal(bIntegrated, aIntegrated);
        Assert.Equal(bMomentary, aMomentary);
        Assert.Equal(bShortTerm, aShortTerm);
        Assert.Equal(bLoudnessRange, aLoudnessRange);
    }

    /// <summary>
    /// Verifies that KWeightingFilter produces identical results after reset and reuse as a fresh instance.
    /// </summary>
    [Fact]
    public void KWeightingFilter_ResetAndReuseMatchesFreshInstance()
    {
        // Arrange
        var samples = new float[] { 0.1f, -0.2f, 0.3f, -0.4f, 0.5f };
        var filterA = new KWeightingFilter(SampleRate);
        var filterB = new KWeightingFilter(SampleRate);

        // Contaminate filterA with initial processing
        foreach (var sample in samples)
        {
            filterA.Process(sample);
        }

        // Reset filterA to initial state
        filterA.Reset();

        // Process samples with fresh filterB
        var bOutputs = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            bOutputs[i] = (float)filterB.Process(samples[i]);
        }

        // Process samples with reset filterA
        var aOutputs = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            aOutputs[i] = (float)filterA.Process(samples[i]);
        }

        // Assert
        Assert.Equal(bOutputs.Length, aOutputs.Length);
        for (int i = 0; i < bOutputs.Length; i++)
        {
            Assert.Equal(bOutputs[i], aOutputs[i]);
        }
    }

    /// <summary>
    /// Verifies that Biquad produces identical results after reset and reuse as a fresh instance.
    /// </summary>
    [Fact]
    public void Biquad_ResetAndReuseMatchesFreshInstance()
    {
        // Arrange
        // Using coefficients for a simple low-pass filter for testing
        var biquadA = new Biquad(0.1f, 0.2f, 0.3f, 0.4f, 0.5f);
        var biquadB = new Biquad(0.1f, 0.2f, 0.3f, 0.4f, 0.5f);
        var samples = new float[] { 0.1f, -0.2f, 0.3f, -0.4f, 0.5f, 0.6f, -0.7f, 0.8f };

        // Contaminate biquadA with initial processing
        foreach (var sample in samples)
        {
            biquadA.Process(sample);
        }

        // Reset biquadA to initial state
        biquadA.Reset();

        // Process samples with fresh biquadB
        var bOutputs = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            bOutputs[i] = (float)biquadB.Process(samples[i]);
        }

        // Process samples with reset biquadA
        var aOutputs = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            aOutputs[i] = (float)biquadA.Process(samples[i]);
        }

        // Assert
        Assert.Equal(bOutputs.Length, aOutputs.Length);
        for (int i = 0; i < bOutputs.Length; i++)
        {
            Assert.Equal(bOutputs[i], aOutputs[i]);
        }
    }
}