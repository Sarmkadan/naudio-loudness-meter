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
    /// Amplitude for a -23 LUFS stereo signal (reference ~0 LUFS at full scale).
    /// </summary>
    private static readonly double AmpMinus23 = Math.Pow(10.0, -23.0 / 20.0);

    /// <summary>
    /// Amplitude for a -50 LUFS stereo signal.
    /// </summary>
    private static readonly double AmpMinus50 = Math.Pow(10.0, -50.0 / 20.0);

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

    /// <summary>
    /// Scenario 1: 5 s of -23 LUFS followed by 5 s of digital silence;
    /// assert integrated loudness stays ~-23 LUFS (silence gated out by absolute gate),
    /// tolerance +/-0.5 LU.
    /// </summary>
    [Fact]
    public void AbsoluteGate_SilenceAfterTone_IntegratedStaysAtToneLevel()
    {
        var meter = new LoudnessMeter(Fs, 2);
        // 5 seconds of -23 LUFS stereo tone
        meter.AddSamples(SignalGenerator.Sine(1000, AmpMinus23, Fs, 5, 2));
        double afterTone = meter.IntegratedLufs;
        // 5 seconds of digital silence
        meter.AddSamples(SignalGenerator.Silence(Fs, 5, 2));
        double withSilence = meter.IntegratedLufs;
        // Silence should be gated out; integrated stays at tone level
        Assert.True(Math.Abs(afterTone - withSilence) < 0.5,
            $"before silence={afterTone:0.000} after silence={withSilence:0.000}");
    }

    /// <summary>
    /// Scenario 2: 10 s at -23 LUFS followed by 10 s at -50 LUFS;
    /// assert the quiet part is excluded by the relative gate
    /// and integrated loudness is within +/-0.5 LU of -23.
    /// </summary>
    [Fact]
    public void RelativeGate_QuietSectionExcluded_IntegratedNearTarget()
    {
        var meter = new LoudnessMeter(Fs, 2);
        // 10 seconds at -23 LUFS
        meter.AddSamples(SignalGenerator.Sine(1000, AmpMinus23, Fs, 10, 2));
        // 10 seconds at -50 LUFS (much quieter)
        meter.AddSamples(SignalGenerator.Sine(1000, AmpMinus50, Fs, 10, 2));
        double result = meter.IntegratedLufs;
        // The quiet part should be excluded by the -10 LU relative gate;
        // integrated should be close to -23 LUFS
        Assert.InRange(result, -23.5, -22.5);
    }

    /// <summary>
    /// Scenario 3: fully silent input;
    /// assert the meter reports NegativeInfinity rather than throwing or returning NaN.
    /// </summary>
    [Fact]
    public void FullySilentInput_ReturnsNegativeInfinity()
    {
        var meter = new LoudnessMeter(Fs, 2);
        meter.AddSamples(SignalGenerator.Silence(Fs, 5, 2));
        Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.Equal(double.NegativeInfinity, meter.MomentaryLufs);
        Assert.Equal(double.NegativeInfinity, meter.ShortTermLufs);
    }

    /// <summary>
    /// Scenario 4: verify momentary (400 ms) and short-term (3 s) windows
    /// report the expected value during the loud segment.
    /// </summary>
    [Fact]
    public void MomentaryAndShortTerm_DuringLoudSegment_ReportExpected()
    {
        var meter = new LoudnessMeter(Fs, 2);
        // 5 seconds of -23 LUFS stereo tone
        meter.AddSamples(SignalGenerator.Sine(1000, AmpMinus23, Fs, 5, 2));
        // During the loud segment, momentary and short-term should agree
        // (within tolerance of 1 LU, since momentary window is 400 ms
        // and short-term is 3 s)
        double momentary = meter.MomentaryLufs;
        double shortTerm = meter.ShortTermLufs;
        Assert.InRange(momentary, -23.5, -22.5);
        Assert.InRange(shortTerm, -23.5, -22.5);
        // Momentary and short-term should be close for a steady tone
        Assert.True(Math.Abs(momentary - shortTerm) < 1.0,
            $"momentary={momentary:0.000} short-term={shortTerm:0.000} differ by >1 LU");
    }
}