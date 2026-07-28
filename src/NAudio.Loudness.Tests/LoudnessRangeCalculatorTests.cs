using System;
using System.Collections.Generic;
using NAudio.Loudness;
using Xunit;

namespace NAudio.Loudness.Tests
{
    public static class LoudnessRangeCalculatorTests
    {
        [Fact]
        public static void ComputeLoudnessRange_EmptyInput_ReturnsZero()
        {
            // Arrange
            IEnumerable<double> values = Array.Empty<double>();

            // Act
            double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

            // Assert
            Assert.Equal(0.0, lra);
        }

        [Fact]
        public static void ComputeLoudnessRange_SingleValue_ReturnsZero()
        {
            // Arrange
            IEnumerable<double> values = new[] { -23.5 };

            // Act
            double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

            // Assert
            Assert.Equal(0.0, lra);
        }

        [Fact]
        public static void ComputeLoudnessRange_AllIdenticalValues_ReturnsZero()
        {
            // Arrange
            IEnumerable<double> values = new[] { -30.0, -30.0, -30.0, -30.0 };

            // Act
            double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

            // Assert
            Assert.Equal(0.0, lra);
        }

        [Fact]
        public static void ComputeLoudnessRange_AllBelowAbsoluteGate_ReturnsZero()
        {
            // Arrange
            IEnumerable<double> values = new[] { -80.0, -75.5, -71.0, -70.1 };

            // Act
            double lra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

            // Assert
            Assert.Equal(0.0, lra);
        }

        [Fact]
        public static void ComputeLoudnessRange_RealisticDistribution_MatchesManualCalculation()
        {
            // Arrange: values spanning a wide range, some will be gated out by the relative gate.
            double[] values =
            {
                -60, -55, -50, -45, -40, -35, -30, -25, -20, -15, -10
            };

            // Expected LRA calculated manually (see analysis):
            // After absolute gate: all values kept.
            // Max = -10, relative threshold = -30.
            // Values kept after relative gate: -30, -25, -20, -15, -10.
            // Sorted: -30, -25, -20, -15, -10.
            // 10th percentile ≈ -28.0, 95th percentile ≈ -11.0, LRA ≈ 17.0.
            const double expectedLra = 17.0;
            const double tolerance = 0.1;

            // Act
            double actualLra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

            // Assert
            Assert.InRange(actualLra, expectedLra - tolerance, expectedLra + tolerance);
        }

        [Fact]
        public static void ComputeLoudnessRange_ContainsNaNOrNegativeInfinity_IgnoresInvalidValues()
        {
            // Arrange: mix of valid, NaN and -Infinity values.
            double[] values =
            {
                double.NaN,
                double.NegativeInfinity,
                -65.0,
                -55.0,
                -45.0,
                -35.0,
                -25.0,
                -15.0,
                -5.0   // above the absolute gate and will become the max
            };

            // Expected calculation:
            // Absolute gate keeps all except NaN and -Infinity (they are filtered out).
            // Max = -5, relative threshold = -25.
            // Values kept after relative gate: -25, -15, -5.
            // Sorted: -25, -15, -5.
            // 10th percentile ≈ -23.0, 95th percentile ≈ -5.5, LRA ≈ 17.5.
            const double expectedLra = 17.5;
            const double tolerance = 0.1;

            // Act
            double actualLra = LoudnessRangeCalculator.ComputeLoudnessRange(values);

            // Assert
            Assert.InRange(actualLra, expectedLra - tolerance, expectedLra + tolerance);
        }
    }
}
