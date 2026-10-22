using NAudio.Loudness;
using NAudio.Loudness.Tests;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Integration tests for <see cref="LoudnessMeter"/> that exercise end-to-end loudness measurement
/// with synthesized audio signals, verifying chunk-size independence and correctness per ITU-R BS.1770.
/// </summary>
public class LoudnessMeterIntegrationTests
{
    /// <summary>
    /// Sample rate used for all tests (48 kHz).
    /// </summary>
    private const int Fs = 48000;

    /// <summary>
    /// Duration of the test signal in seconds.
    /// </summary>
    private const double Duration = 10.0;

    /// <summary>
    /// Generates a stereo interleaved float buffer containing a sine wave.
    /// </summary>
    /// <param name="frequency">Sine wave frequency in Hz.</param>
    /// <param name="amplitude">Amplitude in range [-1, 1] (0 dBFS = full scale).</param>
    /// <param name="channels">Number of interleaved channels.</param>
    /// <returns>Interleaved float buffer.</returns>
    private static float[] GenerateSine(double frequency, double amplitude, int channels)
    {
        int frames = (int)(Fs * Duration);
        var buffer = new float[frames * channels];
        double w = 2.0 * Math.PI * frequency / Fs;
        for (int n = 0; n < frames; n++)
        {
            float v = (float)(amplitude * Math.Sin(w * n));
            for (int c = 0; c < channels; c++)
                buffer[n * channels + c] = v;
        }
        return buffer;
    }

    /// <summary>
    /// Tests that integrated loudness is chunk-size independent for a 48kHz stereo 1kHz sine at -20 dBFS.
    /// Expected: approx -20.0 LUFS per ITU-R BS.1770 for a full-scale-referenced sine at -20 dBFS.
    /// </summary>
    [Fact]
    public void IntegratedLufs_ChunkSizeIndependent_1kHzMinus20dBFS_Stereo()
    {
        // Arrange: -20 dFS sine at 1 kHz, stereo
        double amplitude = 0.1; // -20 dBFS = 10^(-20/20) = 0.1
        float[] samples = GenerateSine(1000, amplitude, 2);

        // Act: feed chunks of varying sizes
        var meter100 = new LoudnessMeter(Fs, 2);
        int chunk100 = 100;
        int totalFrames100 = samples.Length / 2;
        int fullChunks100 = totalFrames100 / chunk100;
        for (int i = 0; i < fullChunks100; i++)
        {
            int start = i * chunk100 * 2;
            int count = Math.Min(chunk100, totalFrames100 - start);
            if (count > 0)
                meter100.AddSamples(new ReadOnlySpan<float>(samples, start, count));
        }
        // Feed any remaining samples
        int remainingStart100 = fullChunks100 * chunk100 * 2;
        if (remainingStart100 < samples.Length)
            meter100.AddSamples(new ReadOnlySpan<float>(samples, remainingStart100, samples.Length - remainingStart100));

        double loudness100 = meter100.IntegratedLufs;

        var meter4096 = new LoudnessMeter(Fs, 2);
        int chunk4096 = 4096;
        int totalFrames4096 = samples.Length / 2;
        int fullChunks4096 = totalFrames4096 / chunk4096;
        for (int i = 0; i < fullChunks4096; i++)
        {
            int start = i * chunk4096 * 2;
            int count = Math.Min(chunk4096, totalFrames4096 - start);
            if (count > 0)
                meter4096.AddSamples(new ReadOnlySpan<float>(samples, start, count));
        }
        int remainingStart4096 = fullChunks4096 * chunk4096 * 2;
        if (remainingStart4096 < samples.Length)
            meter4096.AddSamples(new ReadOnlySpan<float>(samples, remainingStart4096, samples.Length - remainingStart4096));

        double loudness4096 = meter4096.IntegratedLufs;

        var meter48000 = new LoudnessMeter(Fs, 2);
        int chunk48000 = 48000;
        int totalFrames48000 = samples.Length / 2;
        int fullChunks48000 = totalFrames48000 / chunk48000;
        for (int i = 0; i < fullChunks48000; i++)
        {
            int start = i * chunk48000 * 2;
            int count = Math.Min(chunk48000, totalFrames48000 - start);
            if (count > 0)
                meter48000.AddSamples(new ReadOnlySpan<float>(samples, start, count));
        }
        int remainingStart48000 = fullChunks48000 * chunk48000 * 2;
        if (remainingStart48000 < samples.Length)
            meter48000.AddSamples(new ReadOnlySpan<float>(samples, remainingStart48000, samples.Length - remainingStart48000));

        double loudness48000 = meter48000.IntegratedLufs;

        // Assert: all chunk sizes should give approximately -20.0 LUFS
        Assert.True(loudness100 >= -20.5 && loudness100 <= -19.5,
            $"Chunk size 100: expected -20.0 +/- 0.5 LUFS, got {loudness100:F3} LUFS");
        Assert.True(loudness4096 >= -20.5 && loudness4096 <= -19.5,
            $"Chunk size 4096: expected -20.0 +/- 0.5 LUFS, got {loudness4096:F3} LUFS");
        Assert.True(loudness48000 >= -20.5 && loudness48000 <= -19.5,
            $"Chunk size 48000: expected -20.0 +/- 0.5 LUFS, got {loudness48000:F3} LUFS");

        // Assert: chunk size should not change the result by more than 0.15 LU
        // (gating boundary effects with small chunks naturally cause ~0.1 LU variation)
        double maxDelta = Math.Max(Math.Abs(loudness100 - loudness4096),
            Math.Max(Math.Abs(loudness100 - loudness48000),
                     Math.Abs(loudness4096 - loudness48000)));
        Assert.True(maxDelta < 0.15,
            $"Max delta between chunk sizes ({maxDelta:F3} LU) exceeds 0.15 LU tolerance");
    }

    /// <summary>
    /// Tests that integrated loudness for a 997 Hz sine at -20 dBFS stereo matches EBU test signal convention.
    /// </summary>
    [Fact]
    public void IntegratedLufs_997HzMinus20dBFS_Stereo_MatchesEbuConvention()
    {
        // Arrange: -20 dBFS sine at 997 Hz, stereo (EBU test signal convention)
        double amplitude = 0.1; // -20 dBFS = 10^(-20/20) = 0.1
        float[] samples = GenerateSine(997, amplitude, 2);

        // Act: feed all samples at once (full block)
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(samples);

        double loudness = meter.IntegratedLufs;

        // Assert: should be approximately -20.0 LUFS
        // 997 Hz is the conventional EBU test frequency; the K-weighting and
        // measurement conventions make the result close to -20.0 LUFS for -20 dBFS reference
        Assert.True(loudness >= -20.5 && loudness <= -19.5,
            $"997 Hz stereo at -20 dBFS: expected -20.0 +/- 0.5 LUFS, got {loudness:F3} LUFS");
    }
}