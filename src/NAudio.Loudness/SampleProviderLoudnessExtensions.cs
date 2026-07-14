using NAudio.Wave;

namespace NAudio.Loudness;

/// <summary>Convenience helpers for measuring an entire <see cref="ISampleProvider"/>.</summary>
public static class SampleProviderLoudnessExtensions
{
    /// <summary>
    /// Reads the provider to the end and returns integrated loudness, LRA and
    /// true-peak. The provider is consumed; wrap it if you need it afterwards.
    /// </summary>
    public static LoudnessAnalysis MeasureLoudness(this ISampleProvider source, int bufferFrames = 4096)
    {
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
            peak.SamplePeakDb);
    }
}
