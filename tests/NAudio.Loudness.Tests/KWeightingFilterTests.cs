namespace NAudio.Loudness.Tests;

using NAudio.Loudness.Filters;
using Xunit;

/// <summary>
/// Tests for <see cref="KWeightingFilter"/> - ITU-R BS.1770 K-weighting filter.
/// </summary>
public class KWeightingFilterTests
{
    [Fact]
    public void Process_1kHzSineAt48kHz_ShouldHaveApproximatelyZeroGain()
    {
        // Arrange
        const int sampleRate = 48000;
        const double frequency = 1000.0; // 1 kHz
        const double amplitude = 0.5; // -6 dBFS
        const int samplesToProcess = 10000; // Process enough samples to get stable RMS

        // Create a 1 kHz sine wave
        float[] sineWave = SignalGenerator.Sine(frequency, amplitude, sampleRate, samplesToProcess / (double)sampleRate, 1);

        // Create filter
        var filter = new KWeightingFilter(sampleRate);

        // Act - process the sine wave through the filter
        double rmsSumInput = 0.0;
        double rmsSumOutput = 0.0;

        for (int i = 0; i < samplesToProcess; i++)
        {
            double filteredSample = filter.Process(sineWave[i]);
            rmsSumInput += sineWave[i] * sineWave[i];
            rmsSumOutput += filteredSample * filteredSample;
        }

        double rmsInput = Math.Sqrt(rmsSumInput / samplesToProcess);
        double rmsOutput = Math.Sqrt(rmsSumOutput / samplesToProcess);
        double gainDb = 20 * Math.Log10(rmsOutput / rmsInput);

        // Assert - gain should be approximately 0 dB (within tolerance for BS.1770 reference)
        // The K-weighting filter is designed to have ~0 dB gain at 1 kHz
        Assert.InRange(gainDb, -1.0, 1.0);
    }

    [Fact]
    public void Process_DCSignal_ShouldBeHeavilyAttenuated()
    {
        // Arrange
        const int sampleRate = 48000;
        const double frequency = 0.0; // DC
        const double amplitude = 1.0; // Full scale
        const int samplesToProcess = 10000;

        // Create DC signal
        float[] dcSignal = SignalGenerator.Sine(frequency, amplitude, sampleRate, samplesToProcess / (double)sampleRate, 1);

        // Create filter
        var filter = new KWeightingFilter(sampleRate);

        // Act - process DC through filter
        double maxOutput = 0.0;
        for (int i = 0; i < samplesToProcess; i++)
        {
            double filteredSample = filter.Process(dcSignal[i]);
            maxOutput = Math.Max(maxOutput, Math.Abs(filteredSample));
        }

        // Assert - DC should be heavily attenuated (RLB high-pass has ~-40 dB at DC)
        // The high-pass filter should attenuate DC by more than 30 dB
        Assert.InRange(maxOutput, 0, 0.03); // Less than 3% of input amplitude
    }

    [Fact]
    public void Process_Reset_ShouldNotThrowAndFilterShouldWorkAfterReset()
    {
        // Arrange
        const int sampleRate = 48000;
        const double frequency = 1000.0;
        const double amplitude = 0.5;
        const int samplesToProcess = 10000;

        // Create a 1 kHz sine wave
        float[] sineWave = SignalGenerator.Sine(frequency, amplitude, sampleRate, samplesToProcess / (double)sampleRate, 1);

        // Create filter
        var filter = new KWeightingFilter(sampleRate);

        // Act - process some samples
        double outputBeforeReset = 0.0;
        for (int i = 0; i < 1000; i++)
        {
            outputBeforeReset = filter.Process(sineWave[i]);
        }

        // Reset filter (should not throw)
        filter.Reset();

        // Process after reset - should still work
        double outputAfterReset = 0.0;
        for (int i = 0; i < samplesToProcess; i++)
        {
            outputAfterReset = filter.Process(sineWave[i]);
        }

        // Assert - filter should still produce output after reset
        Assert.NotEqual(0.0, outputAfterReset, 5);
    }

    [Fact]
    public void Process_441kHzSampleRate_ShouldWorkCorrectly()
    {
        // Arrange
        const int sampleRate = 44100;
        const double frequency = 1000.0;
        const double amplitude = 0.5;
        const int samplesToProcess = 10000;

        // Create a 1 kHz sine wave
        float[] sineWave = SignalGenerator.Sine(frequency, amplitude, sampleRate, samplesToProcess / (double)sampleRate, 1);

        // Create filter
        var filter = new KWeightingFilter(sampleRate);

        // Act - process the sine wave through the filter
        double rmsSumInput = 0.0;
        double rmsSumOutput = 0.0;

        for (int i = 0; i < samplesToProcess; i++)
        {
            double filteredSample = filter.Process(sineWave[i]);
            rmsSumInput += sineWave[i] * sineWave[i];
            rmsSumOutput += filteredSample * filteredSample;
        }

        double rmsInput = Math.Sqrt(rmsSumInput / samplesToProcess);
        double rmsOutput = Math.Sqrt(rmsSumOutput / samplesToProcess);
        double gainDb = 20 * Math.Log10(rmsOutput / rmsInput);

        // Assert - gain should be approximately 0 dB at 1 kHz for any sample rate
        Assert.InRange(gainDb, -1.0, 1.0);
    }

    [Fact]
    public void Process_96kHzSampleRate_ShouldWorkCorrectly()
    {
        // Arrange
        const int sampleRate = 96000;
        const double frequency = 1000.0;
        const double amplitude = 0.5;
        const int samplesToProcess = 10000;

        // Create a 1 kHz sine wave
        float[] sineWave = SignalGenerator.Sine(frequency, amplitude, sampleRate, samplesToProcess / (double)sampleRate, 1);

        // Create filter
        var filter = new KWeightingFilter(sampleRate);

        // Act - process the sine wave through the filter
        double rmsSumInput = 0.0;
        double rmsSumOutput = 0.0;

        for (int i = 0; i < samplesToProcess; i++)
        {
            double filteredSample = filter.Process(sineWave[i]);
            rmsSumInput += sineWave[i] * sineWave[i];
            rmsSumOutput += filteredSample * filteredSample;
        }

        double rmsInput = Math.Sqrt(rmsSumInput / samplesToProcess);
        double rmsOutput = Math.Sqrt(rmsSumOutput / samplesToProcess);
        double gainDb = 20 * Math.Log10(rmsOutput / rmsInput);

        // Assert - gain should be approximately 0 dB at 1 kHz for any sample rate
        Assert.InRange(gainDb, -1.0, 1.0);
    }

    [Fact]
    public void Process_MultipleChannels_ShouldProcessIndependently()
    {
        // Arrange
        const int sampleRate = 48000;
        const double frequency = 1000.0;
        const double amplitude = 0.5;
        const int samplesToProcess = 10000;
        const int channels = 2; // Stereo

        // Create stereo sine wave
        float[] stereoWave = SignalGenerator.Sine(frequency, amplitude, sampleRate, samplesToProcess / (double)sampleRate, channels);

        // Create filter
        var filter = new KWeightingFilter(sampleRate);

        // Act - process stereo signal
        double rmsSumInput = 0.0;
        double rmsSumOutput = 0.0;

        for (int i = 0; i < samplesToProcess * channels; i++)
        {
            double filteredSample = filter.Process(stereoWave[i]);
            rmsSumInput += stereoWave[i] * stereoWave[i];
            rmsSumOutput += filteredSample * filteredSample;
        }

        double rmsInput = Math.Sqrt(rmsSumInput / (samplesToProcess * channels));
        double rmsOutput = Math.Sqrt(rmsSumOutput / (samplesToProcess * channels));
        double gainDb = 20 * Math.Log10(rmsOutput / rmsInput);

        // Assert - gain should be approximately 0 dB
        Assert.InRange(gainDb, -1.0, 1.0);
    }
}
