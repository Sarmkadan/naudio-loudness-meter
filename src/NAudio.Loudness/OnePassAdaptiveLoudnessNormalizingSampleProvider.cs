using NAudio.Wave;

namespace NAudio.Loudness;

/// <summary>
/// Applies adaptive loudness normalization in a single pass.
/// 
/// Note: This is an adaptive implementation that updates gain dynamically based on measured 
/// loudness. It may introduce gain fluctuations during the stream. It converges to the 
/// target loudness over time.
/// </summary>
public sealed class OnePassAdaptiveLoudnessNormalizingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly double _targetLufs;
    private readonly float _ceilingLinear;
    private readonly LoudnessMeter _meter;
    private readonly bool _limit;

    /// <summary>
    /// Initializes a new instance of <see cref="OnePassAdaptiveLoudnessNormalizingSampleProvider"/>.
    /// </summary>
    /// <param name="source">The source provider.</param>
    /// <param name="targetLufs">The target integrated loudness in LUFS.</param>
    /// <param name="truePeakCeilingDb">The optional true-peak ceiling in dBTP.</param>
    public OnePassAdaptiveLoudnessNormalizingSampleProvider(ISampleProvider source, double targetLufs, double? truePeakCeilingDb = -1.0)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _targetLufs = targetLufs;
        _limit = truePeakCeilingDb.HasValue;
        _ceilingLinear = _limit ? (float)Math.Pow(10.0, truePeakCeilingDb!.Value / 20.0) : 1.0f;
        _meter = new LoudnessMeter(source.WaveFormat.SampleRate, source.WaveFormat.Channels);
    }

    /// <inheritdoc/>
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <inheritdoc/>
    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        if (read == 0) return 0;
        
        var span = buffer.AsSpan(offset, read);
        _meter.AddSamples(span);
        
        // Simple adaptive logic: adjust gain based on current integrated loudness
        // Note: IntegratedLufs depends on all samples seen so far, so it converges over time.
        // This is a basic implementation.
        double currentLufs = _meter.IntegratedLufs;
        double gainDb = _targetLufs - currentLufs;
        float gainLinear = (float)Math.Pow(10.0, gainDb / 20.0);
        
        for (int i = 0; i < read; i++)
        {
            buffer[offset + i] *= gainLinear;
            
            // Hard clipping as final safety measure
            if (_limit)
            {
                if (buffer[offset + i] > _ceilingLinear) buffer[offset + i] = _ceilingLinear;
                else if (buffer[offset + i] < -_ceilingLinear) buffer[offset + i] = -_ceilingLinear;
            }
        }
        
        return read;
    }
}
