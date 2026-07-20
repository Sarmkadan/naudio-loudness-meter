using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudio.Loudness;

/// <summary>
/// Calculates the Loudness Range (LRA) according to EBU R128 / ITU‑R BS.1770.
/// The algorithm follows the specification:
///   1. Apply an absolute gate (default –70 LUFS).
///   2. From the remaining values, apply a relative gate (max – 10 LU).
///   3. Compute the 10 th and 95 th percentiles of the gated short‑term loudness values.
///   4. LRA = 95 th percentile – 10 th percentile.
/// </summary>
public static class LoudnessRangeCalculator
{
    // Absolute gate threshold in LUFS (values below are discarded)
    private const double AbsoluteGateThreshold = -70.0;

    // Relative gate offset in LU (values more than 10 LU below the gated maximum are discarded)
    private const double RelativeGateOffset = -10.0;

    /// <summary>
    /// Computes the loudness range (LRA) from a collection of short‑term loudness values.
    /// </summary>
    /// <param name="shortTermLoudnessValues">
    /// A sequence of short‑term loudness measurements (in LUFS). Typically these are the
    /// short‑term values produced by <see cref="LoudnessMeter"/> during analysis.
    /// </param>
    /// <returns>The loudness range in LU (95 th percentile – 10 th percentile) after gating.</returns>
    public static double ComputeLoudnessRange(IEnumerable<double> shortTermLoudnessValues)
    {
        if (shortTermLoudnessValues == null) throw new ArgumentNullException(nameof(shortTermLoudnessValues));

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
    /// Applies the absolute and relative gating steps to the supplied loudness values.
    /// </summary>
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
    private static double Percentile(double[] sorted, double p)
    {
        if (sorted == null) throw new ArgumentNullException(nameof(sorted));
        if (sorted.Length == 0) throw new ArgumentException("Sequence contains no elements.", nameof(sorted));
        if (p < 0.0 || p > 1.0) throw new ArgumentOutOfRangeException(nameof(p), "Percentile must be between 0 and 1.");

        // Position in the sorted array (zero‑based).
        double pos = (sorted.Length - 1) * p;
        int lowerIndex = (int)Math.Floor(pos);
        int upperIndex = (int)Math.Ceiling(pos);

        if (lowerIndex == upperIndex)
            return sorted[lowerIndex];

        double weight = pos - lowerIndex;
        return sorted[lowerIndex] * (1.0 - weight) + sorted[upperIndex] * weight;
    }
}
