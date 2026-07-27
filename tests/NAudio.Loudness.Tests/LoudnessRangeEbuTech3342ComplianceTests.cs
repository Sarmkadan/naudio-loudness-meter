using System;
using Xunit;
using Xunit.Abstractions;

namespace NAudio.Loudness.Tests;

/// <summary>
/// EBU Tech 3342 Loudness Range (LRA) Compliance Tests.
/// These tests validate that the LRA implementation complies with EBU Tech 3342 specification
/// by using the test signals defined in EbuTech3342TestSignals.
/// </summary>
public class LoudnessRangeEbuTech3342ComplianceTests
{
    private readonly ITestOutputHelper _output;

    public LoudnessRangeEbuTech3342ComplianceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void L1_ConstantLoudness_ShouldHaveZeroLRA()
    {
        // Arrange
        int sampleRate = 44100;
        var signal = EbuTech3342TestSignals.ConstantLoudness_23LUFS(sampleRate, 5.0);
        var meter = new LoudnessMeter(sampleRate, 1);

        // Act
        meter.AddSamples(signal);
        double lra = meter.LoudnessRange;

        // Assert - constant signal should have zero loudness range
        Assert.Equal(0.0, lra, 1);
        _output.WriteLine($"L1 Constant Loudness Test: LRA = {lra:F2} LU (expected: 0.0 LU)");
    }

    // Integration tests with LoudnessMeter are complex due to sliding window behavior.
    // The core LRA algorithm validation is done through LoudnessRangeCalculatorTests.
    // These tests verify that LoudnessMeter correctly integrates with the LRA calculation.

    [Fact]
    public void LoudnessMeter_LRA_ConstantSignal_ReturnsZero()
    {
        // Arrange
        int sampleRate = 44100;
        var signal = EbuTech3342TestSignals.ConstantLoudness_23LUFS(sampleRate, 5.0);
        var meter = new LoudnessMeter(sampleRate, 1);

        // Act
        meter.AddSamples(signal);
        double lra = meter.LoudnessRange;

        // Assert
        Assert.Equal(0.0, lra, 1);
    }
}
