namespace NAudio.Loudness.Filters;

/// <summary>
/// ITU-R BS.1770 K-weighting: a high-frequency "head" shelving filter followed
/// by an RLB high-pass. Coefficients are re-derived for the actual sample rate
/// via the bilinear transform of the 48 kHz analogue prototype, so metering
/// stays valid at 44.1 kHz, 96 kHz, etc. (the fixed tables in the spec are only
/// exact at 48 kHz).
/// </summary>
public sealed class KWeightingFilter
{
    private readonly Biquad _shelf;
    private readonly Biquad _highpass;

    public KWeightingFilter(int sampleRate)
    {
        _shelf = BuildShelf(sampleRate);
        _highpass = BuildHighPass(sampleRate);
    }

    public double Process(double sample) => _highpass.Process(_shelf.Process(sample));

    public void Reset()
    {
        _shelf.Reset();
        _highpass.Reset();
    }

    // Stage 1 - high shelf ("pre-filter" modelling the acoustic effect of the head).
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

    // Stage 2 - RLB high-pass.
    private static Biquad BuildHighPass(int fs)
    {
        const double f0 = 38.13547087602444;
        const double q = 0.5003270373238773;

        double k = 2.0 * Math.Tan(Math.PI * f0 / fs);
        double denom = 1.0 + k / q + k * k;

        double a1 = 2.0 * (k * k - 1.0) / denom;
        double a2 = (1.0 - k / q + k * k) / denom;

        // Numerator of a pure high-pass is (1, -2, 1), normalised by the same denom.
        return new Biquad(1.0, -2.0, 1.0, a1, a2);
    }
}
