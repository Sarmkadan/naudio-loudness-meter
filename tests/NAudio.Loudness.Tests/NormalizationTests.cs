using Xunit;

namespace NAudio.Loudness.Tests;

public class NormalizationTests
{
    private const int Fs = 48000;

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

    [Fact]
    public void GainToReach_ComputesDelta()
    {
        var analysis = new LoudnessAnalysis(-30.0, 5.0, -12.0, -13.0);
        Assert.Equal(7.0, analysis.GainToReach(-23.0), 5);
    }

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
