namespace NAudio.Loudness;

/// <summary>
/// True-peak meter per ITU-R BS.1770 Annex 2. Inter-sample peaks are estimated
/// by 4x oversampling with a polyphase windowed-sinc FIR before taking the
/// absolute maximum. Sample peak is tracked in parallel for comparison.
/// </summary>
public sealed class TruePeakMeter
{
    private const int Oversample = 4;
    private const int TapsPerPhase = 12;

    private readonly int _channels;
    private readonly double[][] _phases;      // [phase][tap]
    private readonly double[][] _history;     // [channel][tap] circular delay line
    private readonly int[] _pos;
    private readonly double[] _channelTruePeaks;

    private double _truePeak;
    private double _samplePeak;

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
        for (int p = 0; p < Oversample; p++)
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

    public void Reset()
    {
        _truePeak = 0.0;
        _samplePeak = 0.0;
        Array.Clear(_pos);
        Array.Clear(_channelTruePeaks);
        foreach (var h in _history) Array.Clear(h);
    }

    private static double LinearToDb(double v) =>
        v <= 0.0 ? double.NegativeInfinity : 20.0 * Math.Log10(v);

    // Windowed-sinc low-pass split into `Oversample` polyphase branches.
    private static double[][] BuildPolyphase()
    {
        int length = Oversample * TapsPerPhase;
        var proto = new double[length];
        double center = (length - 1) / 2.0;
        double sum = 0.0;

        for (int n = 0; n < length; n++)
        {
            double x = (n - center) / Oversample;
            double sinc = x == 0.0 ? 1.0 : Math.Sin(Math.PI * x) / (Math.PI * x);
            // Hann window.
            double w = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * n / (length - 1));
            proto[n] = sinc * w;
            sum += proto[n];
        }

        // Normalise so the summed branches have unity DC gain (per branch = 1).
        double perBranch = sum / Oversample;
        for (int n = 0; n < length; n++)
            proto[n] /= perBranch;

        var phases = new double[Oversample][];
        for (int p = 0; p < Oversample; p++)
        {
            var branch = new double[TapsPerPhase];
            for (int t = 0; t < TapsPerPhase; t++)
                branch[t] = proto[p + t * Oversample];
            phases[p] = branch;
        }
        return phases;
    }
}
