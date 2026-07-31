using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudio.Loudness;

/// <summary>
/// Calculates the Loudness Range (LRA) according to EBU R128 / ITU‑R BS.1770.
/// The algorithm follows the specification:
///   1. Apply an absolute gate (default –70 LUFS).
///   2. From the remaining values, apply a relative gate (max – 20 LU).
///   3. Compute the 10 th and 95 th percentiles of the gated short‑term loudness values.
///   4. LRA = 95 th percentile – 10 th percentile.
/// </summary>
public static class LoudnessRangeCalculator
{
    // Absolute gate threshold in LUFS (values below are discarded)
    private const double AbsoluteGateThreshold = LoudnessConstants.LraAbsoluteGateThreshold;

    // Relative gate offset in LU (values more than 20 LU below the gated maximum are discarded)
    private const double RelativeGateOffset = LoudnessConstants.LraRelativeGateOffset;

    /// <summary>
    /// Computes the loudness range (LRA) from a collection of short‑term loudness values.
    /// </summary>
    /// <param name="shortTermLoudnessValues">
    /// A sequence of short‑term loudness measurements (in LUFS). Typically these are the
    /// short-term values produced by <see cref="LoudnessMeter"/> during analysis.
    /// </param>
    /// <returns>The loudness range in LU (95 th percentile – 10 th percentile) after gating.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shortTermLoudnessValues"/> is <c>null</c>.</exception>
    public static double ComputeLoudnessRange(IEnumerable<double> shortTermLoudnessValues)
    {
        ArgumentNullException.ThrowIfNull(shortTermLoudnessValues);

        // Apply absolute and relative gating as defined by the standard.
        var gatedValues = ApplyGating(shortTermLoudnessValues);
        if (!gatedValues.Any())
            return 0.0; // No values survive gating – LRA is defined as 0.

        // Sort the gated values for percentile calculation.
        var sorted = gatedValues.OrderBy(v => v).ToArray();

        double p10 = Percentile(sorted, 0.10);
        double p95 = Percentile(sorted, 0.95);

        return p95 - p10;
    }

    /// <summary>
    /// Validates the computed LRA against an expected value, using a tolerance.
    /// </summary>
    /// <param name="shortTermLoudnessValues">
    /// The short‑term loudness values to be evaluated.
    /// </param>
    /// <param name="expectedLra">
    /// Expected LRA value (in LU) as defined by the EBU Tech 3342 test case.
    /// </param>
    /// <param name="tolerance">
    /// Acceptable deviation (in LU). Must be non‑negative. Default is <c>0.5</c> LU.
    /// </param>
    /// <returns>
    /// <c>true</c> if the absolute difference between the computed LRA and <paramref name="expectedLra"/>
    /// is less than or equal to <paramref name="tolerance"/>; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shortTermLoudnessValues"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tolerance"/> is negative.</exception>
    public static bool ValidateLra(
        IEnumerable<double> shortTermLoudnessValues,
        double expectedLra,
        double tolerance = 0.5)
    {
        ArgumentNullException.ThrowIfNull(shortTermLoudnessValues);
        if (tolerance < 0.0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be non‑negative.");

        double actual = ComputeLoudnessRange(shortTermLoudnessValues);
        return Math.Abs(actual - expectedLra) <= tolerance;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Applies the absolute and relative gating steps to the supplied loudness values.
    /// </summary>
    /// <param name="values">The raw short‑term loudness values.</param>
    /// <returns>A sequence of values that survive both gating stages.</returns>
    private static IEnumerable<double> ApplyGating(IEnumerable<double> values)
    {
        // Absolute gate: keep only values above the absolute threshold.
        var absoluteGated = values.Where(v => v > AbsoluteGateThreshold).ToArray();
        if (absoluteGated.Length == 0)
            return Enumerable.Empty<double>();

        // Relative gate: keep values that are within RelativeGateOffset LU of the maximum gated value.
        double max = absoluteGated.Max();
        double relativeThreshold = max + RelativeGateOffset; // RelativeGateOffset is negative.
        return absoluteGated.Where(v => v >= relativeThreshold);
    }

    /// <summary>
    /// Returns the p‑th percentile (p expressed as a fraction between 0 and 1) of a sorted array.
    /// Linear interpolation is used between the two nearest ranks, matching the method described
    /// in the EBU R128 specification.
    /// </summary>
    /// <param name="sorted">Array of values sorted in ascending order.</param>
    /// <param name="p">Percentile expressed as a fraction (0 ≤ p ≤ 1).</param>
    /// <returns>The interpolated percentile value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sorted"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sorted"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="p"/> is outside the range [0,1].</exception>
    private static double Percentile(double[] sorted, double p)
    {
        ArgumentNullException.ThrowIfNull(sorted);
        if (sorted.Length == 0)
            throw new ArgumentException("Sequence contains no elements.", nameof(sorted));
        if (p < 0.0 || p > 1.0)
            throw new ArgumentOutOfRangeException(nameof(p), "Percentile must be between 0 and 1.");

        // Position in the sorted array (zero‑based).
        double pos = (sorted.Length - 1) * p;
        int lowerIndex = (int)Math.Floor(pos);
        int upperIndex = (int)Math.Ceiling(pos);

        return lowerIndex == upperIndex
            ? sorted[lowerIndex]
            : sorted[lowerIndex] * (1.0 - (pos - lowerIndex)) + sorted[upperIndex] * (pos - lowerIndex);
    }
}
