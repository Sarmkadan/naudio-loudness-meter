using System;
using System.Collections.Generic;

namespace NAudio.Loudness.Tests;

/// <summary>
/// EBU Tech 3341 Minimum Test Signals for EBU R128 compliance verification.
///
/// These test signals are defined in EBU Tech 3341 and are used to verify
/// that loudness meters comply with EBU R128 specifications.
///
/// Reference: EBU Tech 3341 "Loudness Metering: EBU Mode Meter" specification
/// </summary>
internal static class EbuTech3341TestSignals
{
    /// <summary>
    /// Test signal T1: 1 kHz sine wave at -23 LUFS (EBU R128 target loudness)
    /// Duration: 30 seconds
    /// Expected: Integrated loudness should measure -23 LUFS ±0.1 LU
    /// </summary>
    public static float[] ToneBurst1kHz_23LUFS(int sampleRate, double durationSeconds = 30.0)
    {
        // -23 LUFS: amplitude = 10^(-23 / 20)
        // The LoudnessMeter will add the BS.1770 offset (-0.691 LU) internally
        double amplitude = Math.Pow(10.0, -23.0 / 20.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T2: 1 kHz sine wave at -33 LUFS
    /// Duration: 30 seconds
    /// Expected: Integrated loudness should measure -33 LUFS ±0.1 LU
    /// </summary>
    public static float[] ToneBurst1kHz_33LUFS(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = Math.Pow(10.0, -33.0 / 20.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T3: 1 kHz sine wave at -10 LUFS
    /// Duration: 30 seconds
    /// Expected: Integrated loudness should measure -10 LUFS ±0.1 LU
    /// </summary>
    public static float[] ToneBurst1kHz_10LUFS(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = Math.Pow(10.0, -10.0 / 20.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T4: Alternating tone bursts (EBU Tech 3341 Section 5.2)
    /// Pattern: 1 kHz at -23 LUFS for 1 second, then silence for 1 second
    /// Repeated for 60 seconds
    /// Expected: Integrated loudness should measure approximately -23 LUFS ±0.5 LU
    /// This tests the gating behavior - quiet sections should be excluded from integrated loudness
    /// </summary>
    public static float[] AlternatingToneBursts(int sampleRate, double durationSeconds = 60.0)
    {
        double loudAmplitude = Math.Pow(10.0, -23.0 / 20.0);
        var buffer = new List<float>();
        double remaining = durationSeconds;

        while (remaining > 0)
        {
            // Add 1 second of tone
            buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, Math.Min(1.0, remaining), 1));
            remaining -= 1.0;

            if (remaining <= 0) break;

            // Add 1 second of silence
            buffer.AddRange(SignalGenerator.Silence(sampleRate, Math.Min(1.0, remaining), 1));
            remaining -= 1.0;
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal T5: Tone burst with pauses (EBU Tech 3341 Section 5.3)
    /// Pattern: 1 kHz at -23 LUFS for 0.5 seconds, then silence for 9.5 seconds
    /// Repeated for 60 seconds
    /// Expected: Integrated loudness should measure approximately -33 LUFS ±0.5 LU
    /// This tests the gating threshold - only the loud sections should contribute
    /// </summary>
    public static float[] ToneBurstWithPauses(int sampleRate, double durationSeconds = 60.0)
    {
        double loudAmplitude = Math.Pow(10.0, -23.0 / 20.0);

        var buffer = new List<float>();
        double remaining = durationSeconds;

        while (remaining > 0)
        {
            // Add 0.5 seconds of tone
            buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, Math.Min(0.5, remaining), 1));
            remaining -= 0.5;

            if (remaining <= 0) break;

            // Add 9.5 seconds of silence
            buffer.AddRange(SignalGenerator.Silence(sampleRate, Math.Min(9.5, remaining), 1));
            remaining -= 9.5;
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal T6: Multiple frequency test (EBU Tech 3341 Section 5.4)
    /// Pattern: 100 Hz, 1 kHz, and 10 kHz tones at -23 LUFS, each for 10 seconds
    /// Expected: Integrated loudness should measure -23 LUFS ±0.1 LU
    /// Tests frequency response of the K-weighting filter
    /// </summary>
    public static float[] MultiFrequencyTest(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = Math.Pow(10.0, -23.0 / 20.0);
        var buffer = new List<float>();
        double remaining = durationSeconds;

        // 100 Hz for 10 seconds
        if (remaining > 0)
        {
            buffer.AddRange(SignalGenerator.Sine(100, amplitude, sampleRate, Math.Min(10.0, remaining), 1));
            remaining -= 10.0;
        }

        // 1 kHz for 10 seconds
        if (remaining > 0)
        {
            buffer.AddRange(SignalGenerator.Sine(1000, amplitude, sampleRate, Math.Min(10.0, remaining), 1));
            remaining -= 10.0;
        }

        // 10 kHz for remaining time
        if (remaining > 0)
        {
            buffer.AddRange(SignalGenerator.Sine(10000, amplitude, sampleRate, remaining, 1));
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal T7: Stereo test signal (EBU Tech 3341 Section 5.5)
    /// Pattern: Left channel -23 LUFS, Right channel -23 LUFS, both channels identical
    /// Duration: 30 seconds
    /// Expected: Integrated loudness should measure -23 LUFS ±0.1 LU
    /// Tests stereo handling and channel weighting
    /// </summary>
    public static float[] StereoTestSignal(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = Math.Pow(10.0, -23.0 / 20.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 2);
    }

    /// <summary>
    /// Test signal T8: Silence test (EBU Tech 3341 Section 5.6)
    /// Duration: 30 seconds of pure silence
    /// Expected: Integrated loudness should be -∞ LUFS
    /// Tests that silence is properly detected
    /// </summary>
    public static float[] SilenceTest(int sampleRate, double durationSeconds = 30.0)
    {
        return SignalGenerator.Silence(sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T9: Absolute gate threshold test
    /// Pattern: Tone bursts at various levels to test absolute gate (-70 LUFS)
    /// Sequence: -75 LUFS (below gate), -65 LUFS (above gate), -75 LUFS, -65 LUFS
    /// Each burst: 5 seconds
    /// Expected: Only -65 LUFS sections should contribute to integrated loudness
    /// </summary>
    public static float[] AbsoluteGateThresholdTest(int sampleRate, double durationSeconds = 20.0)
    {
        var buffer = new List<float>();
        double amplitudeBelow = Math.Pow(10.0, -75.0 / 20.0);
        double amplitudeAbove = Math.Pow(10.0, -65.0 / 20.0);
        double remaining = durationSeconds;

        // -75 LUFS (below absolute gate of -70 LUFS)
        buffer.AddRange(SignalGenerator.Sine(1000, amplitudeBelow, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        // -65 LUFS (above absolute gate)
        buffer.AddRange(SignalGenerator.Sine(1000, amplitudeAbove, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        // -75 LUFS (below absolute gate)
        buffer.AddRange(SignalGenerator.Sine(1000, amplitudeBelow, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        // -65 LUFS (above absolute gate)
        buffer.AddRange(SignalGenerator.Sine(1000, amplitudeAbove, sampleRate, remaining, 1));

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal T10: Relative gate threshold test
    /// Pattern: Loud section (-20 LUFS) followed by quiet section (-40 LUFS)
    /// Each section: 10 seconds
    /// Expected: Integrated loudness should be close to -20 LUFS (quiet section excluded by relative gate)
    /// Tests the two-stage gating: absolute gate first, then relative gate
    /// </summary>
    public static float[] RelativeGateThresholdTest(int sampleRate, double durationSeconds = 20.0)
    {
        var buffer = new List<float>();
        double loudAmplitude = Math.Pow(10.0, -20.0 / 20.0);
        double quietAmplitude = Math.Pow(10.0, -40.0 / 20.0);
        double remaining = durationSeconds;

        // Loud section (-20 LUFS)
        buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, Math.Min(10.0, remaining), 1));
        remaining -= 10.0;

        if (remaining <= 0) return buffer.ToArray();

        // Quiet section (-40 LUFS)
        buffer.AddRange(SignalGenerator.Sine(1000, quietAmplitude, sampleRate, remaining, 1));

        return buffer.ToArray();
    }
}