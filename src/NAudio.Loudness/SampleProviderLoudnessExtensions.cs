using NAudio.Wave;

namespace NAudio.Loudness;

/// <summary>Convenience helpers for measuring and normalizing <see cref="ISampleProvider"/>.</summary>
public static class SampleProviderLoudnessExtensions
{
    /// <summary>
    /// Reads the provider to the end and returns integrated loudness, LRA and
    /// true-peak. The provider is consumed; wrap it if you need it afterwards.
    /// </summary>
    /// <param name="source">The sample provider to measure. Cannot be <see langword="null"/>.</param>
    /// <param name="bufferFrames">Buffer size in frames per channel. Must be positive.</param>
    /// <returns>A <see cref="LoudnessAnalysis"/> containing the measured loudness metrics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferFrames"/> is not positive.</exception>
    public static LoudnessAnalysis MeasureLoudness(this ISampleProvider source, int bufferFrames = 4096)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (bufferFrames <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferFrames), bufferFrames, "Buffer frames must be positive.");

        int channels = source.WaveFormat.Channels;
        var meter = new LoudnessMeter(source.WaveFormat.SampleRate, channels);
        var peak = new TruePeakMeter(channels);

        var buffer = new float[bufferFrames * channels];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            var span = buffer.AsSpan(0, read);
            meter.AddSamples(span);
            peak.AddSamples(span);
        }

        return new LoudnessAnalysis(
            meter.IntegratedLufs,
            meter.LoudnessRange,
            peak.TruePeakDb,
            peak.SamplePeakDb,
            meter.MomentaryLufs,
            meter.ShortTermLufs,
            meter.TotalBlockCount,
            meter.GatedBlockCount);
    }

    /// <summary>
    /// Creates a two-pass normalizer. The provided <paramref name="sourceFactory"/>
    /// is called twice: once to measure, then again to apply the calculated gain.
    /// </summary>
    /// <param name="sourceFactory">A factory to create the source provider (must support rewinding/re-opening).</param>
    /// <param name="targetLufs">The target integrated loudness in LUFS.</param>
    /// <param name="truePeakCeilingDb">The optional true-peak ceiling in dBTP.</param>
    /// <returns>A normalized <see cref="ISampleProvider"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceFactory"/> is <see langword="null"/>.</exception>
    public static ISampleProvider NormalizeLoudnessTwoPass(this Func<ISampleProvider> sourceFactory, double targetLufs, double? truePeakCeilingDb = -1.0)
    {
        ArgumentNullException.ThrowIfNull(sourceFactory);

        var source = sourceFactory();
        var analysis = source.MeasureLoudness();

        var source2 = sourceFactory();
        return new LoudnessNormalizingSampleProvider(source2, analysis.GainToReach(targetLufs), truePeakCeilingDb);
    }

    /// <summary>
    /// Creates a one-pass adaptive normalizer.
    /// </summary>
    /// <param name="source">The source provider.</param>
    /// <param name="targetLufs">The target integrated loudness in LUFS.</param>
    /// <param name="truePeakCeilingDb">The optional true-peak ceiling in dBTP.</param>
    /// <returns>A normalized <see cref="ISampleProvider"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ISampleProvider NormalizeLoudnessAdaptive(this ISampleProvider source, double targetLufs, double? truePeakCeilingDb = -1.0)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new OnePassAdaptiveLoudnessNormalizingSampleProvider(source, targetLufs, truePeakCeilingDb);
    }
}