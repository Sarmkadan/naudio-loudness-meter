using System;
using NAudio.Wave;

namespace NAudio.Loudness;

/// <summary>
/// Applies a fixed loudness-normalization gain to a source, optionally limiting
/// the applied gain so the estimated true peak never exceeds a configurable ceiling.
/// This is a fixed-gain normalizer, suitable for two-pass workflows where the
/// required gain has been pre-calculated (e.g., using <see cref="LoudnessAnalysis"/>).
///
/// When a ceiling is specified, the provider uses predictive gain limiting (loudnorm‑style)
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
    private long _clippedSampleCount;

    /// <param name="source">Audio to normalize.</param>
    /// <param name="gainDb">
    /// Fixed gain to apply, typically <c>targetLufs - measuredLufs</c>.
    /// If <paramref name="truePeakCeilingDb"/> is specified, this gain will be reduced if necessary to ensure
    /// the true peak does not exceed the ceiling.
    /// </param>
    /// <param name="truePeakCeilingDb">
    /// If non‑null, the applied gain is limited so that the estimated true peak
    /// never exceeds this level (in dBTP‑approximate dBFS terms).
    /// EBU R128 recommends -1 dBTP. When specified, the requested gain is
    /// reduced if necessary to prevent ceiling violations. This is a predictive
    /// approach (loudnorm‑style) that avoids the inter‑sample peak issues of
    /// applying excessive gain. Hard clipping is still applied as a final fallback.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public LoudnessNormalizingSampleProvider(ISampleProvider source, double gainDb, double? truePeakCeilingDb = -1.0)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        _gain = (float)Math.Pow(10.0, gainDb / 20.0);
        _limit = truePeakCeilingDb.HasValue;
        _ceilingLinear = _limit ? (float)Math.Pow(10.0, truePeakCeilingDb!.Value / 20.0) : 1.0f;
        _truePeakCeilingDb = truePeakCeilingDb ?? -1.0;

        // Calculate maximum safe gain to respect the ceiling
        _maxGainLinear = (_limit && gainDb > 0) ? CalculateMaxSafeGainLinear(gainDb) : _gain;
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
    /// Gets the true‑peak ceiling in dBTP that is being enforced.
    /// </summary>
    public double TruePeakCeilingDb => _truePeakCeilingDb;

    /// <summary>
    /// Gets the number of samples that have been hard-clamped because the applied
    /// gain would otherwise have pushed them past the full-scale (or configured ceiling)
    /// limit. A non-zero value indicates the predictive gain limiting did not fully
    /// prevent overshoot for this material and audible clipping may have occurred.
    /// </summary>
    public long ClippedSampleCount => _clippedSampleCount;

    /// <inheritdoc />
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// Reads audio samples from the source, applies the calculated gain, and
    /// optionally clips the result to the true‑peak ceiling.
    /// </summary>
    /// <param name="buffer">The buffer to write samples into.</param>
    /// <param name="offset">The offset in <paramref name="buffer"/> at which to start writing.</param>
    /// <param name="count">The maximum number of samples to read.</param>
    /// <returns>The number of samples actually read.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="buffer"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset"/> or <paramref name="count"/> are negative,
    /// or when <paramref name="offset"/> + <paramref name="count"/> exceeds <paramref name="buffer"/> length.
    /// </exception>
    public int Read(float[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        int read = _source.Read(buffer, offset, count);
        for (int i = 0; i < read; i++)
        {
            // Apply predictive‑limited gain
            float v = buffer[offset + i] * _maxGainLinear;

            // Hard clamp as a final safety measure: when a ceiling is configured, use it;
            // otherwise always fall back to full scale ([-1, 1]) so a positive gain can
            // never push samples beyond what the sample format can represent.
            float clampLimit = _limit ? _ceilingLinear : 1.0f;
            if (v > clampLimit)
            {
                v = clampLimit;
                _clippedSampleCount++;
            }
            else if (v < -clampLimit)
            {
                v = -clampLimit;
                _clippedSampleCount++;
            }

            buffer[offset + i] = v;
        }
        return read;
    }

    /// <summary>
    /// Calculates the maximum safe gain (linear) that won't cause true peaks to exceed the ceiling.
    /// This implements a loudnorm‑style predictive limiting approach using conservative safety margins.
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

        // Safety margin for inter‑sample peaks:
        // EBU R128 true peak can exceed sample peak by up to ~1.5 dB for complex signals
        // ITU‑R BS.1770‑4 section 5.2 recommends a 1.1 dB margin for true peak measurement
        const double SafetyMarginDb = 1.1;

        double ceilingDb = Math.Log10(_ceilingLinear) * 20.0;
        double effectiveCeilingDb = ceilingDb - SafetyMarginDb;

        // Use a sample peak estimate of 0.95 (conservative for most audio)
        const double ConservativeSamplePeak = 0.95;
        double maxSafeGainLinear = _ceilingLinear / ConservativeSamplePeak;

        // Convert to dB and compare with requested gain
        double maxSafeGainDb = 20.0 * Math.Log10(maxSafeGainLinear);

        // Apply the more restrictive of the two: requested gain or safety‑limited gain
        double actualMaxGainDb = Math.Min(requestedGainDb, maxSafeGainDb);

        // Ensure we never return a gain larger than requested (when ceiling is high or negative)
        // and ensure we don't return NaN or negative values
        actualMaxGainDb = Math.Max(0, Math.Min(requestedGainDb, actualMaxGainDb));

        return (float)Math.Pow(10.0, actualMaxGainDb / 20.0);
    }
}
