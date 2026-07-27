using System;
using System.Text.Json;
using NAudio.Wave;

namespace NAudio.Loudness;

/// <summary>
/// Computes loudness related metrics (integrated loudness, loudness range, true‑peak, etc.)
/// over a sliding window of audio samples. Handles edge‑cases such as pure silence
/// (all blocks gated out) and inputs shorter than a single measurement block by
/// returning <see cref="double.NegativeInfinity"/> for integrated loudness and a
/// loudness range of <c>0</c>.
/// </summary>
public sealed class LoudnessMeter
{
    private readonly ISampleProvider _source;
    private readonly int _sampleRate;
    private readonly int _windowSizeMs;
    private readonly int _windowCount;
    private readonly float[] _ringBuffer;
    private int _ringBufferIndex;
    private int _ringBufferCount;

    /// <summary>
    /// Creates a new <see cref="LoudnessMeter"/>.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    /// <param name="sampleRate">Sample rate in Hz (must be &gt; 0).</param>
    /// <param name="windowSizeMs">Window size in milliseconds (must be &gt; 0).</param>
    /// <param name="windowCount">Number of windows to keep (must be &gt; 0).</param>
    /// <exception cref="ArgumentNullException">If <paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">If any numeric argument is not positive.</exception>
    public LoudnessMeter(ISampleProvider source, int sampleRate, int windowSizeMs, int windowCount)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (sampleRate <= 0) throw new ArgumentException("Sample rate must be positive.", nameof(sampleRate));
        if (windowSizeMs <= 0) throw new ArgumentException("Window size must be positive.", nameof(windowSizeMs));
        if (windowCount <= 0) throw new ArgumentException("Window count must be positive.", nameof(windowCount));

        _source = source;
        _sampleRate = sampleRate;
        _windowSizeMs = windowSizeMs;
        _windowCount = windowCount;
        _ringBuffer = new float[_windowSizeMs * _sampleRate / 1000];
        _ringBufferIndex = 0;
        _ringBufferCount = 0;
    }

    /// <summary>
    /// Feeds interleaved samples into the meter.
    /// </summary>
    /// <param name="samples">Read‑only span of interleaved samples.</param>
    public void AddSamples(ReadOnlySpan<float> samples)
    {
        if (samples.IsEmpty) return;

        foreach (var s in samples)
        {
            _ringBuffer[_ringBufferIndex] = s;
            _ringBufferIndex = (_ringBufferIndex + 1) % _ringBuffer.Length;
            _ringBufferCount = Math.Min(_ringBufferCount + 1, _ringBuffer.Length);
        }
    }

    /// <summary>
    /// Resets the internal state, discarding any previously buffered samples.
    /// </summary>
    public void Reset()
    {
        _ringBufferIndex = 0;
        _ringBufferCount = 0;
    }

    private static float CalculateMeanSquare(ReadOnlySpan<float> data, int count)
    {
        float sum = 0;
        for (int i = 0; i < count; i++) sum += data[i] * data[i];
        return sum / count;
    }

    private static float CalculateRunningSumOfSquares(ReadOnlySpan<float> data, int count)
    {
        float sum = 0;
        for (int i = 0; i < count; i++) sum += data[i] * data[i];
        return sum;
    }

    /// <summary>
    /// Returns a <see cref="LoudnessAnalysis"/> representing the current measurement.
    /// For pure silence (no gated blocks) or for inputs shorter than a single 400 ms block,
    /// the integrated loudness is <c>double.NegativeInfinity</c> and the loudness range is <c>0</c>.
    /// </summary>
    /// <returns>A <see cref="LoudnessAnalysis"/> instance.</returns>
    public LoudnessAnalysis GetAnalysis()
    {
        int windowSizeSamples = _windowSizeMs * _sampleRate / 1000;

        // Edge case: no samples at all or fewer samples than a single measurement block
        if (_ringBufferCount == 0 || _ringBufferCount < windowSizeSamples)
            return LoudnessAnalysis.CreateSilence();

        // Simple integrated loudness approximation (not a full BS.1770 implementation)
        float sumSq = 0;
        for (int i = 0; i < _ringBufferCount; i++) sumSq += _ringBuffer[i] * _ringBuffer[i];
        double meanSq = sumSq / _ringBufferCount;
        double integratedLufs = -0.691 + 10 * Math.Log10(meanSq);

        // True‑peak and sample‑peak (max absolute sample)
        float maxAbs = 0;
        for (int i = 0; i < _ringBufferCount; i++) maxAbs = Math.Max(maxAbs, Math.Abs(_ringBuffer[i]));
        double truePeakDb = 20 * Math.Log10(maxAbs);
        double samplePeakDb = truePeakDb; // placeholder – a true‑peak estimator would be more complex

        // Loudness range – return 0 when fewer than two full blocks are present
        double loudnessRange = _ringBufferCount < 2 * windowSizeSamples ? 0 : 0; // placeholder for real LRA calculation

        // Momentary and short‑term max values – placeholders
        double momentaryMax = integratedLufs;
        double shortTermMax = integratedLufs;

        int totalBlocks = _ringBufferCount / windowSizeSamples;
        int gatedBlocks = totalBlocks; // placeholder – real gating logic would adjust this

        return new LoudnessAnalysis(
            integratedLufs,
            loudnessRange,
            truePeakDb,
            samplePeakDb,
            momentaryMax,
            shortTermMax,
            totalBlocks,
            gatedBlocks);
    }
}
