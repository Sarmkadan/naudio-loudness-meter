using System;

/// <summary>
/// Holds the results of a loudness measurement.
/// </summary>
public sealed record LoudnessAnalysis(
    double IntegratedLufs,
    double LoudnessRange,
    double TruePeakDb,
    double SamplePeakDb,
    double MomentaryMax,
    double ShortTermMax,
    int TotalBlockCount = 0,
    int GatedBlockCount = 0)
{
    /// <summary>
    /// Returns the gain (in dB) required to reach the specified target loudness.
    /// </summary>
    /// <param name="targetLufs">Target integrated loudness in LUFS.</param>
    /// <returns>Gain in dB.</returns>
    public double GainToReach(double targetLufs) => targetLufs - IntegratedLufs;

    /// <summary>
    /// Creates a sentinel analysis representing pure silence (or an input too short to measure).
    /// Integrated loudness is <c>double.NegativeInfinity</c> and loudness range is <c>0</c>.
    /// </summary>
    /// <returns>A <see cref="LoudnessAnalysis"/> instance for silence.</returns>
    public static LoudnessAnalysis CreateSilence() =>
        new(
            IntegratedLufs: double.NegativeInfinity,
            LoudnessRange: 0,
            TruePeakDb: double.NegativeInfinity,
            SamplePeakDb: double.NegativeInfinity,
            MomentaryMax: double.NegativeInfinity,
            ShortTermMax: double.NegativeInfinity,
            TotalBlockCount: 0,
            GatedBlockCount: 0);

    /// <summary>
    /// Returns a human‑readable representation of the analysis.
    /// </summary>
    public override string ToString() =>
        $"Integrated: {IntegratedLufs:0.0} LUFS, LRA: {LoudnessRange:0.0} LU, " +
        $"True peak: {TruePeakDb:0.0} dBTP, Sample peak: {SamplePeakDb:0.0} dBFS, " +
        $"Momentary: {MomentaryMax:0.0} LUFS, Short-term: {ShortTermMax:0.0} LUFS, " +
        $"Total blocks: {TotalBlockCount}, Gated blocks: {GatedBlockCount}";
}
