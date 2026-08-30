using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for <see cref="OnePassAdaptiveLoudnessNormalizingSampleProvider"/>.
/// </summary>
public class OnePassAdaptiveNormalizingProviderTests
{
    private const int Fs = 48000;

    [Fact]
    public void Constructor_NullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OnePassAdaptiveLoudnessNormalizingSampleProvider(null!, -23.0));
    }

    [Fact]
    public void Read_ExhaustedSource_ReturnsZero()
    {
        var provider = new OnePassAdaptiveLoudnessNormalizingSampleProvider(
            new ArraySampleProvider(Array.Empty<float>(), Fs, 1),
            targetLufs: -23.0);

        int read = provider.Read(new float[128], 0, 128);

        Assert.Equal(0, read);
    }

    [Fact]
    public void WaveFormat_IsPassedThroughFromSource()
    {
        var source = new ArraySampleProvider(Array.Empty<float>(), Fs, 2);
        var provider = new OnePassAdaptiveLoudnessNormalizingSampleProvider(source, -23.0);

        Assert.Same(source.WaveFormat, provider.WaveFormat);
    }

    [Fact]
    public void SteadySineBelowTarget_OutputConvergesTowardTarget()
    {
        const double targetLufs = -23.0;
        var signal = SignalGenerator.Sine(1000, 0.01, Fs, 10.0, 2);
        var inputMeter = new LoudnessMeter(Fs, 2);
        inputMeter.AddSamples(signal);
        double inputLufs = inputMeter.IntegratedLufs;

        var provider = new OnePassAdaptiveLoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2),
            targetLufs,
            truePeakCeilingDb: null);
        var output = new float[signal.Length];
        int read = provider.Read(output, 0, output.Length);
        var outputMeter = new LoudnessMeter(Fs, 2);
        outputMeter.AddSamples(output.AsSpan(0, read));
        double outputLufs = outputMeter.IntegratedLufs;

        Assert.Equal(signal.Length, read);
        Assert.True(Math.Abs(outputLufs - targetLufs) < Math.Abs(inputLufs - targetLufs));
        Assert.InRange(outputLufs, targetLufs - 3.0, targetLufs + 3.0);
    }

    [Fact]
    public void Read_WithTruePeakCeiling_NoSampleExceedsCeiling()
    {
        const double ceilingDb = -6.0;
        double ceilingLinear = Math.Pow(10.0, ceilingDb / 20.0);
        var signal = SignalGenerator.Sine(1000, 0.8, Fs, 2.0, 2);
        var provider = new OnePassAdaptiveLoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2),
            targetLufs: 0.0,
            truePeakCeilingDb: ceilingDb);
        var buffer = new float[2048];

        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                Assert.True(Math.Abs(buffer[i]) <= ceilingLinear + 1e-6,
                    $"Sample {buffer[i]} exceeds ceiling {ceilingLinear}.");
            }
        }
    }
}
