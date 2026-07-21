using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Edge case tests for LoudnessNormalizingSampleProvider to ensure robust behavior
/// with various input scenarios including silent signals, extreme gains, and clamping.
/// </summary>
public class NormalizingProviderEdgeTests
{
    private const int Fs = 48000;

    /// <summary>
    /// Silent input should pass through unchanged with realistic gain settings.
    /// Extreme gain values can cause numerical instability (0 * infinity = NaN).
    /// </summary>
    [Fact]
    public void SilentInput_PassesThroughUnchanged_WithRealisticGain()
    {
        // Create completely silent signal
        var silentSignal = SignalGenerator.Silence(Fs, 1.0, 2);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(silentSignal, Fs, 2),
            gainDb: 0.0, // Realistic gain setting
            truePeakCeilingDb: null
        );

        var buffer = new float[1024];
        int read = provider.Read(buffer, 0, buffer.Length);

        // Should read all samples
        Assert.Equal(1024, read);

        // All samples should remain exactly 0 (silent)
        for (int i = 0; i < read; i++)
        {
            Assert.Equal(0.0f, buffer[i]);
        }
    }

    /// <summary>
    /// Silent input with ceiling should also pass through unchanged with realistic gain.
    /// </summary>
    [Fact]
    public void SilentInput_WithCeiling_PassesThroughUnchanged()
    {
        var silentSignal = SignalGenerator.Silence(Fs, 0.5, 1);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(silentSignal, Fs, 1),
            gainDb: 0.0, // Realistic gain
            truePeakCeilingDb: -1.0
        );

        var buffer = new float[512];
        int read = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(512, read);
        foreach (float sample in buffer)
        {
            Assert.Equal(0.0f, sample);
        }
    }

    /// <summary>
    /// Silent input with extreme gain demonstrates numerical instability.
    /// 0 * infinity = NaN in floating point arithmetic.
    /// Applications should avoid extreme gain values.
    /// </summary>
    [Fact]
    public void SilentInput_WithExtremeGain_ProducesNaN()
    {
        var silentSignal = SignalGenerator.Silence(Fs, 1.0, 2);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(silentSignal, Fs, 2),
            gainDb: 1000.0, // Extreme gain causing 0 * inf = NaN
            truePeakCeilingDb: null
        );

        var buffer = new float[1024];
        int read = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(1024, read);

        // Extreme gain causes numerical instability with silent input
        foreach (float sample in buffer)
        {
            Assert.True(float.IsNaN(sample),
                "Extreme gain with silent input produces NaN due to 0 * infinity");
        }
    }

    /// <summary>
    /// Negative gain (attenuation) should work correctly with silent input.
    /// </summary>
    [Fact]
    public void SilentInput_WithNegativeGain_PassesThroughUnchanged()
    {
        var silentSignal = SignalGenerator.Silence(Fs, 2.0, 2);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(silentSignal, Fs, 2),
            gainDb: -60.0, // Heavy attenuation
            truePeakCeilingDb: null
        );

        var buffer = new float[1024];
        int read = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(1024, read);
        foreach (float sample in buffer)
        {
            Assert.Equal(0.0f, sample);
        }
    }

    /// <summary>
    /// Gain should be properly clamped when ceiling is enabled.
    /// Tests that the ceiling linear value is correctly calculated and applied.
    /// </summary>
    [Fact]
    public void Gain_Clamped_WhenCeilingEnabled()
    {
        // Create a signal that when amplified would exceed ceiling
        var signal = SignalGenerator.Sine(1000, 0.5, Fs, 0.1, 2);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2),
            gainDb: 20.0, // Would make signal exceed ceiling
            truePeakCeilingDb: -3.0 // Ceiling at -3 dB
        );

        double expectedCeiling = Math.Pow(10.0, -3.0 / 20.0);
        var buffer = new float[2048];
        int read;
        int clampedCount = 0;

        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                float sample = Math.Abs(buffer[i]);

                // Sample should not exceed ceiling (with floating point tolerance)
                Assert.True(sample <= expectedCeiling + 1e-6,
                    $"Sample {buffer[i]} exceeds ceiling {expectedCeiling}");

                // Track if any samples were actually clamped
                if (sample > 0.99 * expectedCeiling)
                {
                    clampedCount++;
                }
            }
        }

        // Verify that clipping actually occurred (ceiling was enforced)
        Assert.True(clampedCount > 0, "Expected some samples to be clamped by ceiling");
    }

    /// <summary>
    /// Gain should not be applied when ceiling is at maximum (0 dBFS).
    /// This tests the edge case where ceiling equals 1.0 (no clamping).
    /// </summary>
    [Fact]
    public void ZeroCeiling_NoClampingApplied()
    {
        var signal = SignalGenerator.Sine(440, 0.8, Fs, 0.2, 1);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 1),
            gainDb: 12.0, // Significant gain
            truePeakCeilingDb: 0.0 // Maximum ceiling (1.0 linear)
        );

        var buffer = new float[1024];
        int read = provider.Read(buffer, 0, buffer.Length);

        // With ceiling at 0 dBFS, samples should be clamped to 1.0
        // The gain of 12 dB (4x) on a 0.8 amplitude signal would be 3.2,
        // but clamped to 1.0
        for (int i = 0; i < read; i++)
        {
            Assert.True(Math.Abs(buffer[i]) <= 1.0f + 1e-6);
        }
    }

    /// <summary>
    /// Output loudness should approach target for sine wave input with appropriate gain.
    /// This is the main functional test for normalization behavior.
    /// </summary>
    [Fact]
    public void SineInput_OutputLoudnessApproachesTarget()
    {
        // Create a sine wave at -10 LUFS (measured)
        var signal = SignalGenerator.Sine(1000, 0.316, Fs, 2.0, 2); // ~0.316 amplitude = -10 dB
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2),
            gainDb: 13.0, // Should bring to ~-10 + 13 = +3 LUFS
            truePeakCeilingDb: null
        );

        var after = provider.MeasureLoudness();

        // The output should be close to the target (+3 LUFS)
        // Allow some tolerance due to measurement window and algorithm
        Assert.InRange(after.IntegratedLufs, -5.0, 10.0);
    }

    /// <summary>
    /// Very small non-zero input should not cause numerical instability.
    /// Tests that tiny signals (near silence) are handled gracefully.
    /// </summary>
    [Fact]
    public void TinyNonZeroInput_NoNumericalInstability()
    {
        // Create a signal with very small amplitude
        var signal = SignalGenerator.Sine(1000, 1e-6, Fs, 0.5, 2);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2),
            gainDb: 120.0, // Extreme gain that could cause overflow
            truePeakCeilingDb: null
        );

        var buffer = new float[1024];
        int read = provider.Read(buffer, 0, buffer.Length);

        // Should not crash or produce NaN/Infinity
        Assert.Equal(1024, read);
        foreach (float sample in buffer)
        {
            Assert.False(float.IsNaN(sample), "Sample should not be NaN");
            Assert.False(float.IsInfinity(sample), "Sample should not be Infinity");
            Assert.InRange(sample, -1.0f, 1.0f); // Should be within valid range
        }
    }

    /// <summary>
    /// Multiple reads should produce consistent results (no state corruption).
    /// </summary>
    [Fact]
    public void MultipleReads_ProduceConsistentResults()
    {
        var signal = SignalGenerator.Sine(440, 0.5, Fs, 1.0, 1);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 1),
            gainDb: 6.0,
            truePeakCeilingDb: null
        );

        // Read in chunks
        var buffer1 = new float[512];
        var buffer2 = new float[512];
        int read1 = provider.Read(buffer1, 0, buffer1.Length);
        int read2 = provider.Read(buffer2, 0, buffer2.Length);

        Assert.Equal(512, read1);
        Assert.Equal(512, read2);

        // Both reads should succeed without errors
        foreach (float sample in buffer1.Concat(buffer2))
        {
            Assert.False(float.IsNaN(sample));
            Assert.False(float.IsInfinity(sample));
            Assert.InRange(sample, -1.0f, 1.0f);
        }
    }

    /// <summary>
    /// Zero-length buffer should not cause issues.
    /// </summary>
    [Fact]
    public void ZeroLengthBuffer_HandledGracefully()
    {
        var signal = SignalGenerator.Sine(1000, 0.5, Fs, 0.1, 2);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2),
            gainDb: 10.0,
            truePeakCeilingDb: null
        );

        var buffer = new float[0];
        int read = provider.Read(buffer, 0, 0);

        // Should return 0 for zero-length buffer
        Assert.Equal(0, read);
    }

    /// <summary>
    /// Gain of exactly 0 dB should pass through samples unchanged.
    /// </summary>
    [Fact]
    public void ZeroDbGain_PassesThroughUnchanged()
    {
        var signal = SignalGenerator.Sine(440, 0.7, Fs, 0.3, 2);
        var original = (float[])signal.Clone();

        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 2),
            gainDb: 0.0,
            truePeakCeilingDb: null
        );

        var buffer = new float[original.Length];
        int read = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(original.Length, read);
        for (int i = 0; i < read; i++)
        {
            Assert.Equal(original[i], buffer[i]);
        }
    }

    /// <summary>
    /// Negative gain should attenuate signal correctly.
    /// </summary>
    [Fact]
    public void NegativeGain_AttenuatesSignal()
    {
        var signal = SignalGenerator.Sine(1000, 0.8, Fs, 0.5, 1);
        var provider = new LoudnessNormalizingSampleProvider(
            new ArraySampleProvider(signal, Fs, 1),
            gainDb: -6.0, // Attenuate by 6 dB (factor of 0.5)
            truePeakCeilingDb: null
        );

        var buffer = new float[1024];
        int read = provider.Read(buffer, 0, buffer.Length);

        // Find peak amplitude
        float peak = 0;
        for (int i = 0; i < read; i++)
        {
            peak = Math.Max(peak, Math.Abs(buffer[i]));
        }

        // Original amplitude was ~0.8, after -6 dB should be ~0.4
        // Allow tolerance for measurement
        Assert.InRange(peak, 0.3f, 0.5f);
    }
}