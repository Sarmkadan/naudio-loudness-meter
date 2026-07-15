using NAudio.Wave;

namespace NAudio.Loudness.Tests;

/// <summary>Minimal in-memory <see cref="ISampleProvider"/> over an interleaved float buffer.</summary>
internal sealed class ArraySampleProvider : ISampleProvider
{
    private readonly float[] _data;
    private int _position;

    public ArraySampleProvider(float[] data, int sampleRate, int channels)
    {
        _data = data;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        int remaining = _data.Length - _position;
        int n = Math.Min(remaining, count);
        Array.Copy(_data, _position, buffer, offset, n);
        _position += n;
        return n;
    }
}
