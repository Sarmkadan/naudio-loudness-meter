using NAudio.Wave;

namespace NAudio.Loudness;

/// <summary>
/// Applies a fixed loudness-normalization gain to a source, optionally clamping
/// the output so the estimated true peak never exceeds a ceiling (a hard
/// brick-wall clip, not a look-ahead limiter - see the README for the trade-off).
/// </summary>
public sealed class LoudnessNormalizingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _gain;
    private readonly float _ceilingLinear;
    private readonly bool _limit;

    /// <param name="source">Audio to normalize.</param>
    /// <param name="gainDb">Gain to apply, typically <c>targetLufs - measuredLufs</c>.</param>
    /// <param name="truePeakCeilingDb">
    /// If non-null, output samples are clamped to this level (in dBTP-approximate
    /// dBFS terms). EBU R128 recommends -1 dBTP.
    /// </param>
    public LoudnessNormalizingSampleProvider(ISampleProvider source, double gainDb, double? truePeakCeilingDb = -1.0)
    {
        _source = source;
        _gain = (float)Math.Pow(10.0, gainDb / 20.0);
        _limit = truePeakCeilingDb.HasValue;
        _ceilingLinear = _limit ? (float)Math.Pow(10.0, truePeakCeilingDb!.Value / 20.0) : 1.0f;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        for (int i = 0; i < read; i++)
        {
            float v = buffer[offset + i] * _gain;
            if (_limit)
            {
                if (v > _ceilingLinear) v = _ceilingLinear;
                else if (v < -_ceilingLinear) v = -_ceilingLinear;
            }
            buffer[offset + i] = v;
        }
        return read;
    }
}
