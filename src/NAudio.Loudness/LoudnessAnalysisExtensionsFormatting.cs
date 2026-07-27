using System;

/// <summary>
/// Extensions for <see cref="LoudnessAnalysis"/>.
/// </summary>
public static class LoudnessAnalysisExtensionsFormatting
{
    /// <summary>
    /// Returns a summary of the analysis in the format required for EBU R128 compliance checks.
    /// </summary>
    /// <param name="analysis">The analysis to summarize.</param>
    /// <returns>A string summarizing the analysis.</returns>
    public static string ToEbuR128Summary(this LoudnessAnalysis analysis)
    {
        return $"Integrated: {analysis.IntegratedLufs:0.0} LUFS, LRA: {analysis.LoudnessRange:0.0} LU, " +
               $"True peak: {analysis.TruePeakDb:0.0} dBTP, Sample peak: {analysis.SamplePeakDb:0.0} dBFS";
    }

    /// <summary>
    /// Checks if the analysis meets the target loudness within a specified tolerance.
    /// </summary>
    /// <param name="analysis">The analysis to check.</param>
    /// <param name="targetLufs">The target loudness in LUFS.</param>
    /// <param name="toleranceLu">The tolerance in LU.</param>
    /// <returns>True if the analysis meets the target loudness within the specified tolerance, false otherwise.</returns>
    public static bool MeetsTarget(this LoudnessAnalysis analysis, double targetLufs, double toleranceLu)
    {
        return Math.Abs(analysis.IntegratedLufs - targetLufs) <= toleranceLu;
    }

    /// <summary>
    /// Returns the gain required to reach the specified target loudness.
    /// </summary>
    /// <param name="analysis">The analysis to check.</param>
    /// <param name="targetLufs">The target loudness in LUFS.</param>
    /// <returns>The gain required to reach the target loudness.</returns>
    public static double GainToReachTarget(this LoudnessAnalysis analysis, double targetLufs)
    {
        return targetLufs - analysis.IntegratedLufs;
    }
}
