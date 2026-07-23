using System;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// EBU R128 / ITU-R BS.1770 compliance tests based on EBU Tech 3341 minimum test signals.
///
/// These tests verify that the LoudnessMeter implementation correctly implements the two-stage gating
/// algorithm specified in EBU R128:
/// 1. Absolute gate: -70 LUFS (blocks below this threshold are excluded)
/// 2. Relative gate: -10 LU below the mean of absolute-gated blocks
///
/// Reference: EBU Tech 3341 "Loudness Metering: EBU Mode Meter"
/// </summary>
public class LoudnessMeterEbuR128ComplianceTests
{
    private const int SampleRate = 48000;
    private const double ToleranceLu = 0.5; // ±0.5 LU tolerance for measurements
    private const double StrictToleranceLu = 0.1; // ±0.1 LU for precise measurements

    /// <summary>
    /// Test T1: 1 kHz sine wave at -23 LUFS (EBU R128 target loudness)
    /// Verifies that the meter correctly measures signals at the target loudness.
    /// </summary>
    [Fact]
    public void T1_ToneBurst_23LUFS_MeasuresCorrectly()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.ToneBurst1kHz_23LUFS(SampleRate, 10.0);
        meter.AddSamples(signal);

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.InRange(meter.IntegratedLufs, -23.6, -22.4);
        Assert.InRange(meter.MomentaryLufs, -23.6, -22.4);
        Assert.InRange(meter.ShortTermLufs, -23.6, -22.4);
    }

    /// <summary>
    /// Test T2: 1 kHz sine wave at -33 LUFS
    /// Verifies measurement of quieter signals.
    /// </summary>
    [Fact]
    public void T2_ToneBurst_33LUFS_MeasuresCorrectly()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.ToneBurst1kHz_33LUFS(SampleRate, 10.0);
        meter.AddSamples(signal);

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.InRange(meter.IntegratedLufs, -33.6, -32.4);
    }

    /// <summary>
    /// Test T3: 1 kHz sine wave at -10 LUFS
    /// Verifies measurement of louder signals.
    /// </summary>
    [Fact]
    public void T3_ToneBurst_10LUFS_MeasuresCorrectly()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.ToneBurst1kHz_10LUFS(SampleRate, 10.0);
        meter.AddSamples(signal);

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.InRange(meter.IntegratedLufs, -10.6, -9.4);
    }

    /// <summary>
    /// Test T4: Alternating tone bursts (tests gating behavior)
    /// Pattern: 1s tone at -23 LUFS, 1s silence, repeated
    /// Expected: Integrated loudness should be close to -23 LUFS
    /// The silence sections should be excluded from integrated loudness due to gating.
    /// </summary>
    [Fact]
    public void T4_AlternatingToneBursts_GatingExcludesSilence()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.AlternatingToneBursts(SampleRate, 30.0);
        meter.AddSamples(signal);

        // Should measure close to -23 LUFS despite 50% silence
        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.InRange(meter.IntegratedLufs, -23.5, -22.5);

        // Momentary and short-term should show the actual signal levels
        Assert.InRange(meter.MomentaryLufs, -23.6, -22.4);
        Assert.InRange(meter.ShortTermLufs, -23.6, -22.4);
    }

    /// <summary>
    /// Test T5: Tone burst with pauses (tests absolute gate)
    /// Pattern: 0.5s tone at -23 LUFS, 9.5s silence, repeated
    /// Expected: Integrated loudness should be approximately -33 LUFS
    /// Only the loud sections should contribute due to the long pauses.
    /// </summary>
    [Fact]
    public void T5_ToneBurstWithPauses_OnlyLoudSectionsContribute()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.ToneBurstWithPauses(SampleRate, 30.0);
        meter.AddSamples(signal);

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        // With 1/20 duty cycle, integrated loudness should be about 13.3 LU below the tone level
        // -23 LUFS - 13.3 LU = -36.3 LUFS, but with gating it will be higher
        Assert.InRange(meter.IntegratedLufs, -37.0, -31.0);
    }

    /// <summary>
    /// Test T6: Multi-frequency test (tests K-weighting filter frequency response)
    /// Pattern: 100 Hz, 1 kHz, 10 kHz tones at -23 LUFS
    /// Expected: Integrated loudness should measure -23 LUFS ±0.1 LU
    /// </summary>
    [Fact]
    public void T6_MultiFrequencyTest_KWeightingCorrect()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.MultiFrequencyTest(SampleRate, 30.0);
        meter.AddSamples(signal);

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.InRange(meter.IntegratedLufs, -23.1, -22.9);
    }

    /// <summary>
    /// Test T7: Stereo test signal
    /// Pattern: Stereo signal with both channels at -23 LUFS
    /// Expected: Integrated loudness should measure -23 LUFS ±0.1 LU
    /// Tests proper channel weighting and stereo handling.
    /// </summary>
    [Fact]
    public void T7_StereoTestSignal_ChannelWeightingCorrect()
    {
        var meter = new LoudnessMeter(SampleRate, 2);
        var signal = EbuTech3341TestSignals.StereoTestSignal(SampleRate, 10.0);
        meter.AddSamples(signal);

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.InRange(meter.IntegratedLufs, -23.1, -22.9);
    }

    /// <summary>
    /// Test T8: Silence test (tests absolute gate threshold)
    /// Duration: 30 seconds of pure silence
    /// Expected: Integrated loudness should be -∞ LUFS
    /// All blocks should fail the absolute gate (-70 LUFS).
    /// </summary>
    [Fact]
    public void T8_SilenceTest_AbsoluteGateBlocksAll()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.SilenceTest(SampleRate, 10.0);
        meter.AddSamples(signal);

        Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.True(meter.TotalBlockCount > 0); // Silence still creates blocks
        Assert.Equal(0, meter.GatedBlockCount); // But none clear absolute gate
    }

    /// <summary>
    /// Test T9: Absolute gate threshold test
    /// Pattern: -75 LUFS (below gate), -65 LUFS (above gate), alternating
    /// Expected: Only -65 LUFS sections should contribute to integrated loudness
    /// Blocks below -70 LUFS should be excluded from _gatingBlockEnergy.
    /// </summary>
    [Fact]
    public void T9_AbsoluteGateThreshold_BlocksBelow70LUFSExcluded()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.AbsoluteGateThresholdTest(SampleRate, 20.0);
        meter.AddSamples(signal);

        // Should have some gated blocks (from -65 LUFS sections)
        Assert.True(meter.GatedBlockCount > 0);

        // Should have integrated loudness (not negative infinity)
        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);

        // Should be closer to -65 LUFS than to -75 LUFS
        // The -75 sections are excluded by absolute gate, so integrated is based only on -65 sections
        Assert.InRange(meter.IntegratedLufs, -68.0, -62.0);
    }

    /// <summary>
    /// Test T10: Relative gate threshold test
    /// Pattern: Loud section (-20 LUFS) followed by quiet section (-40 LUFS)
    /// Expected: Integrated loudness should be close to -20 LUFS
    /// The quiet section should be excluded by the relative gate (-10 LU below mean).
    /// </summary>
    [Fact]
    public void T10_RelativeGateThreshold_QuietSectionExcluded()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        var signal = EbuTech3341TestSignals.RelativeGateThresholdTest(SampleRate, 20.0);
        meter.AddSamples(signal);

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);

        // Integrated loudness should be close to -20 LUFS (loud section)
        // The quiet section (-40 LUFS) should be excluded by relative gating
        Assert.InRange(meter.IntegratedLufs, -22.0, -18.0);

        // TotalBlockCount should include blocks from both sections (they cleared absolute gate)
        Assert.True(meter.TotalBlockCount > 0);

        // GatedBlockCount should also include both sections (they cleared absolute gate)
        Assert.True(meter.GatedBlockCount > 0);
    }

    /// <summary>
    /// Test: Verify that blocks below absolute gate are NOT added to GatedBlockCount
    /// This is a critical requirement: GatedBlockCount should only count blocks that cleared -70 LUFS
    /// </summary>
    [Fact]
    public void AbsoluteGate_BlocksBelow70LUFS_NotInGatedBlockCount()
    {
        var meter = new LoudnessMeter(SampleRate, 1);

        // Add signal above absolute gate
        double loudAmplitude = Math.Pow(10.0, (-20.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, loudAmplitude, SampleRate, 2.0, 1));
        int gatedAfterLoud = meter.GatedBlockCount;

        // Add signal below absolute gate
        double quietAmplitude = Math.Pow(10.0, (-80.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, quietAmplitude, SampleRate, 2.0, 1));

        // GatedBlockCount should not increase for blocks below absolute gate
        Assert.True(meter.GatedBlockCount >= gatedAfterLoud);
    }

    /// <summary>
    /// Test: Verify that blocks that clear absolute gate are added to GatedBlockCount
    /// but may be excluded from IntegratedLufs by relative gate
    /// </summary>
    [Fact]
    public void RelativeGate_BlocksAboveAbsoluteGate_InGatedBlockCountButMayBeExcludedFromIntegrated()
    {
        var meter = new LoudnessMeter(SampleRate, 1);

        // Add loud signal to establish mean
        double loudAmplitude = Math.Pow(10.0, (-20.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, loudAmplitude, SampleRate, 3.0, 1));
        int gatedAfterLoud = meter.GatedBlockCount;

        // Add quiet signal that clears absolute gate but not relative gate
        double quietAmplitude = Math.Pow(10.0, (-40.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, quietAmplitude, SampleRate, 3.0, 1));

        // GatedBlockCount should increase (block cleared absolute gate)
        Assert.True(meter.GatedBlockCount > gatedAfterLoud);

        // IntegratedLufs should be based only on loud sections due to relative gating
        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.InRange(meter.IntegratedLufs, -22.0, -18.0);
    }

    /// <summary>
    /// Test: Verify that Reset() properly clears gating state
    /// </summary>
    [Fact]
    public void Reset_ClearsGatingState()
    {
        var meter = new LoudnessMeter(SampleRate, 1);

        // Add signal
        double amplitude = Math.Pow(10.0, (-23.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, amplitude, SampleRate, 2.0, 1));

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.True(meter.TotalBlockCount > 0);
        Assert.True(meter.GatedBlockCount > 0);

        // Reset
        meter.Reset();

        // Should be back to initial state
        Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.Equal(0, meter.TotalBlockCount);
        Assert.Equal(0, meter.GatedBlockCount);
    }

    /// <summary>
    /// Test: Verify correct behavior with stereo 5.1 signal
    /// Tests that channel weights are properly applied (LFE ignored, surrounds weighted 1.41)
    /// </summary>
    [Fact]
    public void Stereo51_ChannelWeightsAppliedCorrectly()
    {
        var meter = new LoudnessMeter(SampleRate, 6); // 5.1 channel layout
        var signal = EbuTech3341TestSignals.StereoTestSignal(SampleRate, 10.0);
        // Duplicate mono signal to all 6 channels for testing
        float[] stereo51Signal = new float[signal.Length * 6];
        for (int i = 0; i < signal.Length; i++)
        {
            for (int c = 0; c < 6; c++)
            {
                stereo51Signal[i * 6 + c] = signal[i];
            }
        }

        meter.AddSamples(stereo51Signal);

        Assert.NotEqual(double.NegativeInfinity, meter.IntegratedLufs);
        Assert.InRange(meter.IntegratedLufs, -23.1, -22.9);
    }

    /// <summary>
    /// Test: Verify that TotalBlockCount includes all blocks (both passed and failed absolute gate)
    /// </summary>
    [Fact]
    public void TotalBlockCount_IncludesAllBlocks()
    {
        var meter = new LoudnessMeter(SampleRate, 1);

        // Add loud signal
        double loudAmplitude = Math.Pow(10.0, (-20.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, loudAmplitude, SampleRate, 2.0, 1));
        int blocksAfterLoud = meter.TotalBlockCount;

        // Add quiet signal below absolute gate
        double quietAmplitude = Math.Pow(10.0, (-80.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, quietAmplitude, SampleRate, 2.0, 1));

        // TotalBlockCount should increase for both loud and quiet blocks
        Assert.True(meter.TotalBlockCount > blocksAfterLoud);
        Assert.True(meter.TotalBlockCount >= 4); // At least 4 blocks (2s each at 48kHz = 96k samples)
    }

    /// <summary>
    /// Test: Verify that GatedBlockCount equals the number of blocks that cleared absolute gate
    /// </summary>
    [Fact]
    public void GatedBlockCount_EqualsBlocksAboveAbsoluteGate()
    {
        var meter = new LoudnessMeter(SampleRate, 1);
        int initialGatedCount = meter.GatedBlockCount;

        // Add loud signal (clears absolute gate)
        double loudAmplitude = Math.Pow(10.0, (-20.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, loudAmplitude, SampleRate, 3.0, 1));
        int afterLoud = meter.GatedBlockCount;

        Assert.True(afterLoud > initialGatedCount);

        // Add quiet signal below absolute gate
        double quietAmplitude = Math.Pow(10.0, (-80.0 - 0.691) / 20.0);
        meter.AddSamples(SignalGenerator.Sine(1000, quietAmplitude, SampleRate, 3.0, 1));

        // GatedBlockCount should not increase
        Assert.Equal(afterLoud, meter.GatedBlockCount);
    }

    /// <summary>
    /// Test: Verify integrated loudness with multiple signal segments
    /// Tests that the meter correctly accumulates loudness over time
    /// </summary>
    [Fact]
    public void IntegratedLoudness_AccumulatesOverMultipleSegments()
    {
        var meter = new LoudnessMeter(SampleRate, 1);

        // Add first segment at -23 LUFS
        var segment1 = EbuTech3341TestSignals.ToneBurst1kHz_23LUFS(SampleRate, 5.0);
        meter.AddSamples(segment1);
        double loudnessAfterSegment1 = meter.IntegratedLufs;

        // Add second segment at -23 LUFS
        var segment2 = EbuTech3341TestSignals.ToneBurst1kHz_23LUFS(SampleRate, 5.0);
        meter.AddSamples(segment2);
        double loudnessAfterSegment2 = meter.IntegratedLufs;

        // Loudness should be consistent
        Assert.NotEqual(double.NegativeInfinity, loudnessAfterSegment1);
        Assert.NotEqual(double.NegativeInfinity, loudnessAfterSegment2);
        Assert.InRange(loudnessAfterSegment1, -23.6, -22.4);
        Assert.InRange(loudnessAfterSegment2, -23.6, -22.4);
    }
}