using NAudio.Wave;

namespace NAudio.Loudness;

/// <summary>
/// Applies adaptive loudness normalization in a single pass.
/// 
/// Note: This is an adaptive implementation that updates gain dynamically based on measured 
/// loudness. Audio passes through at unity gain until an integrated measurement is available,
/// after which gain changes are smoothed to reduce audible fluctuations. It converges to the
/// target loudness over time.
/// </summary>
public sealed class OnePassAdaptiveLoudnessNormalizingSampleProvider : ISampleProvider
{
    private const double MinimumMeasurementSeconds = 0.4;
    private const double GainSmoothingTimeSeconds = 1.0;

    private readonly ISampleProvider _source;
    private readonly double _targetLufs;
    private readonly float _ceilingLinear;
    private readonly LoudnessMeter _meter;
    private readonly bool _limit;
    private readonly long _minimumMeasurementSamples;
    private readonly float _gainSmoothingCoefficient;
    private float _currentGainLinear = 1.0f;
    private long _samplesProcessed;

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
        _minimumMeasurementSamples = (long)Math.Ceiling(
            source.WaveFormat.SampleRate * source.WaveFormat.Channels * MinimumMeasurementSeconds);
        _gainSmoothingCoefficient = (float)(1.0 - Math.Exp(
            -1.0 / (source.WaveFormat.SampleRate * source.WaveFormat.Channels * GainSmoothingTimeSeconds)));
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

        // IntegratedLufs depends on all samples seen so far, so the target gain converges over time.
        // Keep the initial 400 ms at unity because no complete gating block exists before then.
        double currentLufs = _meter.IntegratedLufs;
        bool measurementAvailable = double.IsFinite(currentLufs)
            && _samplesProcessed + read >= _minimumMeasurementSamples;
        float targetGainLinear = measurementAvailable
            ? (float)Math.Pow(10.0, (_targetLufs - currentLufs) / 20.0)
            : 1.0f;

        for (int i = 0; i < read; i++)
        {
            if (measurementAvailable && _samplesProcessed + i >= _minimumMeasurementSamples)
            {
                _currentGainLinear += (targetGainLinear - _currentGainLinear) * _gainSmoothingCoefficient;
            }

            buffer[offset + i] *= _currentGainLinear;
            
            // Hard clipping as final safety measure
            if (_limit)
            {
                if (buffer[offset + i] > _ceilingLinear) buffer[offset + i] = _ceilingLinear;
                else if (buffer[offset + i] < -_ceilingLinear) buffer[offset + i] = -_ceilingLinear;
            }
        }

        _samplesProcessed += read;
        return read;
    }
}
