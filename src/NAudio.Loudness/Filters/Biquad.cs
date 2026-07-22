namespace NAudio.Loudness.Filters;

/// <summary>
/// Direct-form I biquad section operating on <see cref="double"/> samples.
/// State is kept per instance, so one section maps to exactly one channel.
/// </summary>
public sealed class Biquad
{
    private readonly double _b0, _b1, _b2, _a1, _a2;
    private double _x1, _x2, _y1, _y2;

    /// <param name="b0">Feed-forward coefficients (already normalised by a0).</param>
    /// <param name="a1">Feed-back coefficients (already normalised by a0).</param>
    public Biquad(double b0, double b1, double b2, double a1, double a2)
    {
        _b0 = b0;
        _b1 = b1;
        _b2 = b2;
        _a1 = a1;
        _a2 = a2;
    }

    public double Process(double x)
    {
        double y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
        _x2 = _x1;
        _x1 = x;
        _y2 = _y1;
        _y1 = y;
        return y;
    }

    public void Reset()
    {
        _x1 = _x2 = _y1 = _y2 = 0.0;
    }
}
