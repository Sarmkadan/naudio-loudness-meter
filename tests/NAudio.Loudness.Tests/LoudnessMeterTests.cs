using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Unit tests for <see cref="LoudnessMeter"/> class that verify loudness measurement behavior
/// according to EBU R128 and ITU-R BS.1770 standards.
/// </summary>
public class LoudnessMeterTests
{
    /// <summary>
    /// Sample rate used for all tests (48 kHz).
    /// </summary>
    private const int Fs = 48000;

    /// <summary>
    /// Tests that a full-scale 1 kHz sine wave on a single mono channel measures approximately -3.01 LUFS.
    /// This follows the BS.1770 reference where a full-scale 1 kHz sine reads ~-3.01 LUFS due to
    /// mean square of 0.5 and K-weighting being approximately unity at 1 kHz.
    /// </summary>
    [Fact]
    public void FullScaleSine_Mono_IsMinus3Lufs()
    {
        var meter = new LoudnessMeter(Fs, 1);
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 4, 1));
        Assert.Equal(-3.01, meter.IntegratedLufs, 1);
    }

    /// <summary>
    /// Tests that two coherent channels summing in energy results in approximately 0 LUFS.
    /// Two coherent channels sum in energy: +3.01 LU over a single channel.
    /// </summary>
    [Fact]
    public void FullScaleSine_Stereo_IsAboutZeroLufs()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 4, 2));
        Assert.Equal(0.0, meter.IntegratedLufs, 1);
    }

    /// <summary>
    /// Tests that halving the amplitude of a signal results in a -6.02 LU drop.
    /// Halving amplitude is a clean -6.02 dB / LU shift.
    /// </summary>
    [Fact]
    public void HalvingAmplitude_DropsSixLu()
    {
        var full = new LoudnessMeter(Fs, 2);
        full.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 4, 2));
        var half = new LoudnessMeter(Fs, 2);
        half.AddSamples(SignalGenerator.Sine(1000, 0.5, Fs, 4, 2));
        Assert.Equal(6.02, full.IntegratedLufs - half.IntegratedLufs, 1);
    }

    /// <summary>
    /// Tests that a sine wave tuned to the EBU R128 target production loudness of -23 LUFS
    /// measures exactly -23 LUFS.
    /// </summary>
    [Fact]
    public void SineTunedToMinus23_MeasuresMinus23()
    {
        double amp = Math.Pow(10.0, (-23.0 - 0.0) / 20.0); // stereo reference ~0 LUFS at fs
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, amp, Fs, 5, 2));
        Assert.Equal(-23.0, meter.IntegratedLufs, 0);
    }

    /// <summary>
    /// Tests that silence (no audio signal) results in negative infinity integrated loudness.
    /// </summary>
    [Fact]
    public void Silence_IntegratedIsNegativeInfinity()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Silence(Fs, 2, 2));
        Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);
    }

    /// <summary>
    /// Tests that the absolute gate correctly ignores silent sections, preventing them from
    /// dragging down the integrated loudness value.
    /// Absolute gate: a short silent tail must not drag the integrated value down.
    /// </summary>
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

    /// <summary>
    /// Tests that momentary and short-term loudness measurements agree on a steady tone.
    /// </summary>
    [Fact]
    public void MomentaryAndShortTerm_AgreeOnSteadyTone()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, 0.5, Fs, 4, 2));
        Assert.Equal(meter.MomentaryLufs, meter.ShortTermLufs, 1);
    }

    /// <summary>
    /// Tests that insufficient audio (< 400 ms) results in negative infinity for both
    /// momentary and short-term loudness windows.
    /// </summary>
    [Fact]
    public void NotEnoughAudio_WindowsReturnNegativeInfinity()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Sine(1000, 0.5, Fs, 0.2, 2)); // < 400 ms
        Assert.Equal(double.NegativeInfinity, meter.MomentaryLufs);
        Assert.Equal(double.NegativeInfinity, meter.ShortTermLufs);
    }

    /// <summary>
    /// Tests that loudness measurements are independent of sample rate.
    /// The re-derived coefficients must give the same loudness across sample rates.
    /// </summary>
    /// <param name="sampleRate">The sample rate to test (44100, 48000, or 96000).</param>
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