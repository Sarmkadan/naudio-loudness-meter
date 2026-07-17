namespace NAudio.Loudness;

/// <summary>Full-program loudness measurement of a piece of audio.</summary>
public sealed record LoudnessAnalysis(
    double IntegratedLufs,
    double LoudnessRange,
    double TruePeakDb,
    double SamplePeakDb)
{
    /// <summary>Gain in dB required to reach <paramref name="targetLufs"/>.</summary>
    public double GainToReach(double targetLufs) => targetLufs - IntegratedLufs;

    /// <summary>
  /// Returns a string representation of the loudness analysis results.
  /// </summary>
  /// <returns>A formatted string containing integrated loudness, loudness range, true peak, and sample peak values.</returns>
  public override string ToString() =>
        $"Integrated: {IntegratedLufs:0.0} LUFS, LRA: {LoudnessRange:0.0} LU, " +
        $"True peak: {TruePeakDb:0.0} dBTP, Sample peak: {SamplePeakDb:0.0} dBFS";
}
