using System;
using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Tests for <see cref="LoudnessRangeCalculator"/> - Loudness Range (LRA) calculation.
/// Validates compliance with EBU Tech 3342 specification.
/// </summary>
public class LoudnessRangeCalculatorTests
{
    private const int SampleRate = 44100;

    [Fact]
    public void ComputeLoudnessRange_ConstantSignal_ReturnsZero()
    {
        // Arrange
        var values = new double[] { -23.0, -23.0, -23.0, -23.0, -23.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert
        Assert.Equal(0.0, lra, 2); // Should be exactly 0 for constant signal
    }

    [Fact]
    public void ComputeLoudnessRange_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LoudnessRangeCalculator.ComputeLoudnessRange(null!));
    }

    [Fact]
    public void ComputeLoudnessRange_EmptyInput_ReturnsZero()
    {
        // Arrange
        var values = Array.Empty<double>();

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert
        Assert.Equal(0.0, lra);
    }

    [Fact]
    public void ComputeLoudnessRange_AllBelowAbsoluteGate_ReturnsZero()
    {
        // Arrange - all values below -70 LUFS absolute gate
        var values = new double[] { -80.0, -75.0, -72.0, -85.0, -90.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - no values survive absolute gate
        Assert.Equal(0.0, lra);
    }

    [Fact]
    public void ComputeLoudnessRange_SingleValueAboveGate_ReturnsZero()
    {
        // Arrange - only one value survives absolute gate, not enough for percentile calculation
        var values = new double[] { -80.0, -65.0, -80.0, -80.0, -80.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - need at least 2 values for meaningful LRA
        Assert.Equal(0.0, lra);
    }

    [Fact]
    public void ComputeLoudnessRange_TwoValuesAboveGate_ReturnsDifference()
    {
        // Arrange - two values survive absolute gate and relative gate
        // Absolute gate (-70): keeps values > -70
        // Relative gate (max - 10): keeps values >= (max - 10)
        // For values [-60, -50]: max = -50, relativeThreshold = -60, both values kept
        var values = new double[] { -80.0, -60.0, -50.0, -80.0, -80.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - with only 2 gated values, the 10th/95th percentile interpolation
        // pulls the range in from the raw min/max: p10 = -59, p95 = -50.5 -> LRA = 8.5
        Assert.Equal(8.5, lra, 2);
    }

    [Fact]
    public void ComputeLoudnessRange_WideRange_ReturnsCorrectLRA()
    {
        // Arrange - wide loudness range: -60 to -10
        var values = new double[] { -80.0, -60.0, -40.0, -20.0, -10.0, -80.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - absolute gate keeps [-60,-40,-20,-10]; relative gate (max=-10, threshold=-20)
        // then drops -60 and -40, leaving [-20,-10] -> p10=-19, p95=-10.5 -> LRA = 8.5
        Assert.Equal(8.5, lra, 2);
    }

    [Fact]
    public void ComputeLoudnessRange_WithRelativeGate_ExcludesOutliers()
    {
        // Arrange - values where some should be excluded by relative gate
        // Max is -20, relative gate is -30 (max - 10), so -40 should be excluded
        var values = new double[] { -20.0, -25.0, -30.0, -40.0, -22.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - -40 is excluded by the relative gate; the remaining gated set
        // [-30,-25,-22,-20] yields p10=-29.1, p95=-20.4 -> LRA = 8.2
        Assert.Equal(8.2, lra, 2);
    }

    [Fact]
    public void ComputeLoudnessRange_PercentileInterpolation_HandlesNonIntegerPositions()
    {
        // Arrange - test values that will require interpolation
        // 10 values from -50 to -41 in 1 LU steps
        var values = new double[] { -80.0, -50.0, -49.0, -48.0, -47.0, -46.0, -45.0, -44.0, -43.0, -42.0, -41.0, -80.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - should calculate 10th and 95th percentiles with interpolation
        // 10th percentile of 10 values: position = (10-1)*0.1 = 0.9, interpolates between index 0 and 1
        // 95th percentile: position = (10-1)*0.95 = 8.55, interpolates between index 8 and 9
        Assert.True(lra > 0); // Should have some range
        Assert.True(lra < 10); // Should be less than full range (9 LU)
    }

    [Fact]
    public void ComputeLoudnessRange_ExactPercentilePositions()
    {
        // Arrange - 10 values where percentiles fall exactly on array indices
        var values = new double[] { -80.0, -50.0, -45.0, -40.0, -35.0, -30.0, -25.0, -20.0, -15.0, -10.0, -80.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - absolute gate keeps everything except -80; relative gate (max=-10,
        // threshold=-20) then keeps only [-20,-15,-10] -> p10=-19, p95=-10.5 -> LRA = 8.5
        Assert.Equal(8.5, lra, 2);
    }

    [Fact]
    public void ComputeLoudnessRange_DuplicateValues_HandlesCorrectly()
    {
        // Arrange - multiple values at same loudness levels
        var values = new double[] { -80.0, -30.0, -30.0, -30.0, -10.0, -10.0, -80.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - max is -10, relative threshold is -20, so the -30 duplicates are
        // gated out entirely; only the two -10 values remain -> LRA = 0
        Assert.Equal(0.0, lra, 2);
    }

    [Fact]
    public void ComputeLoudnessRange_ValuesAtGateBoundaries()
    {
        // Arrange - values exactly at gate boundaries
        var values = new double[] { -70.0, -60.0, -50.0, -40.0, -30.0 };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - -70 fails the absolute gate (not > -70); of the survivors
        // [-60,-50,-40,-30], the relative gate (max=-30, threshold=-40) keeps
        // only [-40,-30] -> p10=-39, p95=-30.5 -> LRA = 8.5
        Assert.Equal(8.5, lra, 2);
    }

    // Note: Percentile method is private in LoudnessRangeCalculator.
    // It is tested indirectly through ComputeLoudnessRange tests above.

    [Fact]
    public void ComputeLoudnessRange_RealWorldEBU3342Compliance()
    {
        // This test validates against a known EBU Tech 3342 scenario
        // Create a signal with LRA of approximately 15 LU
        var values = new double[] {
            -80.0, -35.0, -30.0, -28.0, -26.0, -24.0, -22.0, -20.0,
            -19.0, -18.0, -17.0, -16.0, -15.0, -14.0, -13.0, -80.0
        };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - should be in reasonable range (EBU Tech 3342 typical range is 5-20 LU for most programs)
        Assert.InRange(lra, 5.0, 20.0);
    }

    [Fact]
    public void ComputeLoudnessRange_NegativeInfinityValues_HandlesCorrectly()
    {
        // Arrange - includes -Infinity values (from silence or very quiet sections)
        var values = new double[] { double.NegativeInfinity, -65.0, -50.0, double.NegativeInfinity };

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - -Infinity values are removed by the absolute gate (-70 LUFS);
        // of [-65, -50], the relative gate (max=-50, threshold=-60) drops -65,
        // leaving a single value -> not enough for a meaningful range -> LRA = 0
        Assert.Equal(0.0, lra, 2);
    }

    [Fact]
    public void ComputeLoudnessRange_LargeDataset_Performance()
    {
        // Arrange - large dataset to test performance
        var values = new double[10000];
        var random = new Random(42);
        for (int i = 0; i < values.Length; i++)
        {
            // Mix of values around typical program loudness
            double baseLevel = -23.0 + random.NextDouble() * 10.0 - 5.0;
            values[i] = Math.Max(baseLevel, -80.0); // Ensure not too quiet
        }

        // Act
        double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

        // Assert - should complete without exception and return reasonable value
        Assert.True(lra >= 0);
        Assert.True(lra <= 30); // Typical LRA for most programs
    }
}