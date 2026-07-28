using System;
using NAudio.Wave;
using NAudio.Loudness;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for <see cref="LoudnessNormalizingSampleProvider"/>.
/// </summary>
public class LoudnessNormalizingSampleProviderTests
{
    /// <summary>
    /// Simple constant‑value sample provider used for deterministic testing.
    /// </summary>
    private sealed class ConstantSampleProvider : ISampleProvider
    {
        private readonly WaveFormat _waveFormat;
        private readonly float _value;
        private readonly int _totalSamples;
        private int _samplesReturned;

        public ConstantSampleProvider(WaveFormat waveFormat, float value, int totalSamples)
        {
            _waveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));
            _value = value;
            _totalSamples = totalSamples;
        }

        public WaveFormat WaveFormat => _waveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            int remaining = _totalSamples - _samplesReturned;
            int toCopy = Math.Min(remaining, count);
            for (int i = 0; i < toCopy; i++)
            {
                buffer[offset + i] = _value;
            }
            _samplesReturned += toCopy;
            return toCopy;
        }
    }

    [Fact]
    public void AppliedGain_NoLimit_EqualsRequestedGain()
    {
        // Arrange: source with any value, no true‑peak ceiling (null)
        var source = new ConstantSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2), 0.5f, 100);
        double gainDb = 6.0; // ~2× linear
        var provider = new LoudnessNormalizingSampleProvider(source, gainDb, truePeakCeilingDb: null);

        // Act
        double expectedLinear = Math.Pow(10.0, gainDb / 20.0);
        // Assert
        Assert.Equal((float)expectedLinear, provider.AppliedGainLinear, 5);
    }

    [Fact]
    public void AppliedGain_WithLimit_ClampsToCeiling()
    {
        // Arrange: source value 0.5, ceiling -6 dBTP (linear 0.5)
        var source = new ConstantSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1), 0.5f, 100);
        double gainDb = 6.0; // request 2× gain
        double ceilingDb = -6.0; // linear 0.5
        var provider = new LoudnessNormalizingSampleProvider(source, gainDb, truePeakCeilingDb: ceilingDb);

        // Act
        // Expected max gain = ceilingLinear / ConservativeSamplePeak (0.95)
        float expectedMaxGain = (float)(Math.Pow(10.0, ceilingDb / 20.0) / 0.95);
        // Assert
        Assert.Equal(expectedMaxGain, provider.AppliedGainLinear, 5);
    }

    [Fact]
    public void Read_AppliesGainAndClipping()
    {
        // Arrange: source value 1.0 (full scale), ceiling -6 dBTP (linear 0.5)
        var source = new ConstantSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1), 1.0f, 10);
        double gainDb = 6.0; // request 2× gain
        double ceilingDb = -6.0;
        var provider = new LoudnessNormalizingSampleProvider(source, gainDb, truePeakCeilingDb: ceilingDb);

        float[] buffer = new float[10];
        // Act
        int read = provider.Read(buffer, 0, buffer.Length);

        // Assert
        Assert.Equal(10, read);
        // After gain limiting, the provider should clip to the ceiling (0.5)
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -0.5f, 0.5f);
        }
    }

    [Fact]
    public void Read_ReturnsExactCountWhenFewerSamplesAvailable()
    {
        // Arrange: source only has 5 samples
        var source = new ConstantSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1), 0.2f, 5);
        var provider = new LoudnessNormalizingSampleProvider(source, 0.0, truePeakCeilingDb: null);

        float[] buffer = new float[10];
        // Act
        int read = provider.Read(buffer, 0, 10);

        // Assert
        Assert.Equal(5, read);
    }
}
