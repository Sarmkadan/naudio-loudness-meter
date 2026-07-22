namespace NAudio.Loudness.Tests;

using NAudio.Loudness.Filters;
using Xunit;

/// <summary>
/// Tests for <see cref="Biquad"/> - Direct-form I biquad filter section.
/// </summary>
public class BiquadTests
{
    [Fact]
    public void Process_DCInput_LowpassFilterShouldPassDC()
    {
        // Arrange: Create a simple low-pass filter with cutoff at 1000 Hz
        // Using bilinear transform coefficients for a simple low-pass at 48 kHz
        // This creates a filter that should pass DC (0 Hz) with minimal attenuation
        var lowpass = new Biquad(
            b0: 0.0675,  // Feed-forward coefficients (normalized)
            b1: 0.1349,
            b2: 0.0675,
            a1: -1.1429,
            a2: 0.4128
        );

        const int samplesToProcess = 10000;
        const double dcValue = 1.0; // Full scale DC

        // Act: Process DC through the low-pass filter
        double maxOutput = 0.0;
        double lastOutput = 0.0;
        for (int i = 0; i < samplesToProcess; i++)
        {
            lastOutput = lowpass.Process(dcValue);
            maxOutput = Math.Max(maxOutput, Math.Abs(lastOutput));
        }

        // Assert: DC should pass through with minimal attenuation (low-pass filter)
        // The DC gain should be close to 1.0 (0 dB)
        Assert.InRange(lastOutput, 0.9, 1.1);

        // Output should stabilize to a constant value
        Assert.Equal(lastOutput, lowpass.Process(dcValue), 10);
    }

    [Fact]
    public void Process_DCInput_HighpassFilterShouldBeDifferentFromLowpass()
    {
        // Arrange: Create both low-pass and high-pass filters
        var lowpass = new Biquad(
            b0: 0.0675,
            b1: 0.1349,
            b2: 0.0675,
            a1: -1.1429,
            a2: 0.4128
        );

        var highpass = new Biquad(
            b0: 0.5,
            b1: -0.5,
            b2: 0.0,
            a1: 0.0,
            a2: 0.0
        );

        const double dcValue = 1.0;

        // Act: Process DC through both filters
        double lowpassOutput = 0.0;
        double highpassOutput = 0.0;
        for (int i = 0; i < 1000; i++)
        {
            lowpassOutput = lowpass.Process(dcValue);
            highpassOutput = highpass.Process(dcValue);
        }

        // Assert: Low-pass should pass DC, high-pass should attenuate it
        // They should produce significantly different outputs
        Assert.NotEqual(lowpassOutput, highpassOutput, 2);
        Assert.InRange(Math.Abs(lowpassOutput), 0.9, 1.1); // Low-pass passes DC
    }

    [Fact]
    public void Process_SineWave_StableOutput()
    {
        // Arrange
        const int sampleRate = 48000;
        const double frequency = 1000.0; // 1 kHz
        const double amplitude = 0.5; // -6 dBFS
        const int samplesToProcess = 20000;

        // Create a 1 kHz sine wave
        float[] sineWave = SignalGenerator.Sine(frequency, amplitude, sampleRate, samplesToProcess / (double)sampleRate, 1);

        // Create a low-pass filter at 2000 Hz
        var lowpass = new Biquad(
            b0: 0.2929,
            b1: 0.5858,
            b2: 0.2929,
            a1: -0.0000,
            a2: 0.1716
        );

        // Act: Process the sine wave
        double maxOutput = 0.0;
        double minOutput = 0.0;
        for (int i = 0; i < samplesToProcess; i++)
        {
            double filteredSample = lowpass.Process(sineWave[i]);
            maxOutput = Math.Max(maxOutput, filteredSample);
            minOutput = Math.Min(minOutput, filteredSample);
        }

        // Assert: Output should be bounded (stable)
        // For a bounded input (-0.5 to 0.5), output should remain bounded
        Assert.InRange(maxOutput, -0.6, 0.6);
        Assert.InRange(minOutput, -0.6, 0.6);
    }

    [Fact]
    public void Process_BoundedInput_ProducesBoundedOutput()
    {
        // Arrange: Create a low-pass filter
        var filter = new Biquad(
            b0: 0.2929,
            b1: 0.5858,
            b2: 0.2929,
            a1: -0.0000,
            a2: 0.1716
        );

        // Act: Process various bounded inputs
        double[] testInputs = { -1.0, -0.5, -0.1, 0.0, 0.1, 0.5, 1.0 };
        double[] outputs = new double[testInputs.Length];

        for (int i = 0; i < testInputs.Length; i++)
        {
            outputs[i] = filter.Process(testInputs[i]);
        }

        // Assert: All outputs should be bounded by input magnitude
        foreach (double output in outputs)
        {
            Assert.InRange(output, -1.1, 1.1);
        }
    }

    [Fact]
    public void Process_Reset_ShouldClearState()
    {
        // Arrange
        var filter = new Biquad(
            b0: 0.2929,
            b1: 0.5858,
            b2: 0.2929,
            a1: -0.0000,
            a2: 0.1716
        );

        // Process some samples to build up state
        filter.Process(1.0);
        filter.Process(0.5);
        filter.Process(0.0);

        // Get output before reset
        double outputBeforeReset = filter.Process(0.0);

        // Reset the filter
        filter.Reset();

        // Act: Process the same input after reset
        double outputAfterReset = filter.Process(0.0);

        // Assert: After reset, the filter should produce a different output
        // (reset clears the state variables, changing the output)
        Assert.NotEqual(outputBeforeReset, outputAfterReset);
    }

    [Fact]
    public void Process_Reset_ShouldNotThrow()
    {
        // Arrange
        var filter = new Biquad(
            b0: 0.2929,
            b1: 0.5858,
            b2: 0.2929,
            a1: -0.0000,
            a2: 0.1716
        );

        // Act: Reset should not throw
        var exception = Record.Exception(() => filter.Reset());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Process_MultipleSamples_ShouldProcessCorrectly()
    {
        // Arrange
        var filter = new Biquad(
            b0: 0.5,
            b1: 0.0,
            b2: 0.0,
            a1: 0.0,
            a2: 0.0
        );

        const double input = 1.0;
        const int samplesToProcess = 100;

        // Act
        double output = 0.0;
        for (int i = 0; i < samplesToProcess; i++)
        {
            output = filter.Process(input);
        }

        // Assert: Output should be 0.5 (b0 * input with b1=b2=a1=a2=0)
        Assert.Equal(0.5, output, 10);
    }

    [Fact]
    public void Process_ZeroCoefficients_ShouldPassInput()
    {
        // Arrange: Identity filter (b0=1, all others=0)
        var identity = new Biquad(
            b0: 1.0,
            b1: 0.0,
            b2: 0.0,
            a1: 0.0,
            a2: 0.0
        );

        const double input = 0.75;

        // Act
        double output = identity.Process(input);

        // Assert: Should pass input unchanged
        Assert.Equal(input, output);
    }

    [Fact]
    public void Process_NegativeInput_ShouldProduceCorrectOutput()
    {
        // Arrange
        var filter = new Biquad(
            b0: 0.5,
            b1: 0.0,
            b2: 0.0,
            a1: 0.0,
            a2: 0.0
        );

        const double negativeInput = -0.5;

        // Act
        double output = filter.Process(negativeInput);

        // Assert: Should produce correct output for negative input
        Assert.Equal(negativeInput * 0.5, output);
    }

    [Fact]
    public void Process_ConsecutiveCalls_ShouldMaintainState()
    {
        // Arrange
        var filter = new Biquad(
            b0: 0.5,
            b1: 0.5,
            b2: 0.0,
            a1: 0.0,
            a2: 0.0
        );

        // Act: Process a sequence of values
        double output1 = filter.Process(1.0);
        double output2 = filter.Process(0.5);
        double output3 = filter.Process(0.0);

        // Assert: Outputs should be different due to state accumulation
        Assert.NotEqual(output1, output2);
        Assert.NotEqual(output2, output3);
    }
}
