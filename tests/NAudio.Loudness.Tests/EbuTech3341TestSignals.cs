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
    /// Computes the peak amplitude of a 1 kHz sine wave that will measure the
    /// given integrated LUFS on a mono channel. For a mono sine the mean-square
    /// is A^2/2 (crest factor 3.01 dB), while for stereo (two identical channels)
    /// the two half-powers sum to A^2 and the crest factor cancels. So mono
    /// needs sqrt(2) more amplitude than stereo for the same LUFS reading.
    /// </summary>
    private static double MonoAmplitudeForLufs(double targetLufs)
    {
        return Math.Pow(10.0, targetLufs / 20.0) * Math.Sqrt(2.0);
    }

    /// <summary>
    /// Test signal T1: 1 kHz sine wave at -23 LUFS (EBU R128 target loudness)
    /// </summary>
    public static float[] ToneBurst1kHz_23LUFS(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = MonoAmplitudeForLufs(-23.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T2: 1 kHz sine wave at -33 LUFS
    /// </summary>
    public static float[] ToneBurst1kHz_33LUFS(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = MonoAmplitudeForLufs(-33.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T3: 1 kHz sine wave at -10 LUFS
    /// </summary>
    public static float[] ToneBurst1kHz_10LUFS(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = MonoAmplitudeForLufs(-10.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T4: Alternating tone bursts (EBU Tech 3341 Section 5.2)
    /// Pattern: 1 kHz at -23 LUFS for 1 second, then silence for 1 second
    /// </summary>
    public static float[] AlternatingToneBursts(int sampleRate, double durationSeconds = 60.0)
    {
        double loudAmplitude = MonoAmplitudeForLufs(-23.0);
        var buffer = new List<float>();
        double remaining = durationSeconds;

        while (remaining > 0)
        {
            buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, Math.Min(1.0, remaining), 1));
            remaining -= 1.0;

            if (remaining <= 0) break;

            buffer.AddRange(SignalGenerator.Silence(sampleRate, Math.Min(1.0, remaining), 1));
            remaining -= 1.0;
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal T5: Tone burst with pauses (EBU Tech 3341 Section 5.3)
    /// Pattern: 1 kHz at -23 LUFS for 0.5 seconds, then silence for 9.5 seconds
    /// Expected: With absolute gating, only the tone blocks contribute.
    /// </summary>
    public static float[] ToneBurstWithPauses(int sampleRate, double durationSeconds = 60.0)
    {
        double loudAmplitude = MonoAmplitudeForLufs(-23.0);

        var buffer = new List<float>();
        double remaining = durationSeconds;

        while (remaining > 0)
        {
            buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, Math.Min(0.5, remaining), 1));
            remaining -= 0.5;

            if (remaining <= 0) break;

            buffer.AddRange(SignalGenerator.Silence(sampleRate, Math.Min(9.5, remaining), 1));
            remaining -= 9.5;
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal T6: K-weighting frequency response test.
    /// Uses a single 1 kHz tone at -23 LUFS (mono) as the reference.
    /// </summary>
    public static float[] MultiFrequencyTest(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = MonoAmplitudeForLufs(-23.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T7: Stereo test signal
    /// Both channels at -23 LUFS, identical 1 kHz sine.
    /// For stereo, crest factor cancels with two channels.
    /// </summary>
    public static float[] StereoTestSignal(int sampleRate, double durationSeconds = 30.0)
    {
        double amplitude = Math.Pow(10.0, -23.0 / 20.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 2);
    }

    /// <summary>
    /// Test signal T8: Silence test
    /// </summary>
    public static float[] SilenceTest(int sampleRate, double durationSeconds = 30.0)
    {
        return SignalGenerator.Silence(sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal T9: Absolute gate threshold test
    /// Sequence: -75 LUFS (below gate), -65 LUFS (above gate), alternating.
    /// </summary>
    public static float[] AbsoluteGateThresholdTest(int sampleRate, double durationSeconds = 20.0)
    {
        var buffer = new List<float>();
        double amplitudeBelow = MonoAmplitudeForLufs(-75.0);
        double amplitudeAbove = MonoAmplitudeForLufs(-65.0);
        double remaining = durationSeconds;

        buffer.AddRange(SignalGenerator.Sine(1000, amplitudeBelow, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        buffer.AddRange(SignalGenerator.Sine(1000, amplitudeAbove, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        buffer.AddRange(SignalGenerator.Sine(1000, amplitudeBelow, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        buffer.AddRange(SignalGenerator.Sine(1000, amplitudeAbove, sampleRate, remaining, 1));

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal T10: Relative gate threshold test
    /// Loud section (-20 LUFS) followed by quiet section (-40 LUFS).
    /// </summary>
    public static float[] RelativeGateThresholdTest(int sampleRate, double durationSeconds = 20.0)
    {
        var buffer = new List<float>();
        double loudAmplitude = MonoAmplitudeForLufs(-20.0);
        double quietAmplitude = MonoAmplitudeForLufs(-40.0);
        double remaining = durationSeconds;

        buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, Math.Min(10.0, remaining), 1));
        remaining -= 10.0;

        if (remaining <= 0) return buffer.ToArray();

        buffer.AddRange(SignalGenerator.Sine(1000, quietAmplitude, sampleRate, remaining, 1));

        return buffer.ToArray();
    }
}
