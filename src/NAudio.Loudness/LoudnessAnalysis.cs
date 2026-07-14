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

    public override string ToString() =>
        $"Integrated: {IntegratedLufs:0.0} LUFS, LRA: {LoudnessRange:0.0} LU, " +
        $"True peak: {TruePeakDb:0.0} dBTP, Sample peak: {SamplePeakDb:0.0} dBFS";
}
