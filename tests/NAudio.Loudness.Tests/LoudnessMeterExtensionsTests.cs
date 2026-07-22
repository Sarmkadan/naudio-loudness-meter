using NAudio.Loudness;
using Xunit;

namespace NAudio.Loudness.Tests;

public class LoudnessMeterExtensionsTests
{
    private const int SampleRate = 48000;
    private const int Channels = 1;

    private static LoudnessMeter CreateMeter() => new(SampleRate, Channels);

    [Fact]
    public void GetLufsLevels_WithData_ReturnsCorrectLevels()
    {
        var meter = CreateMeter();
        // Use SignalGenerator to feed valid audio data
        // 1000 Hz tone, 0.5 amplitude, 48kHz, 5 seconds, 1 channel
        var samples = SignalGenerator.Sine(1000, 0.5, SampleRate, 5, Channels);
        meter.AddSamples(samples);

        // Allow some time for the meter to process if necessary (it's sync, so this should be fine)
        var levels = meter.GetLufsLevels();
        
        // Assertions
        Assert.True(levels.MomentaryLufs > -100.0, $"Expected finite momentary LUFS, got {levels.MomentaryLufs}");
        Assert.True(levels.ShortTermLufs > -100.0, $"Expected finite short-term LUFS, got {levels.ShortTermLufs}");
    }

    [Fact]
    public void GetLufsLevels_NullMeter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((LoudnessMeter)null!).GetLufsLevels());
    }

    [Fact]
    public void GetCurrentLufsStatus_ReturnsFormattedString()
    {
        var meter = CreateMeter();
        var samples = new float[SampleRate * 4]; // Enough for momentary
        Array.Fill(samples, 0.5f);
        meter.AddSamples(samples);

        var status = meter.GetCurrentLufsStatus("0.00");
        
        Assert.Contains("Momentary:", status);
        Assert.Contains("Short-term:", status);
        Assert.Contains("Integrated:", status);
        Assert.Contains("LUFS", status);
    }

    [Fact]
    public void IsSilent_Initially_ReturnsTrue()
    {
        var meter = CreateMeter();
        
        Assert.True(meter.IsSilent());
    }

    [Fact]
    public void IsSilent_WithSignal_ReturnsFalse()
    {
        var meter = CreateMeter();
        var samples = new float[SampleRate];
        Array.Fill(samples, 0.1f);
        meter.AddSamples(samples);
        
        // Wait for enough blocks to clear absolute gate (-70 LUFS)
        var moreSamples = new float[SampleRate * 2];
        Array.Fill(moreSamples, 0.1f);
        meter.AddSamples(moreSamples);

        Assert.False(meter.IsSilent());
    }

    [Fact]
    public void GetLoudnessRange_WithData_ReturnsValue()
    {
        var meter = CreateMeter();
        // Need lots of data for LRA calculation
        var samples = new float[SampleRate * 10];
        Array.Fill(samples, 0.1f);
        meter.AddSamples(samples);
        
        var lra = meter.GetLoudnessRange();
        
        // Should be at least 0 (it may be 0 if not enough variance)
        Assert.True(lra >= 0.0);
    }
}
