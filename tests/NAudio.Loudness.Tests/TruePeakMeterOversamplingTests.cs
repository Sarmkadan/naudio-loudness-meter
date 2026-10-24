using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for the sample-rate dependent oversampling factor of <see cref="TruePeakMeter"/>.
/// </summary>
public class TruePeakMeterOversamplingTests
{
    private const double ToleranceDb = 0.5;

    /// <summary>
    /// Verifies that the oversampling factor is the smallest integer that reaches the 192 kHz target rate.
    /// </summary>
    [Theory]
    [InlineData(8000, 24)]
    [InlineData(44100, 5)]
    [InlineData(48000, 4)]
    [InlineData(88200, 3)]
    [InlineData(96000, 2)]
    [InlineData(176400, 2)]
    [InlineData(192000, 1)]
    [InlineData(384000, 1)]
    public void GetOversampleFactor_ReachesTruePeakTargetRate(int sampleRate, int expectedFactor)
    {
        // Act
        int factor = TruePeakMeter.GetOversampleFactor(sampleRate);

        // Assert
        Assert.Equal(expectedFactor, factor);
        Assert.True((long)factor * sampleRate >= LoudnessConstants.TruePeakTargetSampleRate);
    }

    /// <summary>
    /// Verifies that a meter constructed for a sample rate reports the matching oversampling factor.
    /// </summary>
    [Theory]
    [InlineData(48000, 4)]
    [InlineData(96000, 2)]
    [InlineData(192000, 1)]
    public void Constructor_SelectsOversampleFactorFromSampleRate(int sampleRate, int expectedFactor)
    {
        // Arrange & Act
        var meter = new TruePeakMeter(2, sampleRate);

        // Assert
        Assert.Equal(expectedFactor, meter.OversampleFactor);
    }

    /// <summary>
    /// Verifies that a full-scale sine reports near 0 dBTP at every supported factor.
    /// </summary>
    [Theory]
    [InlineData(48000)]
    [InlineData(96000)]
    [InlineData(192000)]
    public void FullScaleSine_ReportsZeroDbTpAtEverySampleRate(int sampleRate)
    {
        // Arrange
        var meter = new TruePeakMeter(1, sampleRate);

        // Act
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, sampleRate, 0.2, 1));

        // Assert
        Assert.InRange(meter.TruePeakDb, -ToleranceDb, ToleranceDb);
    }

    /// <summary>
    /// Verifies that an inter-sample peak is detected at rates that use interpolation.
    /// A 0 dBFS sine at Fs/4 with a 45 degree phase offset has a sample peak of -3.01 dBFS but a
    /// continuous-time peak of 0 dBFS; the true-peak estimate must sit above the sample peak.
    /// </summary>
    [Theory]
    [InlineData(48000)]
    [InlineData(96000)]
    public void InterSamplePeak_ExceedsSamplePeakAtSampleRate(int sampleRate)
    {
        // Arrange
        var meter = new TruePeakMeter(1, sampleRate);
        double freq = sampleRate / 4.0;
        double phaseOffset = Math.PI / 4.0;

        // Act
        meter.AddSamples(SignalGenerator.Sine(freq, 1.0, sampleRate, 0.2, 1, phaseOffset));

        // Assert
        Assert.True(meter.TruePeakDb > meter.SamplePeakDb,
            $"True peak ({meter.TruePeakDb:0.00} dBTP) should exceed sample peak ({meter.SamplePeakDb:0.00} dBFS) at {sampleRate} Hz");
    }

    /// <summary>
    /// Verifies that a sample rate outside the supported range is rejected.
    /// </summary>
    [Fact]
    public void GetOversampleFactor_RejectsUnsupportedSampleRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TruePeakMeter.GetOversampleFactor(LoudnessConstants.MaxSampleRate + 1));
    }
}
