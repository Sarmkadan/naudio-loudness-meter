using System;
using System.Globalization;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Extension methods for <see cref="TruePeakMeter"/> that provide additional testing utilities.
/// </summary>
public static class TruePeakMeterTestsExtensions
{
    /// <summary>
    /// Creates a <see cref="TruePeakMeter"/> instance with the specified number of channels.
    /// </summary>
    /// <param name="channelCount">The number of audio channels.</param>
    /// <returns>A new <see cref="TruePeakMeter"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when channelCount is less than 1.</exception>
    public static TruePeakMeter CreateMeter(this int channelCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(channelCount, 1);
        return new TruePeakMeter(channelCount);
    }

    /// <summary>
    /// Asserts that the true peak level is within the expected range.
    /// </summary>
    /// <param name="meter">The true peak meter instance.</param>
    /// <param name="minimumDb">The minimum expected true peak in dB.</param>
    /// <param name="maximumDb">The maximum expected true peak in dB.</param>
    /// <exception cref="XunitException">Thrown when the true peak is outside the expected range.</exception>
    public static void AssertTruePeakInRange(this TruePeakMeter meter, double minimumDb, double maximumDb)
    {
        ArgumentNullException.ThrowIfNull(meter);
        Assert.InRange(meter.TruePeakDb, minimumDb, maximumDb);
    }

    /// <summary>
    /// Asserts that the sample peak level is within the expected range.
    /// </summary>
    /// <param name="meter">The true peak meter instance.</param>
    /// <param name="minimumDb">The minimum expected sample peak in dB.</param>
    /// <param name="maximumDb">The maximum expected sample peak in dB.</param>
    /// <exception cref="XunitException">Thrown when the sample peak is outside the expected range.</exception>
    public static void AssertSamplePeakInRange(this TruePeakMeter meter, double minimumDb, double maximumDb)
    {
        ArgumentNullException.ThrowIfNull(meter);
        Assert.InRange(meter.SamplePeakDb, minimumDb, maximumDb);
    }

    /// <summary>
    /// Asserts that the true peak exceeds the sample peak by at least the specified amount in dB.
    /// </summary>
    /// <param name="meter">The true peak meter instance.</param>
    /// <param name="minimumDifferenceDb">The minimum difference in dB between true peak and sample peak.</param>
    /// <exception cref="XunitException">Thrown when the true peak does not exceed the sample peak by the specified amount.</exception>
    public static void AssertTruePeakExceedsSamplePeakBy(this TruePeakMeter meter, double minimumDifferenceDb)
    {
        ArgumentNullException.ThrowIfNull(meter);
        Assert.True(
            meter.TruePeakDb > meter.SamplePeakDb + minimumDifferenceDb,
            $"True peak ({meter.TruePeakDb:0.00} dB) should exceed sample peak ({meter.SamplePeakDb:0.00} dB) by at least {minimumDifferenceDb:0.00} dB");
    }

    /// <summary>
    /// Resets the meter to its initial state.
    /// </summary>
    /// <param name="meter">The true peak meter instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when meter is null.</exception>
    public static void Reset(this TruePeakMeter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        meter.Reset();
    }

    /// <summary>
    /// Gets the current true peak level formatted as a string with invariant culture.
    /// </summary>
    /// <param name="meter">The true peak meter instance.</param>
    /// <returns>A formatted string representation of the true peak level.</returns>
    /// <exception cref="ArgumentNullException">Thrown when meter is null.</exception>
    public static string GetTruePeakString(this TruePeakMeter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        return meter.TruePeakDb.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the current sample peak level formatted as a string with invariant culture.
    /// </summary>
    /// <param name="meter">The true peak meter instance.</param>
    /// <returns>A formatted string representation of the sample peak level.</returns>
    /// <exception cref="ArgumentNullException">Thrown when meter is null.</exception>
    public static string GetSamplePeakString(this TruePeakMeter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        return meter.SamplePeakDb.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the current true peak level in linear scale (1.0 == 0 dBTP).
    /// </summary>
    /// <param name="meter">The true peak meter instance.</param>
    /// <returns>The true peak level in linear scale.</returns>
    /// <exception cref="ArgumentNullException">Thrown when meter is null.</exception>
    public static double GetTruePeakLinear(this TruePeakMeter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        return meter.TruePeakLinear;
    }
}