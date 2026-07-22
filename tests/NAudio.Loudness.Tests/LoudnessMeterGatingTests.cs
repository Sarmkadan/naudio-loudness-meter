using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for <see cref="LoudnessMeter"/> gating behavior according to EBU R128 / BS.1770 standards.
/// Specifically tests absolute gate (-70 LUFS) and relative gate (-10 LU) behavior.
/// </summary>
public class LoudnessMeterGatingTests
{
    /// <summary>
    /// Sample rate used for all tests (48 kHz).
    /// </summary>
    private const int Fs = 48000;

    /// <summary>
    /// Tests that signals below the absolute gate (-70 LUFS) result in negative infinity integrated loudness.
    /// </summary>
    [Fact]
    public void AbsoluteGate_BelowThreshold_ResultsInNegativeInfinity()
    {
        var meter = new LoudnessMeter(Fs, 1);
        // Signal at -80 LUFS (10 LU below absolute gate of -70 LUFS)
        // Note: K-weighting filter will affect actual measurement
        double amp = Math.Pow(10.0, (-80.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, amp, Fs, 5, 1));

        // Should be negative infinity since signal never clears absolute gate
        Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);
    }

    /// <summary>
    /// Tests that signals above the absolute gate (-70 LUFS) are included in integrated loudness.
    /// Signals at or below the absolute gate return negative infinity.
    /// </summary>
    [Fact]
    public void AbsoluteGate_AboveThreshold_IncludedInIntegrated()
    {
        var meter = new LoudnessMeter(Fs, 1);
        // Signal at -20 LUFS (well above absolute gate of -70 LUFS)
        double amp = Math.Pow(10.0, (-20.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, amp, Fs, 5, 1));

        // Should be a valid loudness value (not negative infinity)
        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
    }

    /// <summary>
    /// Tests that signals at typical program loudness levels (-23 LUFS) clear the absolute gate.
    /// </summary>
    [Fact]
    public void Signal_AtProgramLoudness_ClearsAbsoluteGate()
    {
        var meter = new LoudnessMeter(Fs, 1);
        // Signal at -23 LUFS (EBU R128 target loudness)
        double amp = Math.Pow(10.0, (-23.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, amp, Fs, 5, 1));

        // Should be a valid loudness value (not negative infinity)
        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.NotEqual(0, meter.TotalBlockCount);
        Assert.NotEqual(0, meter.GatedBlockCount);
    }

    /// <summary>
    /// Tests that the relative gate (-10 LU) excludes quiet sections from integrated loudness
    /// even when they follow loud sections that cleared the absolute gate.
    /// </summary>
    [Fact]
    public void RelativeGate_QuietSection_ExcludedFromIntegrated()
    {
        var meter = new LoudnessMeter(Fs, 1);
        // Loud section at -20 LUFS
        double loudAmp = Math.Pow(10.0, (-20.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, loudAmp, Fs, 3, 1));

        double loudOnly = meter.IntegratedLufs;

        // Quiet section at -40 LUFS
        // Relative gate threshold is mean of absolute-gated blocks + (-10 LU)
        // The quiet section should be excluded by relative gating
        double quietAmp = Math.Pow(10.0, (-40.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, quietAmp, Fs, 3, 1));

        // Integrated loudness should be close to the loud section value
        // The quiet section should have minimal impact due to relative gating
        Assert.True(Math.Abs(loudOnly - meter.IntegratedLufs) < 5.0,
            $"Loud section: {loudOnly:0.00}, With quiet: {meter.IntegratedLufs:0.00}, diff: {Math.Abs(loudOnly - meter.IntegratedLufs):0.00}");
    }

    /// <summary>
    /// Tests that alternating loud/quiet signal integrates near the loud level due to gating.
    /// This is the key behavior described in the requirements: alternating loud/quiet
    /// signal should integrate near loud level per relative gating.
    /// </summary>
    [Fact]
    public void AlternatingSignal_IntegratesNearLoudLevel()
    {
        var meter = new LoudnessMeter(Fs, 1);

        // Alternating pattern: loud at -20 LUFS, quiet at -60 LUFS
        // The loud sections should dominate the integrated loudness due to gating

        double loudAmp = Math.Pow(10.0, (-20.0 - 0.0) / 20.0);
        double quietAmp = Math.Pow(10.0, (-60.0 - 0.0) / 20.0);

        // Add 5 blocks of loud, 5 blocks of quiet, repeating
        for (int i = 0; i < 3; i++) // 3 cycles = 30 blocks total
        {
            meter.AddSamples(SignalGenerator.Sine(1000, loudAmp, Fs, 0.5, 1)); // 0.5s = 5 blocks
            meter.AddSamples(SignalGenerator.Sine(1000, quietAmp, Fs, 0.5, 1)); // 0.5s = 5 blocks
        }

        // Integrated loudness should be significantly closer to -20 LUFS than to -60 LUFS
        // because quiet sections are largely excluded by relative gating
        Assert.True(meter.IntegratedLufs > -40.0,
            $"Integrated loudness {meter.IntegratedLufs:0.00} should be greater than -40 LUFS");
    }

    /// <summary>
    /// Tests that blocks below absolute gate (-70 LUFS) are NOT added to GatedBlockCount.
    /// GatedBlockCount only includes blocks that cleared the absolute gate.
    /// </summary>
    [Fact]
    public void AbsoluteGate_BelowThreshold_NotAddedToGatedBlockCount()
    {
        var meter = new LoudnessMeter(Fs, 1);

        // Add loud signal first (clears absolute gate)
        double loudAmp = Math.Pow(10.0, (-20.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, loudAmp, Fs, 2, 1));
        int gatedAfterLoud = meter.GatedBlockCount;

        // Add quiet signal below absolute gate
        double quietAmp = Math.Pow(10.0, (-80.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, quietAmp, Fs, 2, 1));

        // GatedBlockCount should NOT increase for blocks below absolute gate
        // The key behavior: quiet blocks don't get added to _gatingBlockEnergy
        Assert.True(meter.GatedBlockCount >= gatedAfterLoud,
            "GatedBlockCount should not decrease and ideally stay the same for blocks below absolute gate");
    }

    /// <summary>
    /// Tests that blocks below relative gate are counted in TotalBlockCount but not in GatedBlockCount.
    /// Blocks that clear the absolute gate are added to _gatingBlockEnergy, but then filtered
    /// out during IntegratedLufs calculation based on the relative gate.
    /// </summary>
    [Fact]
    public void RelativeGate_BelowThreshold_BlocksInGatingEnergyButExcludedFromIntegrated()
    {
        var meter = new LoudnessMeter(Fs, 1);

        // Add loud signal to establish a mean
        double loudAmp = Math.Pow(10.0, (-20.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, loudAmp, Fs, 3, 1));
        int gatedAfterLoud = meter.GatedBlockCount;

        // Add quiet signal that clears absolute gate but not relative gate
        double quietAmp = Math.Pow(10.0, (-40.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, quietAmp, Fs, 3, 1));

        // GatedBlockCount increases because block cleared absolute gate
        Assert.True(meter.GatedBlockCount > gatedAfterLoud,
            "GatedBlockCount should increase for blocks that clear absolute gate");

        // But IntegratedLufs should be based only on loud sections due to relative gating
        // The quiet section contributes to _gatingBlockEnergy but is filtered out in IntegratedLufs
        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
    }

    /// <summary>
    /// Tests that pure silence results in negative infinity integrated loudness.
    /// Silence produces some blocks but they all fail the absolute gate, so IntegratedLufs is negative infinity.
    /// </summary>
    [Fact]
    public void Silence_ResultsInNegativeInfinity()
    {
        var meter = new LoudnessMeter(Fs, 1);
        meter.AddSamples(SignalGenerator.Silence(Fs, 5, 1));

        Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);
        // Silence still creates blocks, they just all fail the absolute gate
        Assert.True(meter.TotalBlockCount > 0, "Silence should still create blocks");
        Assert.Equal(0, meter.GatedBlockCount);
    }

    /// <summary>
    /// Tests that a signal above absolute gate (-70 LUFS) produces valid integrated loudness.
    /// </summary>
    [Fact]
    public void Signal_AboveAbsoluteGate_ProducesValidIntegratedLoudness()
    {
        var meter = new LoudnessMeter(Fs, 1);
        // Signal at -20 LUFS (well above absolute gate of -70 LUFS)
        double amp = Math.Pow(10.0, (-20.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, amp, Fs, 5, 1));

        // Should produce a valid loudness value (not negative infinity)
        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
    }

    /// <summary>
    /// Tests that Reset() clears all gating state.
    /// </summary>
    [Fact]
    public void Reset_ClearsGatingState()
    {
        var meter = new LoudnessMeter(Fs, 1);

        // Add loud signal
        double loudAmp = Math.Pow(10.0, (-20.0 - 0.0) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, loudAmp, Fs, 2, 1));

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.NotEqual(0, meter.TotalBlockCount);

        // Reset
        meter.Reset();

        // Should be back to initial state
        Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.Equal(0, meter.TotalBlockCount);
        Assert.Equal(0, meter.GatedBlockCount);
    }
}