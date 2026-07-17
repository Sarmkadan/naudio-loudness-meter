using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Extension methods for <see cref="LoudnessMeterTests"/> that provide convenient
/// assertions and helper methods for testing loudness meter behavior.
/// </summary>
public static class LoudnessMeterTestsExtensions
{
    /// <summary>
    /// Asserts that the loudness meter has measured a specific integrated loudness value
    /// within a given tolerance.
    /// </summary>
    /// <param name="meter">The loudness meter instance.</param>
    /// <param name="expectedLufs">The expected LUFS value.</param>
    /// <param name="tolerance">The maximum allowed difference from expected.</param>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public static void AssertIntegratedLufs(
        this LoudnessMeter meter,
        double expectedLufs,
        double tolerance = 0.01)
    {
        ArgumentNullException.ThrowIfNull(meter);

        double actual = meter.IntegratedLufs;
        if (double.IsNegativeInfinity(actual) && double.IsNegativeInfinity(expectedLufs))
        {
            return; // Both are negative infinity, which is correct for silence
        }

        if (double.IsNegativeInfinity(actual))
        {
            Assert.Equal(expectedLufs, actual);
        }

        Assert.Equal(expectedLufs, actual, tolerance);
    }

    /// <summary>
    /// Asserts that the loudness meter has measured a specific momentary loudness value
    /// within a given tolerance.
    /// </summary>
    /// <param name="meter">The loudness meter instance.</param>
    /// <param name="expectedLufs">The expected LUFS value.</param>
    /// <param name="tolerance">The maximum allowed difference from expected.</param>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public static void AssertMomentaryLufs(
        this LoudnessMeter meter,
        double expectedLufs,
        double tolerance = 0.01)
    {
        ArgumentNullException.ThrowIfNull(meter);

        double actual = meter.MomentaryLufs;
        if (double.IsNegativeInfinity(actual) && double.IsNegativeInfinity(expectedLufs))
        {
            return;
        }

        if (double.IsNegativeInfinity(actual))
        {
            Assert.Equal(expectedLufs, actual);
        }

        Assert.Equal(expectedLufs, actual, tolerance);
    }

    /// <summary>
    /// Asserts that the loudness meter has measured a specific short-term loudness value
    /// within a given tolerance.
    /// </summary>
    /// <param name="meter">The loudness meter instance.</param>
    /// <param name="expectedLufs">The expected LUFS value.</param>
    /// <param name="tolerance">The maximum allowed difference from expected.</param>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public static void AssertShortTermLufs(
        this LoudnessMeter meter,
        double expectedLufs,
        double tolerance = 0.01)
    {
        ArgumentNullException.ThrowIfNull(meter);

        double actual = meter.ShortTermLufs;
        if (double.IsNegativeInfinity(actual) && double.IsNegativeInfinity(expectedLufs))
        {
            return;
        }

        if (double.IsNegativeInfinity(actual))
        {
            Assert.Equal(expectedLufs, actual);
        }

        Assert.Equal(expectedLufs, actual, tolerance);
    }

    /// <summary>
    /// Asserts that two loudness values are approximately equal within a given tolerance.
    /// </summary>
    /// <param name="actual">The actual loudness value.</param>
    /// <param name="expected">The expected loudness value.</param>
    /// <param name="tolerance">The maximum allowed difference from expected.</param>
    /// <exception cref="Xunit.AssertActualExpectedException">Thrown when values differ by more than tolerance.</exception>
    public static void AssertApproximatelyEqual(
        this double actual,
        double expected,
        double tolerance = 0.01,
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
    {
        if (double.IsNegativeInfinity(actual) && double.IsNegativeInfinity(expected))
        {
            return;
        }

        if (double.IsNegativeInfinity(actual) || double.IsNegativeInfinity(expected))
        {
            Assert.Equal(expected, actual);
        }

        Assert.Equal(expected, actual, tolerance);
    }

    /// <summary>
    /// Creates a loudness meter with the specified sample rate and channels,
    /// adds samples to it, and returns the meter for fluent chaining.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <param name="channels">The number of audio channels.</param>
    /// <returns>A new <see cref="LoudnessMeter"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if sampleRate or channels is invalid.</exception>
    public static LoudnessMeter WithSamples(
        this int sampleRate,
        int channels,
        ReadOnlySpan<float> interleavedSamples)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
        }

        if (channels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channel count must be positive.");
        }

        var meter = new LoudnessMeter(sampleRate, channels);
        meter.AddSamples(interleavedSamples);
        return meter;
    }

    /// <summary>
    /// Asserts that the loudness range (LRA) is within expected bounds.
    /// </summary>
    /// <param name="meter">The loudness meter instance.</param>
    /// <param name="expectedMin">The minimum expected LRA value.</param>
    /// <param name="expectedMax">The maximum expected LRA value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public static void AssertLoudnessRange(
        this LoudnessMeter meter,
        double expectedMin,
        double expectedMax)
    {
        ArgumentNullException.ThrowIfNull(meter);

        double actual = meter.LoudnessRange;
        Assert.InRange(actual, expectedMin, expectedMax);
    }

    /// <summary>
    /// Gets the loudness difference between two loudness values in LU.
    /// </summary>
    /// <param name="lufs1">The first loudness value.</param>
    /// <param name="lufs2">The second loudness value.</param>
    /// <returns>The loudness difference in LU.</returns>
    public static double LoudnessDifference(this double lufs1, double lufs2)
    {
        if (double.IsNegativeInfinity(lufs1) || double.IsNegativeInfinity(lufs2))
        {
            return double.NaN;
        }

        return lufs1 - lufs2;
    }

}
