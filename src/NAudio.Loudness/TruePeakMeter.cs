/// <summary>
/// True-peak meter per ITU-R BS.1770 Annex 2. Inter-sample peaks are estimated
/// by 4x oversampling with a polyphase windowed-sinc FIR before taking the
/// absolute maximum. Sample peak is tracked in parallel for comparison.
/// </summary>
public sealed class TruePeakMeter
{
    // Four-times oversampling follows the true-peak measurement model in BS.1770 Annex 2.
    private const int OversampleFactor = 4;

    // Polyphase FIR length used to approximate the BS.1770 Annex 2 interpolation filter.
    private const int TapsPerPhase = 12;

    // Normalized cutoff frequency for the interpolation filter (1/oversample) as per BS.1770 Annex 2.
    private const double FilterCutoffRatio = 1.0 / OversampleFactor;

    // Divisor for calculating the center of the FIR filter (length-1)/2.0.
    private const double FilterCenterDivisor = 2.0;

    // Scale factor for the Hann window (0.5) used in the interpolation filter.
    private const double HannWindowScale = 0.5;

    // Frequency factor for the Hann window (2.0) used in the interpolation filter.
    private const double HannAngularFrequencyFactor = 2.0;

    // Gain at zero frequency for the sinc function (1.0).
    private const double UnityGain = 1.0;

    // Factor for converting linear amplitude to decibels (20.0 * log10).
    private const double DbPerDecadeFactor = 20.0;

    private readonly int _channels;
    private readonly double[][] _phases;      // [phase][tap]
    private readonly double[][] _history;     // [channel][tap] circular delay line
    private readonly int[] _pos;
    private readonly double[] _channelTruePeaks;

    private double _truePeak;
    private double _samplePeak;

    /// <summary>
    /// Initializes a new instance of the <see cref="TruePeakMeter"/> class.
    /// </summary>
    /// <param name="channels">Number of channels in the input signal. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="channels"/> is less than or equal to zero.</exception>
    public TruePeakMeter(int channels)
    {
        if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
        _channels = channels;
        _phases = BuildPolyphase();
        _history = new double[channels][];
        _pos = new int[channels];
        _channelTruePeaks = new double[channels];
        for (int c = 0; c < channels; c++)
            _history[c] = new double[TapsPerPhase];
    }

    /// <summary>
    /// Processes a block of interleaved samples, updating the peak measurements.
    /// </summary>
    /// <param name="interleaved">Interleaved samples in the range [-1, 1].</param>
    public void AddSamples(ReadOnlySpan<float> interleaved)
    {
        int frames = interleaved.Length / _channels;
        for (int f = 0; f < frames; f++)
        {
            int baseIdx = f * _channels;
            for (int c = 0; c < _channels; c++)
            {
                double x = interleaved[baseIdx + c];
                double a = Math.Abs(x);
                if (a > _samplePeak) _samplePeak = a;
                PushAndEvaluate(c, x);
            }
        }
    }

    private void PushAndEvaluate(int channel, double sample)
    {
        var hist = _history[channel];
        int pos = _pos[channel];
        hist[pos] = sample;
        _pos[channel] = (pos + 1) % TapsPerPhase;

        // Convolve each polyphase branch against the delay line.
        for (int p = 0; p < OversampleFactor; p++)
        {
            var coeffs = _phases[p];
            double acc = 0.0;
            int idx = pos; // most recent sample
            for (int t = 0; t < TapsPerPhase; t++)
            {
                acc += coeffs[t] * hist[idx];
                idx = idx == 0 ? TapsPerPhase - 1 : idx - 1;
            }
            double a = Math.Abs(acc);
            if (a > _truePeak) _truePeak = a;
            if (a > _channelTruePeaks[channel]) _channelTruePeaks[channel] = a;
        }
    }

    /// <summary>Estimated true-peak level in dBTP.</summary>
    public double TruePeakDb => LinearToDb(_truePeak);

    /// <summary>Estimated per-channel true-peak level in dBTP.</summary>
    public IReadOnlyList<double> ChannelPeaksDbtp
    {
        get
        {
            var result = new double[_channels];
            for (int i = 0; i < _channels; i++)
                result[i] = LinearToDb(_channelTruePeaks[i]);
            return result;
        }
    }

    /// <summary>Plain sample-peak level in dBFS.</summary>
    public double SamplePeakDb => LinearToDb(_samplePeak);

    /// <summary>Linear true-peak magnitude (1.0 == 0 dBTP).</summary>
    public double TruePeakLinear => _truePeak;

    /// <summary>
    /// Resets the meter to its initial state, clearing all peak history and filter state.
    /// </summary>
    public void Reset()
    {
        _truePeak = 0.0;
        _samplePeak = 0.0;
        Array.Clear(_pos);
        Array.Clear(_channelTruePeaks);
        foreach (var h in _history) Array.Clear(h);
    }

    private static double LinearToDb(double v) =>
        v <= 0.0 ? double.NegativeInfinity : DbPerDecadeFactor * Math.Log10(v);

    // Windowed-sinc low-pass split into `OversampleFactor` polyphase branches.
    private static double[][] BuildPolyphase()
    {
        int length = OversampleFactor * TapsPerPhase;
        var proto = new double[length];
        double center = (length - 1) / FilterCenterDivisor;
        double sum = 0.0;

        for (int n = 0; n < length; n++)
        {
            double x = (n - center) * FilterCutoffRatio;
            double sinc = x == 0.0 ? UnityGain : Math.Sin(Math.PI * x) / (Math.PI * x);
            // Hann window.
            double w = HannWindowScale - HannWindowScale *
                Math.Cos(HannAngularFrequencyFactor * Math.PI * n / (length - 1));
            proto[n] = sinc * w;
            sum += proto[n];
        }

        // Normalise so the summed branches have unity DC gain (per branch = 1).
        double perBranch = sum / OversampleFactor;
        for (int n = 0; n < length; n++)
            proto[n] /= perBranch;

        var phases = new double[OversampleFactor][];
        for (int p = 0; p < OversampleFactor; p++)
        {
            var branch = new double[TapsPerPhase];
            for (int t = 0; t < TapsPerPhase; t++)
                branch[t] = proto[p + t * OversampleFactor];
            phases[p] = branch;
        }
        return phases;
    }
}
