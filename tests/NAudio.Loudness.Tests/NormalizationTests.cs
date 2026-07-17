using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for loudness normalization functionality.
/// </summary>
public class NormalizationTests
{
    private const int Fs = 48000;

    /// <summary>
    /// Measures the loudness of a signal, calculates the gain needed to reach -23 LUFS,
    /// normalizes the signal with that gain, and verifies that the resulting integrated
    /// loudness is approximately -23 LUFS.
    /// </summary>
    [Fact]
    public void MeasureThenNormalize_HitsTarget()
    {
        var signal = SignalGenerator.Sine(1000, 0.2, Fs, 6, 2);

        var measured = new ArraySampleProvider(signal, Fs, 2).MeasureLoudness();
        double gain = measured.GainToReach(-23.0);

        var normalized = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2), gain, truePeakCeilingDb: null);

        var after = normalized.MeasureLoudness();
        Assert.Equal(-23.0, after.IntegratedLufs, 1);
    }

    /// <summary>
    /// Verifies that <see cref="LoudnessAnalysis.GainToReach(double)"/> correctly computes the
    /// difference between the target LUFS value and the measured integrated loudness.
    /// </summary>
    [Fact]
    public void GainToReach_ComputesDelta()
    {
        var analysis = new LoudnessAnalysis(-30.0, 5.0, -12.0, -13.0);
        Assert.Equal(7.0, analysis.GainToReach(-23.0), 5);
    }

    /// <summary>
    /// Ensures that the normalizer clamps output samples to the specified true‑peak ceiling.
    /// A full‑scale sine wave is amplified by 12 dB and a ceiling of –1 dB is applied;
    /// the test reads the normalized samples and asserts that none exceed the ceiling.
    /// </summary>
    [Fact]
    public void TruePeakCeiling_ClampsOutput()
    {
        // Loud full-scale source, big positive gain, ceiling at -1 dB.
        var signal = SignalGenerator.Sine(1000, 1.0, Fs, 1, 2);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2), gainDb: 12.0, truePeakCeilingDb: -1.0);

        double ceiling = Math.Pow(10.0, -1.0 / 20.0);
        var buffer = new float[4096];
        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
                Assert.True(Math.Abs(buffer[i]) <= ceiling + 1e-6);
        }
    }
}
