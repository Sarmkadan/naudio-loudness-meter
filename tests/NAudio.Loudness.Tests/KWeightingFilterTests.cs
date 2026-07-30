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
    }
}
