using Xunit;

namespace NAudio.Loudness.Tests;

public class LoudnessMeterTests
{
    private const int Fs = 48000;

    // BS.1770 reference: a full-scale 1 kHz sine reads ~-3.01 LUFS on a single
    // channel (mean square 0.5, K-weighting ~unity at 1 kHz).
    [Fact]
    public void FullScaleSine_Mono_IsMinus3Lufs()
    {
        var meter = new LoudnessMeter(Fs, 1);
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 4, 1));
        Assert.Equal(-3.01, meter.IntegratedLufs, 1);
    }

    // Two coherent channels sum in energy: +3.01 LU over a single channel.
    [Fact]
    public void FullScaleSine_Stereo_IsAboutZeroLufs()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 4, 2));
        Assert.Equal(0.0, meter.IntegratedLufs, 1);
    }

    // Halving amplitude is a clean -6.02 dB / LU shift.
    [Fact]
    public void HalvingAmplitude_DropsSixLu()
    {
        var full = new LoudnessMeter(Fs, 2);
        full.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 4, 2));
        var half = new LoudnessMeter(Fs, 2);
        half.AddSamples(SignalGenerator.Sine(1000, 0.5, Fs, 4, 2));
        Assert.Equal(6.02, full.IntegratedLufs - half.IntegratedLufs, 1);
    }

    // EBU R128 target production loudness.
    [Fact]
    public void SineTunedToMinus23_MeasuresMinus23()
    {
        double amp = Math.Pow(10.0, (-23.0 - 0.0) / 20.0); // stereo reference ~0 LUFS at fs
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, amp, Fs, 5, 2));
        Assert.Equal(-23.0, meter.IntegratedLufs, 0);
    }

    [Fact]
    public void Silence_IntegratedIsNegativeInfinity()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Silence(Fs, 2, 2));
        Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);
    }

    // Absolute gate: a short silent tail must not drag the integrated value down.
    [Fact]
    public void AbsoluteGate_IgnoresSilentSection()
    {
        var meter = new LoudnessMeter(Fs, 2);
        double amp = Math.Pow(10.0, -23.0 / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, amp, Fs, 4, 2));
        double toneOnly = meter.IntegratedLufs;
        meter.AddSamples(SignalGenerator.Silence(Fs, 4, 2));
        // The silent tail is dropped by the -70 LUFS absolute gate; only a few
        // tone/silence boundary blocks survive, so the shift stays well under 1 LU.
        Assert.True(Math.Abs(toneOnly - meter.IntegratedLufs) < 0.5,
            $"tone={toneOnly:0.000} withTail={meter.IntegratedLufs:0.000}");
    }

    [Fact]
    public void MomentaryAndShortTerm_AgreeOnSteadyTone()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, 0.5, Fs, 4, 2));
        Assert.Equal(meter.MomentaryLufs, meter.ShortTermLufs, 1);
    }

    [Fact]
    public void NotEnoughAudio_WindowsReturnNegativeInfinity()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, 0.5, Fs, 0.2, 2)); // < 400 ms
        Assert.Equal(double.NegativeInfinity, meter.MomentaryLufs);
        Assert.Equal(double.NegativeInfinity, meter.ShortTermLufs);
    }

    // The re-derived coefficients must give the same loudness across sample rates.
    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    [InlineData(96000)]
    public void SampleRateIndependent_WithinTolerance(int sampleRate)
    {
        var meter = new LoudnessMeter(sampleRate, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, 0.5, sampleRate, 4, 2));
        Assert.Equal(-6.0, meter.IntegratedLufs, 1);
    }
}
