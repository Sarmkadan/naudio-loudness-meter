using System;
using System.Buffers;
using NAudio.Wave;

namespace NAudio.Loudness;

public sealed class LoudnessMeter
{
    private readonly ISampleProvider _source;
    private readonly float _sampleRate;
    private readonly int _windowSizeMs;
    private readonly int _windowCount;
    private readonly float[] _ringBuffer;
    private readonly int _ringBufferIndex;
    private readonly int _ringBufferCount;

    public LoudnessMeter(ISampleProvider source, int sampleRate, int windowSizeMs, int windowCount)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sampleRate);
        ArgumentNullException.ThrowIfNull(windowSizeMs);
        ArgumentNullException.ThrowIfNull(windowCount);

        _source = source;
        _sampleRate = sampleRate;
        _windowSizeMs = windowSizeMs;
        _windowCount = windowCount;
        _ringBuffer = new float[_windowSizeMs * _sampleRate / 1000];
        _ringBufferIndex = 0;
        _ringBufferCount = 0;
    }

    public void AddSamples(ReadOnlySpan<float> interleaved)
    {
        int read = _source.Read(interleaved);
        for (int i = 0; i < read; i++)
        {
            _ringBuffer[_ringBufferIndex] = interleaved[i];
            _ringBufferIndex = (_ringBufferIndex + 1) % _ringBuffer.Length;
            _ringBufferCount = Math.Min(_ringBufferCount + 1, _windowSizeMs * _sampleRate / 1000);
        }
    }

    public void Reset()
    {
        _ringBufferIndex = 0;
        _ringBufferCount = 0;
    }

    private float CalculateMeanSquare(ReadOnlySpan<float> data, int count)
    {
        float sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += data[i] * data[i];
        }
        return sum / count;
    }

    private float CalculateRunningSumOfSquares(ReadOnlySpan<float> data, int count)
    {
        float sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += data[i] * data[i];
        }
        return sum;
    }

    public double ComputeLoudnessRange()
    {
        float[] windowData = new float[_windowSizeMs * _sampleRate / 1000];
        int windowCount = 0;
        for (int i = 0; i < _ringBufferCount; i++)
        {
            windowData[windowCount] = _ringBuffer[i];
            windowCount++;
            if (windowCount >= _windowSizeMs * _sampleRate / 1000)
            {
                break;
            }
        }
        float meanSquare = CalculateMeanSquare(windowData, windowCount);
        float runningSumOfSquares = CalculateRunningSumOfSquares(windowData, windowCount);
        // ... rest of the implementation ...
    }
}
