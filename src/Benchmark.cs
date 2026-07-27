using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NAudio.Wave;

namespace NAudio.Loudness.Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public class LoudnessMeterBenchmark
    {
        private readonly ISampleProvider _source;
        private readonly float _sampleRate;
        private readonly int _windowSizeMs;
        private readonly int _windowCount;

        public LoudnessMeterBenchmark()
        {
            _source = new ArraySampleProvider();
            _sampleRate = 44100;
            _windowSizeMs = 400;
            _windowCount = 3;
        }

        [Benchmark]
        public void ComputeLoudnessRange()
        {
            var meter = new LoudnessMeter(_source, _sampleRate, _windowSizeMs, _windowCount);
            for (int i = 0; i < 1000; i++)
            {
                meter.AddSamples(new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f });
                meter.ComputeLoudnessRange();
            }
        }
    }
}
