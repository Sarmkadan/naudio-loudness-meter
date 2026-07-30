namespace NAudio.Loudness.Filters;

/// <summary>
/// Implements the ITU‑R BS.1770 K‑weighting filter, which consists of a high‑shelf
/// (pre‑filter modelling the acoustic effect of the head) followed by an RLB high‑pass
/// filter. The coefficients are re‑derived for the actual sample rate via the bilinear
/// transform of the 48 kHz analogue prototype, ensuring correct metering at common
/// sample rates such as 44.1 kHz, 48 kHz, 96 kHz, etc.
/// </summary>
public sealed class KWeightingFilter
{
    private readonly Biquad _shelf;
    private readonly Biquad _highpass;

    /// <summary>
    /// Initializes a new instance of <see cref="KWeightingFilter"/> for the specified sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate, in Hz, for which the filter coefficients will be calculated.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sampleRate"/> is less than or equal to zero.
    /// </exception>
    public KWeightingFilter(int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);
        _shelf = BuildShelf(sampleRate);
        _highpass = BuildHighPass(sampleRate);
    }

    /// <summary>
    /// Processes a single sample through the cascade of the shelf and high‑pass stages.
    /// </summary>
    /// <param name="sample">The input sample.</param>
    /// <returns>The filtered sample.</returns>
    public double Process(double sample)
    {
        // Use locals to avoid repeated field access for each sample.
        var shelf = _shelf;
        var highpass = _highpass;
        return highpass.Process(shelf.Process(sample));
    }

    /// <summary>
    /// Processes an array of samples through the filter. The filter state is kept in the
    /// underlying <see cref="Biquad"/> instances, but the references to those instances are
    /// stored in locals for the duration of the loop to minimise field‑access overhead.
    /// </summary>
    /// <param name="samples">The input samples.</param>
    /// <returns>An array containing the filtered samples.</returns>
    public double[] ProcessSamples(double[] samples)
    {
        if (samples is null) throw new ArgumentNullException(nameof(samples));

        var shelf = _shelf;
        var highpass = _highpass;
        var output = new double[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            output[i] = highpass.Process(shelf.Process(samples[i]));
        }

        return output;
    }

    /// <summary>
    /// Resets both filter stages to their initial state.
    /// </summary>
    public void Reset()
    {
        _shelf.Reset();
        _highpass.Reset();
    }

    // Stage 1 – high shelf ("pre‑filter" modelling the acoustic effect of the head).
    private static Biquad BuildShelf(int fs)
    {
        const double f0 = 1681.974450955533;
        const double g = 3.999843853973347;
        const double q = 0.7071752369554196;

        double k = 2.0 * Math.Tan(Math.PI * f0 / fs);
        double vh = Math.Pow(10.0, g / 20.0);
        double vb = Math.Pow(vh, 0.4996667741545416);
        double denom = 1.0 + k / q + k * k;

        double b0 = (vh + vb * k / q + k * k) / denom;
        double b1 = 2.0 * (k * k - vh) / denom;
        double b2 = (vh - vb * k / q + k * k) / denom;
        double a1 = 2.0 * (k * k - 1.0) / denom;
        double a2 = (1.0 - k / q + k * k) / denom;

        return new Biquad(b0, b1, b2, a1, a2);
    }

    // Stage 2 – RLB high‑pass.
    private static Biquad BuildHighPass(int fs)
    {
        const double f0 = 38.13547087602444;
        const double q = 0.5003270373238773;

        double k = 2.0 * Math.Tan(Math.PI * f0 / fs);
        double denom = 1.0 + k / q + k * k;

        double a1 = 2.0 * (k * k - 1.0) / denom;
        double a2 = (1.0 - k / q + k * k) / denom;

        // Numerator of a pure high‑pass is (1, -2, 1), normalised by the same denominator.
        return new Biquad(1.0, -2.0, 1.0, a1, a2);
    }
}
