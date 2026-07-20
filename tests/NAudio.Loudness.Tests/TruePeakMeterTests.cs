using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for the TruePeakMeter class.
/// </summary>
public class TruePeakMeterTests
{
    private const int Fs = 48000;

    /// <summary>
    /// Verifies that the TruePeakMeter correctly measures the true peak of a full-scale sine wave.
    /// </summary>
    [Fact]
    public void FullScaleSine_TruePeakNearZeroDb()
    {
        var meter = new TruePeakMeter(2);
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 1, 2));
        /// <summary>
        /// Asserts that the true peak is near 0 dB.
        /// </summary>
        Assert.InRange(meter.TruePeakDb, -0.5, 0.5);
    }

    /// <summary>
    /// Tests that the TruePeakMeter correctly tracks the sample peak of a sine wave.
    /// </summary>
    [Fact]
    public void SamplePeak_TracksRawMaximum()
    {
        var meter = new TruePeakMeter(1);
        meter.AddSamples(SignalGenerator.Sine(1000, 0.5, Fs, 1, 1));
        /// <summary>
        /// Asserts that the sample peak is -6.02 dB.
        /// </summary>
        Assert.Equal(-6.02, meter.SamplePeakDb, 1);
    }

    /// <summary>
    /// Verifies that the TruePeakMeter correctly measures the true peak of a tone at Nyquist/2 sampled 45 degrees off peak.
    /// </summary>
    [Fact]
    public void InterSamplePeak_ExceedsSamplePeak()
    {
        var meter = new TruePeakMeter(1);
        meter.AddSamples(SignalGenerator.Sine(Fs / 4.0, 1.0, Fs, 1, 1, Math.PI / 4.0));
        /// <summary>
        /// Asserts that the true peak is greater than the sample peak by at least 1.5 dB.
        /// </summary>
        Assert.True(meter.TruePeakDb > meter.SamplePeakDb + 1.5,
            $"true={meter.TruePeakDb:0.00} sample={meter.SamplePeakDb:0.00}");
        /// <summary>
        /// Asserts that the true peak is between -1.0 and 0.5 dB.
        /// </summary>
        Assert.InRange(meter.TruePeakDb, -1.0, 0.5);
    }

    [Fact]
    public void ChannelPeaks_TracksIndependently()
    {
        var meter = new TruePeakMeter(2);
        // Left channel: full scale
        // Right channel: -6 dB
        float[] samples = new float[2000];
        for (int i = 0; i < 1000; i++)
        {
            samples[i * 2] = (float)Math.Sin(2 * Math.PI * 1000 * i / Fs);
            samples[i * 2 + 1] = (float)(0.5 * Math.Sin(2 * Math.PI * 1000 * i / Fs));
        }
        meter.AddSamples(samples);

        var peaks = meter.ChannelPeaksDbtp;
        Assert.Equal(2, peaks.Count);
        Assert.InRange(peaks[0], -0.5, 0.5); // Approx 0 dBTP
        Assert.InRange(peaks[1], -6.5, -5.5); // Approx -6 dBTP
    }

    /// <summary>
    /// Tests that the TruePeakMeter correctly measures the true peak of silence.
    /// </summary>
    [Fact]
    public void Silence_PeaksAreNegativeInfinity()
    {
        var meter = new TruePeakMeter(2);
        meter.AddSamples(SignalGenerator.Silence(Fs, 1, 2));
        /// <summary>
        /// Asserts that the true peak is negative infinity.
        /// </summary>
        Assert.Equal(double.NegativeInfinity, meter.TruePeakDb);
    }
}
