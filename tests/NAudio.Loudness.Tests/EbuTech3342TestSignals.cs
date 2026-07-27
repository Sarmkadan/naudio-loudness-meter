using System;
using System.Collections.Generic;

namespace NAudio.Loudness.Tests;

/// <summary>
/// EBU Tech 3342 Loudness Range (LRA) Test Signals.
///
/// These test signals are designed to validate compliance with EBU Tech 3342 specification
/// for Loudness Range measurement. The specification defines LRA as:
/// LRA = P95 - P10 of short-term loudness values after applying:
/// 1. Absolute gate: -70 LUFS (values below are discarded)
/// 2. Relative gate: max - 10 LU (values more than 10 LU below maximum are discarded)
/// 3. Percentile calculation: 10th and 95th percentiles of the remaining values
///
/// Reference: EBU Tech 3342 "Loudness Range: A measure to supplement loudness
/// normalisation in recommendation EBU R128"
/// </summary>
internal static class EbuTech3342TestSignals
{
    /// <summary>
    /// Test signal L1: Constant loudness at -23 LUFS (EBU R128 target)
    /// Expected LRA: 0 LU (no variation)
    /// This tests the baseline case where all loudness values are identical.
    /// </summary>
    public static float[] ConstantLoudness_23LUFS(int sampleRate, double durationSeconds = 10.0)
    {
        double amplitude = Math.Pow(10.0, -23.0 / 20.0);
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal L2: Wide loudness range signal (-40 LU to -10 LU)
    /// Pattern: Alternating between -40 LUFS and -10 LUFS tones
    /// Expected LRA: ~30 LU (95th percentile at -10 LU, 10th percentile at -40 LU)
    /// Duration: 30 seconds (15 seconds at each level)
    /// This tests the core LRA calculation with extreme loudness variation.
    /// </summary>
    public static float[] WideLoudnessRange(int sampleRate, double durationSeconds = 30.0)
    {
        var buffer = new List<float>();
        double loudAmplitude = Math.Pow(10.0, -10.0 / 20.0);  // -10 LUFS
        double quietAmplitude = Math.Pow(10.0, -40.0 / 20.0); // -40 LUFS
        double remaining = durationSeconds;

        while (remaining > 0)
        {
            // Add 1 second of -10 LUFS tone
            buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, Math.Min(1.0, remaining), 1));
            remaining -= 1.0;

            if (remaining <= 0) break;

            // Add 1 second of -40 LUFS tone
            buffer.AddRange(SignalGenerator.Sine(1000, quietAmplitude, sampleRate, Math.Min(1.0, remaining), 1));
            remaining -= 1.0;
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal L3: Gradual loudness sweep (-30 LUFS to -10 LUFS)
    /// Pattern: Linear sweep from -30 LUFS to -10 LUFS over 30 seconds
    /// Expected LRA: ~20 LU (95th percentile near -10 LU, 10th percentile near -30 LU)
    /// This tests percentile interpolation with continuous variation.
    /// </summary>
    public static float[] LoudnessSweep(int sampleRate, double durationSeconds = 30.0)
    {
        int totalFrames = (int)(sampleRate * durationSeconds);
        var buffer = new float[totalFrames];

        for (int n = 0; n < totalFrames; n++)
        {
            // Linear sweep from -30 LUFS to -10 LUFS
            double loudnessLufs = -30.0 + (20.0 * n / totalFrames);
            double amplitude = Math.Pow(10.0, loudnessLufs / 20.0);
            buffer[n] = (float)amplitude;
        }

        // Convert to stereo
        var stereoBuffer = new float[totalFrames * 2];
        for (int n = 0; n < totalFrames; n++)
        {
            stereoBuffer[n * 2] = buffer[n];     // Left channel
            stereoBuffer[n * 2 + 1] = buffer[n]; // Right channel
        }

        return stereoBuffer;
    }

    /// <summary>
    /// Test signal L4: Three-level loudness (-25, -20, -15 LUFS) with equal duration
    /// Expected LRA: ~10 LU (95th percentile at -15 LU, 10th percentile at -25 LU)
    /// Duration: 30 seconds (10 seconds at each level)
    /// Tests percentile calculation with discrete levels.
    /// </summary>
    public static float[] ThreeLevelLoudness(int sampleRate, double durationSeconds = 30.0)
    {
        var buffer = new List<float>();
        double level1Amplitude = Math.Pow(10.0, -25.0 / 20.0); // -25 LUFS
        double level2Amplitude = Math.Pow(10.0, -20.0 / 20.0); // -20 LUFS
        double level3Amplitude = Math.Pow(10.0, -15.0 / 20.0); // -15 LUFS
        double remaining = durationSeconds;

        while (remaining > 0)
        {
            // Add 10 seconds at -25 LUFS
            buffer.AddRange(SignalGenerator.Sine(1000, level1Amplitude, sampleRate, Math.Min(10.0, remaining), 1));
            remaining -= 10.0;

            if (remaining <= 0) break;

            // Add 10 seconds at -20 LUFS
            buffer.AddRange(SignalGenerator.Sine(1000, level2Amplitude, sampleRate, Math.Min(10.0, remaining), 1));
            remaining -= 10.0;

            if (remaining <= 0) break;

            // Add remaining time at -15 LUFS
            buffer.AddRange(SignalGenerator.Sine(1000, level3Amplitude, sampleRate, remaining, 1));
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal L5: Loudness range test with values around the absolute gate
    /// Pattern: Mix of values above and below the -70 LUFS absolute gate threshold
    /// Sequence: -75 LUFS (excluded), -65 LUFS (included), -75 LUFS (excluded), -60 LUFS (included)
    /// Each section: 5 seconds
    /// Expected LRA: Should be calculated only from the included -65 LUFS and -60 LUFS values
    /// Tests absolute gate filtering.
    /// </summary>
    public static float[] AbsoluteGateTest(int sampleRate, double durationSeconds = 20.0)
    {
        var buffer = new List<float>();
        double belowGateAmplitude = Math.Pow(10.0, -75.0 / 20.0);    // -75 LUFS (below -70 LUFS gate)
        double aboveGateAmplitude1 = Math.Pow(10.0, -65.0 / 20.0);  // -65 LUFS (above gate)
        double aboveGateAmplitude2 = Math.Pow(10.0, -60.0 / 20.0);  // -60 LUFS (above gate)
        double remaining = durationSeconds;

        // -75 LUFS (below absolute gate of -70 LUFS) - should be excluded
        buffer.AddRange(SignalGenerator.Sine(1000, belowGateAmplitude, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        // -65 LUFS (above absolute gate) - should be included
        buffer.AddRange(SignalGenerator.Sine(1000, aboveGateAmplitude1, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        // -75 LUFS (below absolute gate) - should be excluded
        buffer.AddRange(SignalGenerator.Sine(1000, belowGateAmplitude, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        // -60 LUFS (above absolute gate) - should be included
        buffer.AddRange(SignalGenerator.Sine(1000, aboveGateAmplitude2, sampleRate, remaining, 1));

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal L6: Relative gate test
    /// Pattern: Loud section followed by quiet section within the same program
    /// Loud: -20 LUFS, Quiet: -45 LUFS
    /// Each section: 10 seconds
    /// Expected: Relative gate (-20 LU from max) should exclude the -45 LUFS section
    /// Tests the two-stage gating: absolute gate first, then relative gate
    /// </summary>
    public static float[] RelativeGateTest(int sampleRate, double durationSeconds = 20.0)
    {
        var buffer = new List<float>();
        double loudAmplitude = Math.Pow(10.0, -20.0 / 20.0);   // -20 LUFS
        double quietAmplitude = Math.Pow(10.0, -45.0 / 20.0);  // -45 LUFS (more than 10 LU below -20 LUFS)
        double remaining = durationSeconds;

        // Loud section (-20 LUFS) - should pass both gates
        buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, Math.Min(10.0, remaining), 1));
        remaining -= 10.0;

        if (remaining <= 0) return buffer.ToArray();

        // Quiet section (-45 LUFS) - should be excluded by relative gate (max is -20, -45 is 25 LU below max)
        buffer.AddRange(SignalGenerator.Sine(1000, quietAmplitude, sampleRate, remaining, 1));

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal L7: Small block count test (edge case)
    /// Pattern: Only 2 short-term blocks worth of data
    /// Duration: ~0.6 seconds (2 * 300ms blocks at 44.1kHz)
    /// Expected LRA: 0 LU (not enough data for meaningful percentile calculation)
    /// Tests edge case handling for small audio files.
    /// </summary>
    public static float[] SmallBlockCountTest(int sampleRate = 44100)
    {
        // Generate only 2 short-term blocks worth of data (2 * 300ms = 600ms at 44.1kHz)
        int frames = (int)(sampleRate * 0.6);
        var buffer = new float[frames * 2]; // Stereo
        double amplitude = Math.Pow(10.0, -23.0 / 20.0);

        for (int n = 0; n < frames; n++)
        {
            float v = (float)amplitude;
            buffer[n * 2] = v;     // Left channel
            buffer[n * 2 + 1] = v; // Right channel
        }

        return buffer;
    }

    /// <summary>
    /// Test signal L8: All values below absolute gate
    /// Pattern: Pure silence and very quiet tones
    /// All values: -80 LUFS (well below -70 LUFS gate)
    /// Duration: 10 seconds
    /// Expected LRA: 0 LU (no values survive gating)
    /// Tests handling of audio that doesn't meet the minimum loudness requirement.
    /// </summary>
    public static float[] AllBelowGate(int sampleRate, double durationSeconds = 10.0)
    {
        double amplitude = Math.Pow(10.0, -80.0 / 20.0); // -80 LUFS
        return SignalGenerator.Sine(1000, amplitude, sampleRate, durationSeconds, 1);
    }

    /// <summary>
    /// Test signal L9: Perfectly distributed loudness values
    /// Pattern: 10 distinct loudness levels from -35 LUFS to -5 LUFS in 3 LU steps
    /// Each level: 3 seconds
    /// Expected LRA: ~30 LU (95th percentile at -5 LU, 10th percentile at -35 LU)
    /// Tests percentile interpolation with evenly distributed discrete values.
    /// </summary>
    public static float[] PerfectDistribution(int sampleRate, double durationSeconds = 30.0)
    {
        var buffer = new List<float>();
        double[] levels = { -35.0, -32.0, -29.0, -26.0, -23.0, -20.0, -17.0, -14.0, -11.0, -8.0, -5.0 };
        double remaining = durationSeconds;

        foreach (double level in levels)
        {
            double amplitude = Math.Pow(10.0, level / 20.0);
            buffer.AddRange(SignalGenerator.Sine(1000, amplitude, sampleRate, Math.Min(3.0, remaining), 1));
            remaining -= 3.0;

            if (remaining <= 0) break;
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Test signal L10: Real-world like program material
    /// Pattern: Speech-like dynamics with quiet and loud passages
    /// Sequence: -30 LUFS (quiet speech), -20 LUFS (normal speech), -15 LUFS (loud passage)
    /// Each section: 5 seconds
    /// Expected LRA: ~15 LU (range from quietest to loudest included values)
    /// Tests realistic program material with natural loudness variation.
    /// </summary>
    public static float[] SpeechLikeMaterial(int sampleRate, double durationSeconds = 15.0)
    {
        var buffer = new List<float>();
        double quietAmplitude = Math.Pow(10.0, -30.0 / 20.0);  // -30 LUFS
        double normalAmplitude = Math.Pow(10.0, -20.0 / 20.0); // -20 LUFS
        double loudAmplitude = Math.Pow(10.0, -15.0 / 20.0);   // -15 LUFS
        double remaining = durationSeconds;

        // Quiet speech section
        buffer.AddRange(SignalGenerator.Sine(1000, quietAmplitude, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        // Normal speech section
        buffer.AddRange(SignalGenerator.Sine(1000, normalAmplitude, sampleRate, Math.Min(5.0, remaining), 1));
        remaining -= 5.0;

        if (remaining <= 0) return buffer.ToArray();

        // Loud passage section
        buffer.AddRange(SignalGenerator.Sine(1000, loudAmplitude, sampleRate, remaining, 1));

        return buffer.ToArray();
    }
}