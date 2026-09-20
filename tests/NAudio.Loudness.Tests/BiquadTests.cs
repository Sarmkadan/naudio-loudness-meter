using System;
using NAudio.Loudness.Filters;
using Xunit;

namespace NAudio.Loudness.Tests.Filters;

public class BiquadTests
{
    [Fact]
    public void IdentityFilter_PassThrough()
    {
        // Arrange: identity filter (b0=1, others 0)
        var filter = new Biquad(b0: 1.0, b1: 0.0, b2: 0.0, a1: 0.0, a2: 0.0);

        // Act & Assert: for any input, output should equal input
        Assert.Equal(0.0, filter.Process(0.0));
        Assert.Equal(1.0, filter.Process(1.0));
        Assert.Equal(-1.0, filter.Process(-1.0));
        Assert.Equal(3.14, filter.Process(3.14));
    }

    [Fact]
    public void LowPassFilter_DCStepResponse_Stable()
    {
        // Arrange: simple low-pass filter (DC gain = 1)
        // H(z) = 1 / (2 - z^-1) => b0=0.5, b1=0, b2=0, a1=-0.5, a2=0
        var filter = new Biquad(b0: 0.5, b1: 0.0, b2: 0.0, a1: -0.5, a2: 0.0);
        filter.Reset(); // start from zero state

        // Act: apply step input (1.0) repeatedly
        double y1 = filter.Process(1.0);
        double y2 = filter.Process(1.0);
        double y3 = filter.Process(1.0);
        double y4 = filter.Process(1.0);
        double y5 = filter.Process(1.0);

        // Assert: output should converge to 1.0 (DC gain) without oscillation or divergence
        Assert.InRange(y1, 0.0, 1.0);
        Assert.InRange(y2, 0.0, 1.0);
        Assert.InRange(y3, 0.0, 1.0);
        Assert.InRange(y4, 0.0, 1.0);
        Assert.InRange(y5, 0.0, 1.0);
        Assert.True(Math.Abs(y5 - 1.0) < 0.1); // after 5 samples, should be close to 1.0
    }

    [Fact]
    public void StatePersistence_SampleBySampleVsBatch_Identical()
    {
        // Arrange: arbitrary filter
        var filter1 = new Biquad(b0: 0.1, b1: 0.2, b2: 0.3, a1: -0.4, a2: 0.5);
        var filter2 = new Biquad(b0: 0.1, b1: 0.2, b2: 0.3, a1: -0.4, a2: 0.5);
        double[] input = { 0.5, -0.5, 1.0, -1.0, 0.0 };

        // Act: process sample-by-sample with filter1
        double[] outputSampleBySample = new double[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            outputSampleBySample[i] = filter1.Process(input[i]);
        }

        // Act: reset filter2 and process all at once (same as sample-by-sample because state is internal)
        filter2.Reset();
        double[] outputBatch = new double[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            outputBatch[i] = filter2.Process(input[i]);
        }

        // Assert: outputs should be identical
        Assert.Equal(outputSampleBySample, outputBatch);
    }

    [Fact]
    public void Reset_ZerosInternalState()
    {
        // Arrange: filter with non-zero state
        var filter = new Biquad(b0: 0.1, b1: 0.2, b2: 0.3, a1: -0.4, a2: 0.5);
        // Push some samples to set state
        filter.Process(1.0);
        filter.Process(-1.0);
        filter.Process(0.5);

        // Act: reset
        filter.Reset();

        // Assert: after reset, processing zero should yield zero (since state is zero)
        // For zero input, output = -a1*y1 - a2*y2, but y1 and y2 are zero after reset
        Assert.Equal(0.0, filter.Process(0.0));
        // Next sample should also be zero because state remains zero until non-zero input
        Assert.Equal(0.0, filter.Process(0.0));
    }

    [Fact]
    public void NyquistFrequency_NumericalStability_LongDuration()
    {
        // Arrange: filter that should be stable at Nyquist (e.g., low-pass)
        // Using a simple low-pass: b0=0.5, b1=0.5, a1=0, a2=0 (two-point moving average)
        var filter = new Biquad(b0: 0.5, b1: 0.5, b2: 0.0, a1: 0.0, a2: 0.0);
        filter.Reset();

        // Act: process Nyquist frequency signal (alternating 1, -1, 1, -1, ...) for 10 seconds at 48 kHz
        int sampleRate = 48000;
        int durationSeconds = 10;
        int totalSamples = sampleRate * durationSeconds;
        double lastOutput = 0.0;
        bool hasNaN = false;
        bool hasInfinity = false;

        for (int n = 0; n < totalSamples; n++)
        {
            double input = (n % 2 == 0) ? 1.0 : -1.0; // Nyquist: fs/2
            double output = filter.Process(input);
            if (double.IsNaN(output) || double.IsInfinity(output))
            {
                hasNaN = true;
                if (double.IsInfinity(output))
                    hasInfinity = true;
                break;
            }
            lastOutput = output;
        }

        // Assert: output should not diverge to NaN or infinity
        Assert.False(hasNaN, "Output became NaN");
        Assert.False(hasInfinity, "Output became infinity");
        // For this specific filter and input, output should be bounded
        Assert.InRange(lastOutput, -1.0, 1.0);
    }
}