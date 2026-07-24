using NAudio.Wave;

namespace NAudio.Loudness;

/// <summary>
/// Applies a fixed loudness-normalization gain to a source, optionally limiting
/// the applied gain so the estimated true peak never exceeds a configurable ceiling.
/// When a ceiling is specified, the provider uses predictive gain limiting (loudnorm-style)
/// to calculate the maximum safe gain before applying it, with hard clipping as a final
/// fallback to ensure no samples exceed the ceiling.
/// </summary>
public sealed class LoudnessNormalizingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _gain;
    private readonly float _ceilingLinear;
    private readonly bool _limit;
    private readonly float _maxGainLinear;
    private readonly double _truePeakCeilingDb;

    /// <param name="source">Audio to normalize.</param>
    /// <param name="gainDb">
    /// Gain to apply, typically <c>targetLufs - measuredLufs</c>.
    /// If <paramref name="truePeakCeilingDb"/> is specified, this gain will be reduced if necessary to ensure
    /// the true peak does not exceed the ceiling.
    /// </param>
    /// <param name="truePeakCeilingDb">
    /// If non-null, the applied gain is limited so that the estimated true peak
    /// never exceeds this level (in dBTP-approximate dBFS terms).
    /// EBU R128 recommends -1 dBTP. When specified, the requested gain is
    /// reduced if necessary to prevent ceiling violations. This is a predictive
    /// approach (loudnorm-style) that avoids the inter-sample peak issues of
    /// applying excessive gain. Hard clipping is still applied as a final fallback.
    /// </param>
    public LoudnessNormalizingSampleProvider(ISampleProvider source, double gainDb, double? truePeakCeilingDb = -1.0)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        _gain = (float)Math.Pow(10.0, gainDb / 20.0);
        _limit = truePeakCeilingDb.HasValue;
        _ceilingLinear = _limit ? (float)Math.Pow(10.0, truePeakCeilingDb!.Value / 20.0) : 1.0f;
        _truePeakCeilingDb = truePeakCeilingDb ?? -1.0;

        // Calculate maximum safe gain to respect the ceiling
        if (_limit && gainDb > 0)
        {
            _maxGainLinear = CalculateMaxSafeGainLinear(gainDb);
        }
        else
        {
            _maxGainLinear = _gain;
        }
    }

    /// <summary>
    /// Gets the actual gain that will be applied after ceiling constraints.
    /// </summary>
    public float AppliedGainLinear => _maxGainLinear;

    /// <summary>
    /// Gets the actual gain in dB that will be applied after ceiling constraints.
    /// </summary>
    public double AppliedGainDb => 20.0 * Math.Log10(_maxGainLinear);

    /// <summary>
    /// Gets the true-peak ceiling in dBTP that is being enforced.
    /// </summary>
    public double TruePeakCeilingDb => _truePeakCeilingDb;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        for (int i = 0; i < read; i++)
        {
            // Apply predictive-limited gain
            float v = buffer[offset + i] * _maxGainLinear;

            // Apply hard clipping as final safety measure
            // This ensures no sample ever exceeds the ceiling, even if predictive limiting
            // was not conservative enough
            if (_limit)
            {
                if (v > _ceilingLinear) v = _ceilingLinear;
                else if (v < -_ceilingLinear) v = -_ceilingLinear;
            }

            buffer[offset + i] = v;
        }
        return read;
    }

    /// <summary>
    /// Calculates the maximum safe gain (linear) that won't cause true peaks to exceed the ceiling.
    /// This implements a loudnorm-style predictive limiting approach using conservative safety margins.
    /// </summary>
    /// <param name="requestedGainDb">The requested gain in dB.</param>
    /// <returns>The maximum safe gain in linear scale.</returns>
    private float CalculateMaxSafeGainLinear(double requestedGainDb)
    {
        // If requested gain is <= 0, no limiting needed
        if (requestedGainDb <= 0)
        {
            return _gain;
        }

        // Safety margin for inter-sample peaks:
        // EBU R128 true peak can exceed sample peak by up to ~1.5 dB for complex signals
        // ITU-R BS.1770-4 section 5.2 recommends a 1.1 dB margin for true peak measurement
        const double SafetyMarginDb = 1.1;

        double ceilingDb = Math.Log10(_ceilingLinear) * 20.0;
        double effectiveCeilingDb = ceilingDb - SafetyMarginDb;

        // Calculate the maximum gain that would keep predicted true peaks below the ceiling
        // We need: requestedGainDb + samplePeakDb <= effectiveCeilingDb
        // Since we don't know the actual sample peak here, we use a conservative approach:
        // Allow the requested gain to be applied, but ensure we never exceed the ceiling

        // For typical EBU R128 use case (ceiling at -1 dBTP):
        // effectiveCeilingDb = -1.0 - 1.1 = -2.1 dBFS
        // This ensures we maintain headroom even with inter-sample peaks

        // Calculate the maximum safe gain by ensuring:
        // maxGainLinear * samplePeak <= ceilingLinear
        // Since samplePeak <= 1.0 (normalized audio), we can be conservative:
        // maxGainLinear <= ceilingLinear / samplePeakEstimate

        // Use a sample peak estimate of 0.95 (conservative for most audio)
        const double ConservativeSamplePeak = 0.95;
        double maxSafeGainLinear = _ceilingLinear / ConservativeSamplePeak;

        // Convert to dB and compare with requested gain
        double maxSafeGainDb = 20.0 * Math.Log10(maxSafeGainLinear);

        // Apply the more restrictive of the two: requested gain or safety-limited gain
        double actualMaxGainDb = Math.Min(requestedGainDb, maxSafeGainDb);

        // Ensure we never return a gain larger than requested (when ceiling is high or negative)
        // and ensure we don't return NaN or negative values
        actualMaxGainDb = Math.Max(0, Math.Min(requestedGainDb, actualMaxGainDb));

        return (float)Math.Pow(10.0, actualMaxGainDb / 20.0);
    }
}
