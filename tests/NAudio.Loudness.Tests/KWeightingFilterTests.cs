using System;
using NAudio.Loudness.Filters;
using Xunit;

namespace NAudio.Loudness.Tests
{
    public class KWeightingFilterTests
    {
        private static readonly double[] TestVector = new double[]
        {
            0.0, 0.1, -0.2, 0.3, -0.4, 0.5, -0.6, 0.7, -0.8, 0.9,
            -1.0, 0.8, -0.6, 0.4, -0.2, 0.0
        };

        [Fact]
        public void ProcessSamples_ShouldMatch_IterativeProcess()
        {
            const int sampleRate = 48000;
            var filter = new KWeightingFilter(sampleRate);

            // Process each sample individually using the public Process method.
            var iterativeResult = new double[TestVector.Length];
            for (int i = 0; i < TestVector.Length; i++)
            {
                iterativeResult[i] = filter.Process(TestVector[i]);
            }

            // Reset the filter to ensure both methods start from the same state.
            filter.Reset();

            // Process the whole vector using the new ProcessSamples method.
            var batchResult = filter.ProcessSamples(TestVector);

            // Verify that both results are identical (within a tight tolerance).
            const double tolerance = 1e-12;
            for (int i = 0; i < TestVector.Length; i++)
            {
                Assert.InRange(Math.Abs(iterativeResult[i] - batchResult[i]), 0, tolerance);
            }
        }

        [Theory]
        // Measured gains at 48 kHz (and ~identical at 44.1 kHz):
        // 100 Hz: -1.13 dB (RLB high-pass transition region)
        // 1 kHz: 0.70 dB (near-unity, reference frequency)
        // 10 kHz: 4.04 dB (shelf boost above corner frequency)
        [InlineData(100, -1.13, 0.3)]
        [InlineData(1000, 0.70, 0.3)]
        [InlineData(10000, 4.04, 0.3)]
        public void FrequencyResponse_At48kHz_MatchesKWeightingCurve(double frequencyHz, double expectedGainDb, double toleranceDb)
        {
            const int sampleRate = 48000;
            const double durationSeconds = 2.0; // 2 seconds total
            const double discardSeconds = 0.5;  // discard first 0.5s transient
            const double amplitude = 1.0;       // full-scale sine

            var filter = new KWeightingFilter(sampleRate);

            // Generate sine wave
            int totalFrames = (int)(sampleRate * durationSeconds);
            int discardFrames = (int)(sampleRate * discardSeconds);
            int measureFrames = totalFrames - discardFrames;

            var input = new double[totalFrames];
            double omega = 2.0 * Math.PI * frequencyHz / sampleRate;
            for (int n = 0; n < totalFrames; n++)
            {
                input[n] = amplitude * Math.Sin(omega * n);
            }

            // Process through filter
            var output = filter.ProcessSamples(input);

            // Measure RMS of steady-state portion (after transient)
            double sumSquares = 0.0;
            for (int i = discardFrames; i < totalFrames; i++)
            {
                sumSquares += output[i] * output[i];
            }
            double rmsOutput = Math.Sqrt(sumSquares / measureFrames);
            double rmsInput = amplitude / Math.Sqrt(2.0); // RMS of full-scale sine

            double gainDb = 20.0 * Math.Log10(rmsOutput / rmsInput);

            Assert.InRange(gainDb, expectedGainDb - toleranceDb, expectedGainDb + toleranceDb);
        }

        [Fact]
        public void FrequencyResponse_At44_1kHz_1kHzGainMatches48kHz()
        {
            const int sampleRate = 44100;
            const double frequencyHz = 1000.0;
            const double durationSeconds = 2.0;
            const double discardSeconds = 0.5;
            const double amplitude = 1.0;

            var filter = new KWeightingFilter(sampleRate);

            int totalFrames = (int)(sampleRate * durationSeconds);
            int discardFrames = (int)(sampleRate * discardSeconds);
            int measureFrames = totalFrames - discardFrames;

            var input = new double[totalFrames];
            double omega = 2.0 * Math.PI * frequencyHz / sampleRate;
            for (int n = 0; n < totalFrames; n++)
            {
                input[n] = amplitude * Math.Sin(omega * n);
            }

            var output = filter.ProcessSamples(input);

            double sumSquares = 0.0;
            for (int i = discardFrames; i < totalFrames; i++)
            {
                sumSquares += output[i] * output[i];
            }
            double rmsOutput = Math.Sqrt(sumSquares / measureFrames);
            double rmsInput = amplitude / Math.Sqrt(2.0);

            double gainDb = 20.0 * Math.Log10(rmsOutput / rmsInput);

            // At 1 kHz, gain should match 48 kHz behavior (~0.70 dB)
            // This confirms coefficients are recomputed for the new sample rate
            Assert.InRange(gainDb, 0.40, 1.00);
        }

        [Fact]
        public void FilterState_IsIndependentPerChannel()
        {
            const int sampleRate = 48000;
            const double frequencyHz = 1000.0;
            const double durationSeconds = 1.0;
            const double amplitude = 1.0;

            // Create two separate filter instances (simulating two channels)
            var filterCh0 = new KWeightingFilter(sampleRate);
            var filterCh1 = new KWeightingFilter(sampleRate);

            int totalFrames = (int)(sampleRate * durationSeconds);

            // Generate signal for channel 0 only
            var inputCh0 = new double[totalFrames];
            double omega = 2.0 * Math.PI * frequencyHz / sampleRate;
            for (int n = 0; n < totalFrames; n++)
            {
                inputCh0[n] = amplitude * Math.Sin(omega * n);
            }

            // Channel 1 gets silence
            var inputCh1 = new double[totalFrames];

            // Process both channels
            var outputCh0 = filterCh0.ProcessSamples(inputCh0);
            var outputCh1 = filterCh1.ProcessSamples(inputCh1);

            // Channel 0 should have non-zero output (processed signal)
            double rmsCh0 = 0.0;
            for (int i = 0; i < totalFrames; i++)
            {
                rmsCh0 += outputCh0[i] * outputCh0[i];
            }
            rmsCh0 = Math.Sqrt(rmsCh0 / totalFrames);
            Assert.True(rmsCh0 > 0.1, "Channel 0 should have processed signal");

            // Channel 1 should remain at zero (no signal fed, state independent)
            double rmsCh1 = 0.0;
            for (int i = 0; i < totalFrames; i++)
            {
                rmsCh1 += outputCh1[i] * outputCh1[i];
            }
            rmsCh1 = Math.Sqrt(rmsCh1 / totalFrames);
            Assert.True(rmsCh1 < 1e-10, "Channel 1 should remain at zero (state independent from channel 0)");
        }

        [Fact]
        public void FilterState_Reset_ClearsStateForBothStages()
        {
            const int sampleRate = 48000;
            const double frequencyHz = 100.0; // Low frequency to excite high-pass transient
            const double amplitude = 1.0;

            var filter = new KWeightingFilter(sampleRate);

            // Feed a signal to build up filter state
            int frames = sampleRate; // 1 second
            var input = new double[frames];
            double omega = 2.0 * Math.PI * frequencyHz / sampleRate;
            for (int n = 0; n < frames; n++)
            {
                input[n] = amplitude * Math.Sin(omega * n);
            }

            // Process to set internal state
            filter.ProcessSamples(input);

            // Now feed silence - output should decay but not be zero immediately due to state
            var silence = new double[100];
            var outputBeforeReset = filter.ProcessSamples(silence);
            bool hasNonZeroBeforeReset = false;
            for (int i = 0; i < 100; i++)
            {
                if (Math.Abs(outputBeforeReset[i]) > 1e-10)
                {
                    hasNonZeroBeforeReset = true;
                    break;
                }
            }
            Assert.True(hasNonZeroBeforeReset, "Filter should have non-zero state after processing signal");

            // Reset the filter
            filter.Reset();

            // Process silence again - output should be exactly zero now
            var outputAfterReset = filter.ProcessSamples(silence);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(0.0, outputAfterReset[i]);
            }
        }
    }
}