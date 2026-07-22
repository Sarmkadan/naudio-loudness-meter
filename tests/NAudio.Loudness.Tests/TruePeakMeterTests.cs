using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for the TruePeakMeter class.
/// </summary>
public class TruePeakMeterTests
{
    private const int Fs = 48000;
    private const double ToleranceDb = 0.5;

    /// <summary>
    /// Verifies that a full-scale sine wave reports true peak near 0 dBTP within tolerance.
    /// </summary>
    [Fact]
    public void FullScaleSine_ReportsZeroDbTpWithinTolerance()
    {
        // Arrange
        var meter = new TruePeakMeter(1);

        // Act - Add a full-scale sine wave
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 0.1, 1));

        // Assert - True peak should be near 0 dBTP
        Assert.InRange(meter.TruePeakDb, -ToleranceDb, ToleranceDb);
    }

    /// <summary>
    /// Verifies that a full-scale sine wave in stereo reports true peak near 0 dBTP.
    /// </summary>
    [Fact]
    public void FullScaleStereoSine_ReportsZeroDbTpWithinTolerance()
    {
        // Arrange
        var meter = new TruePeakMeter(2);

        // Act - Add a full-scale stereo sine wave
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 0.1, 2));

        // Assert - True peak should be near 0 dBTP
        Assert.InRange(meter.TruePeakDb, -ToleranceDb, ToleranceDb);
    }

    /// <summary>
    /// Verifies that intersample peaks can exceed sample peaks.
    /// A sine wave at Nyquist/4 (Fs/4) sampled 45 degrees off peak creates intersample peaks.
    /// </summary>
    [Fact]
    public void InterSamplePeak_ExceedsSamplePeak()
    {
        // Arrange
        var meter = new TruePeakMeter(1);
        double freq = Fs / 4.0; // Nyquist/4
        double phaseOffset = Math.PI / 4.0; // 45 degrees off peak

        // Act - Add sine wave that will create intersample peaks
        meter.AddSamples(SignalGenerator.Sine(freq, 1.0, Fs, 0.2, 1, phaseOffset));

        // Assert - True peak should exceed sample peak
        Assert.True(meter.TruePeakDb > meter.SamplePeakDb,
            $"True peak ({meter.TruePeakDb:0.00} dBTP) should exceed sample peak ({meter.SamplePeakDb:0.00} dBFS)");
    }

    /// <summary>
    /// Verifies that intersample peak exceeds sample peak by a significant margin.
    /// Tests the core functionality of oversampling to detect intersample peaks.
    /// </summary>
    [Fact]
    public void InterSamplePeak_ExceedsSamplePeakBySignificantMargin()
    {
        // Arrange
        var meter = new TruePeakMeter(1);
        double freq = Fs / 4.0; // Nyquist/4
        double phaseOffset = Math.PI / 4.0; // 45 degrees off peak

        // Act - Add sine wave that will create intersample peaks
        meter.AddSamples(SignalGenerator.Sine(freq, 1.0, Fs, 0.2, 1, phaseOffset));

        // Assert - True peak should exceed sample peak by at least 1.5 dB
        double differenceDb = meter.TruePeakDb - meter.SamplePeakDb;
        Assert.True(differenceDb >= 1.5,
            $"True peak should exceed sample peak by at least 1.5 dB. Actual difference: {differenceDb:0.00} dB");
    }

    /// <summary>
    /// Verifies that silence reports negative infinity for true peak.
    /// </summary>
    [Fact]
    public void Silence_ReportsNegativeInfinityForTruePeak()
    {
        // Arrange
        var meter = new TruePeakMeter(2);

        // Act - Add silence
        meter.AddSamples(SignalGenerator.Silence(Fs, 0.1, 2));

        // Assert - True peak should be negative infinity for silence
        Assert.Equal(double.NegativeInfinity, meter.TruePeakDb);
    }

    /// <summary>
    /// Verifies that silence reports negative infinity for sample peak.
    /// </summary>
    [Fact]
    public void Silence_ReportsNegativeInfinityForSamplePeak()
    {
        // Arrange
        var meter = new TruePeakMeter(1);

        // Act - Add silence
        meter.AddSamples(SignalGenerator.Silence(Fs, 0.1, 1));

        // Assert - Sample peak should be negative infinity for silence
        Assert.Equal(double.NegativeInfinity, meter.SamplePeakDb);
    }

    /// <summary>
    /// Verifies that silence reports negative infinity for all channel peaks.
    /// </summary>
    [Fact]
    public void Silence_ReportsNegativeInfinityForChannelPeaks()
    {
        // Arrange
        var meter = new TruePeakMeter(3);

        // Act - Add silence
        meter.AddSamples(SignalGenerator.Silence(Fs, 0.1, 3));

        // Assert - All channel peaks should be negative infinity
        var peaks = meter.ChannelPeaksDbtp;
        Assert.Equal(3, peaks.Count);
        foreach (var peak in peaks)
        {
            Assert.Equal(double.NegativeInfinity, peak);
        }
    }

    /// <summary>
    /// Verifies that channel peaks are tracked independently.
    /// </summary>
    [Fact]
    public void ChannelPeaks_TrackIndependently()
    {
        // Arrange
        var meter = new TruePeakMeter(2);
        float[] samples = new float[2000];

        // Left channel: full scale sine
        // Right channel: -6 dB sine
        for (int i = 0; i < 1000; i++)
        {
            samples[i * 2] = (float)Math.Sin(2 * Math.PI * 1000 * i / Fs);
            samples[i * 2 + 1] = (float)(0.5 * Math.Sin(2 * Math.PI * 1000 * i / Fs));
        }

        // Act
        meter.AddSamples(samples);

        // Assert
        var peaks = meter.ChannelPeaksDbtp;
        Assert.Equal(2, peaks.Count);

        // Left channel should be near 0 dBTP
        Assert.InRange(peaks[0], -ToleranceDb, ToleranceDb);

        // Right channel should be near -6 dBTP
        Assert.InRange(peaks[1], -6.5, -5.5);
    }

    /// <summary>
    /// Verifies that Reset() clears all peak measurements.
    /// </summary>
    [Fact]
    public void Reset_ClearsAllPeakMeasurements()
    {
        // Arrange
        var meter = new TruePeakMeter(1);
        meter.AddSamples(SignalGenerator.Sine(1000, 0.8, Fs, 0.1, 1));

        // Pre-condition check
        Assert.NotEqual(double.NegativeInfinity, meter.TruePeakDb);
        Assert.NotEqual(double.NegativeInfinity, meter.SamplePeakDb);

        // Act
        meter.Reset();

        // Assert - After reset, peaks should be negative infinity
        Assert.Equal(double.NegativeInfinity, meter.TruePeakDb);
        Assert.Equal(double.NegativeInfinity, meter.SamplePeakDb);

        var peaks = meter.ChannelPeaksDbtp;
        Assert.All(peaks, p => Assert.Equal(double.NegativeInfinity, p));
    }

    /// <summary>
    /// Verifies that true peak tracks the maximum absolute value across all channels.
    /// </summary>
    [Fact]
    public void TruePeak_TracksMaximumAcrossAllChannels()
    {
        // Arrange
        var meter = new TruePeakMeter(3);
        float[] samples = new float[3000];

        // Channel 0: 0.9 amplitude
        // Channel 1: 0.7 amplitude
        // Channel 2: 0.5 amplitude
        for (int i = 0; i < 1000; i++)
        {
            samples[i * 3] = (float)(0.9 * Math.Sin(2 * Math.PI * 1000 * i / Fs));
            samples[i * 3 + 1] = (float)(0.7 * Math.Sin(2 * Math.PI * 1000 * i / Fs));
            samples[i * 3 + 2] = (float)(0.5 * Math.Sin(2 * Math.PI * 1000 * i / Fs));
        }

        // Act
        meter.AddSamples(samples);

        // Assert - True peak should match the highest channel peak
        var peaks = meter.ChannelPeaksDbtp;
        Assert.Equal(3, peaks.Count);

        // Find the maximum channel peak
        double maxChannelPeak = peaks.Max();
        Assert.Equal(meter.TruePeakDb, maxChannelPeak);
    }

    /// <summary>
    /// Verifies that sample peak tracks the raw maximum sample value.
    /// </summary>
    [Fact]
    public void SamplePeak_TracksRawMaximumValue()
    {
        // Arrange
        var meter = new TruePeakMeter(1);

        // Act - Add samples with known maximum
        float[] samples = new float[1000];
        for (int i = 0; i < 1000; i++)
        {
            // Create a sample that will be the maximum
            samples[i] = 0.85f;
        }
        meter.AddSamples(samples);

        // Assert - Sample peak should be the linear value converted to dB
        Assert.InRange(meter.SamplePeakDb, 20 * Math.Log10(0.84), 20 * Math.Log10(0.86));
    }

    /// <summary>
    /// Verifies that true peak linear value is accessible.
    /// </summary>
    [Fact]
    public void TruePeakLinear_ProvidesAccessToLinearValue()
    {
        // Arrange
        var meter = new TruePeakMeter(1);
        // Create a signal with a clear maximum
        float[] samples = new float[1000];
        for (int i = 0; i < 1000; i++)
        {
            samples[i] = 0.8f;
        }
        meter.AddSamples(samples);

        // Act
        double linearPeak = meter.TruePeakLinear;

        // Assert - True peak may exceed the sample value due to oversampling detection
        // The true peak should be close to 0.8 but may be slightly higher due to intersample peaks
        Assert.InRange(linearPeak, 0.85, 0.95);
    }
}
