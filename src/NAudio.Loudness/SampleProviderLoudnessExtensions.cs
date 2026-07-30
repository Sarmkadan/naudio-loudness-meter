using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

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
    /// <exception cref="ArgumentException"><paramref name="source"/> has an invalid WaveFormat.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferFrames"/> is not positive.</exception>
    public static LoudnessAnalysis MeasureLoudness(this ISampleProvider source, int bufferFrames = 4096)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.WaveFormat.Channels == 0)
        {
            throw new ArgumentException("Source provider's WaveFormat must have a valid channel count.", nameof(source));
        }
        if (source.WaveFormat.SampleRate <= 0)
        {
            throw new ArgumentException("Source provider's WaveFormat must have a valid sample rate.", nameof(source));
        }
        if (bufferFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferFrames), bufferFrames, "Buffer frames must be positive.");
        }

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
    /// Returns only the integrated loudness (LUFS) of the source. This is a thin wrapper
    /// around <see cref="MeasureLoudness"/> that extracts the <c>IntegratedLufs</c> value.
    /// </summary>
    /// <param name="source">The sample provider to measure.</param>
    /// <param name="bufferFrames">Buffer size in frames per channel. Must be positive.</param>
    /// <returns>The integrated loudness in LUFS.</returns>
    public static double GetIntegratedLoudness(this ISampleProvider source, int bufferFrames = 4096)
    {
        // Re‑use the full measurement logic and just return the integrated value.
        return source.MeasureLoudness(bufferFrames).IntegratedLufs;
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
    public static ISampleProvider NormalizeLoudnessTwoPass(this Func<ISampleProvider> sourceFactory, double targetLufs, double? truePeakCeilingDb = -1.0, bool validateSource = true)
    {
        ArgumentNullException.ThrowIfNull(sourceFactory);

        var source = sourceFactory();
        if (source.WaveFormat.Channels == 0)
        {
            throw new ArgumentException("Source provider's WaveFormat must have a valid channel count.", nameof(sourceFactory));
        }
        if (source.WaveFormat.SampleRate <= 0)
        {
            throw new ArgumentException("Source provider's WaveFormat must have a valid sample rate.", nameof(sourceFactory));
        }

        var analysis = source.MeasureLoudness();

        var source2 = sourceFactory();
        if (source2.WaveFormat.Channels != source.WaveFormat.Channels)
        {
            throw new ArgumentException("Source providers must have the same channel count.", nameof(sourceFactory));
        }
        if (source2.WaveFormat.Encoding != source.WaveFormat.Encoding)
        {
            throw new ArgumentException("Source providers must have the same encoding.", nameof(sourceFactory));
        }

        return new LoudnessNormalizingSampleProvider(source2, analysis.GainToReach(targetLufs), truePeakCeilingDb);
    }

    /// <summary>
    /// Creates a one-pass adaptive normalizer.
    /// </summary>
    /// <param name="source">The source provider to normalize.</param>
    /// <param name="targetLufs">The target integrated loudness in LUFS.</param>
    /// <param name="truePeakCeilingDb">The optional true-peak ceiling in dBTP.</param>
    /// <returns>A normalized <see cref="ISampleProvider"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ISampleProvider NormalizeLoudnessAdaptive(this ISampleProvider source, double targetLufs, double? truePeakCeilingDb = -1.0)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.WaveFormat.Channels == 0)
        {
            throw new ArgumentException("Source provider's WaveFormat must have a valid channel count.", nameof(source));
        }
        if (source.WaveFormat.SampleRate <= 0)
        {
            throw new ArgumentException("Source provider's WaveFormat must have a valid sample rate.", nameof(source));
        }

        return new OnePassAdaptiveLoudnessNormalizingSampleProvider(source, targetLufs, truePeakCeilingDb);
    }

    /// <summary>
    /// Applies a constant gain (in dB) to the source provider.
    /// </summary>
    /// <param name="source">The source provider to which the gain will be applied.</param>
    /// <param name="gainDb">Gain in decibels. Positive values increase level, negative values attenuate.</param>
    /// <returns>A new <see cref="ISampleProvider"/> that outputs the source samples with the specified gain applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ISampleProvider WithGainDb(this ISampleProvider source, double gainDb)
    {
        ArgumentNullException.ThrowIfNull(source);
        // Convert dB gain to linear amplitude factor.
        double linearGain = Math.Pow(10.0, gainDb / 20.0);
        var volumeProvider = new VolumeSampleProvider(source) { Volume = (float)linearGain };
        return volumeProvider;
    }
}
