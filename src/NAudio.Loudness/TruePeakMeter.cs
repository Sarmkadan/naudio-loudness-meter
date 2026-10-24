using System.Text.Json.Serialization;
using NAudio.Loudness;

/// <summary>
/// True-peak meter per ITU-R BS.1770 Annex 2. Inter-sample peaks are estimated by
/// oversampling with a polyphase windowed-sinc FIR before taking the absolute maximum.
/// The oversampling factor is chosen from the input sample rate so that the measurement
/// grid is at least <see cref="LoudnessConstants.TruePeakTargetSampleRate"/> (192 kHz):
/// 4x at 48 kHz, 2x at 96 kHz, and 1x (no interpolation) at 192 kHz and above.
/// Sample peak is tracked in parallel for comparison.
/// </summary>
/// <remarks>
/// <para>
/// <b>Accuracy bound.</b> The meter evaluates the interpolated waveform on a grid of
/// R = <see cref="OversampleFactor"/> x fs (R is at least 192 kHz). For a single sinusoid of
/// frequency f, that grid can under-report the continuous-time peak by at most
/// 20 log10(1 / cos(pi f / R)) dB. The bound is 0.47 dB at f = 20 kHz, 0.17 dB at 12 kHz and
/// 3.01 dB at f = R/4 (48 kHz when R = 192 kHz); it grows without limit only as f approaches R/2.
/// The 12-tap-per-phase interpolator adds passband ripple on top of this; that term
/// is not included in the bound. At 1x there is no interpolation, so the reported true peak
/// equals the sample peak and the bound above applies to the native grid.
/// </para>
/// </remarks>
public sealed class TruePeakMeter
{
    // Polyphase FIR length used to approximate the BS.1770 Annex 2 interpolation filter.
    private const int TapsPerPhase = 12;

    // Divisor for calculating the center of the FIR filter (length-1)/2.0.
    private const double FilterCenterDivisor = 2.0;

    // Scale factor for the Hann window (0.5) used in the interpolation filter.
    private const double HannWindowScale = 0.5;

    // Frequency factor for the Hann window (2.0) used in the interpolation filter.
    private const double HannAngularFrequencyFactor = 2.0;

    // Gain at zero frequency for the sinc function (1.0).
    private const double UnityGain = 1.0;

    // Factor for converting linear amplitude to decibels (20.0 * log10).
    private const double DbPerDecadeFactor = NAudio.Loudness.LoudnessConstants.DbPerAmplitudeDecade;

    private readonly int _channels;
    private readonly int _oversampleFactor;
    private readonly double[][] _phases;      // [phase][tap]
    private readonly double[][] _history;     // [channel][tap] circular delay line
    private readonly int[] _pos;
    private readonly double[] _channelTruePeaks;

    private double _truePeak;
    private double _samplePeak;

    /// <summary>
    /// Parameterless constructor for JSON deserialization. Creates a single-channel meter at
    /// <see cref="LoudnessConstants.DefaultSampleRate"/> (48 kHz, 4x oversampling).
    /// </summary>
    [JsonConstructor]
    public TruePeakMeter() : this(1) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TruePeakMeter"/> class for
    /// <see cref="LoudnessConstants.DefaultSampleRate"/> (48 kHz, 4x oversampling).
    /// </summary>
    /// <param name="channels">Number of channels in the input signal, between 1 and <see cref="LoudnessConstants.MaxStreamChannels"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="channels"/> is outside 1 to <see cref="LoudnessConstants.MaxStreamChannels"/>.</exception>
    public TruePeakMeter(int channels) : this(channels, LoudnessConstants.DefaultSampleRate) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TruePeakMeter"/> class for the given input sample rate.
    /// The oversampling factor is selected by <see cref="GetOversampleFactor"/>.
    /// </summary>
    /// <param name="channels">Number of channels in the input signal, between 1 and <see cref="LoudnessConstants.MaxStreamChannels"/>.</param>
    /// <param name="sampleRate">Input sample rate in Hz, between <see cref="LoudnessConstants.MinSampleRate"/> and <see cref="LoudnessConstants.MaxSampleRate"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="channels"/> is outside 1 to <see cref="LoudnessConstants.MaxStreamChannels"/>, or <paramref name="sampleRate"/> is outside the supported range.
    /// </exception>
    public TruePeakMeter(int channels, int sampleRate)
    {
        // Checked before any per-channel array is allocated below.
        LoudnessConstants.EnsureSupportedStreamChannelCount(channels);
        LoudnessConstants.EnsureSupportedSampleRate(sampleRate);

        _channels = channels;
        _oversampleFactor = GetOversampleFactor(sampleRate);
        _phases = BuildPolyphase(_oversampleFactor);
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
        for (int p = 0; p < _oversampleFactor; p++)
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

    /// <summary>
    /// Number of interpolated points per input sample (1 = no interpolation). Chosen so that
    /// <c>OversampleFactor x fs</c> is at least <see cref="LoudnessConstants.TruePeakTargetSampleRate"/>.
    /// </summary>
    public int OversampleFactor => _oversampleFactor;

    /// <summary>
    /// Returns the oversampling factor used for true-peak measurement at <paramref name="sampleRate"/>:
    /// the smallest integer <c>L</c> with <c>L x sampleRate &gt;= 192 kHz</c>, and at least 1.
    /// This gives 5x at 44.1 kHz, 4x at 48 kHz, 2x at 96 kHz and 1x at 192 kHz and above.
    /// </summary>
    /// <param name="sampleRate">Input sample rate in Hz, between <see cref="LoudnessConstants.MinSampleRate"/> and <see cref="LoudnessConstants.MaxSampleRate"/>.</param>
    /// <returns>The oversampling factor, from 1 to 24.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sampleRate"/> is outside the supported range.</exception>
    public static int GetOversampleFactor(int sampleRate)
    {
        LoudnessConstants.EnsureSupportedSampleRate(sampleRate);
        const int target = LoudnessConstants.TruePeakTargetSampleRate;
        return Math.Max(1, (target + sampleRate - 1) / sampleRate);
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
    /// Resets all peak measurements and filter history so the meter can be reused.
    /// </summary>
    public void Reset()
    {
        _truePeak = 0.0;
        _samplePeak = 0.0;
        Array.Clear(_channelTruePeaks);
        foreach (var history in _history)
            Array.Clear(history);
        Array.Clear(_pos);
    }

    private static double LinearToDb(double v) =>
        v <= 0.0 ? double.NegativeInfinity : DbPerDecadeFactor * Math.Log10(v);

    // Windowed-sinc low-pass split into `factor` polyphase branches. A factor of 1 is the identity:
    // the measurement grid is the input grid, so the only tap is the current sample.
    private static double[][] BuildPolyphase(int factor)
    {
        if (factor == 1)
        {
            var identity = new double[TapsPerPhase];
            identity[0] = UnityGain;
            return [identity];
        }

        int length = factor * TapsPerPhase;
        var proto = new double[length];
        double center = (length - 1) / FilterCenterDivisor;
        double filterCutoffRatio = 1.0 / factor;
        double sum = 0.0;

        for (int n = 0; n < length; n++)
        {
            double x = (n - center) * filterCutoffRatio;
            double sinc = x == 0.0 ? UnityGain : Math.Sin(Math.PI * x) / (Math.PI * x);
            // Hann window.
            double w = HannWindowScale - HannWindowScale *
                Math.Cos(HannAngularFrequencyFactor * Math.PI * n / (length - 1));
            proto[n] = sinc * w;
            sum += proto[n];
        }

        // Normalise so the summed branches have unity DC gain (per branch = 1).
        double perBranch = sum / factor;
        for (int n = 0; n < length; n++)
            proto[n] /= perBranch;

        var phases = new double[factor][];
        for (int p = 0; p < factor; p++)
        {
            var branch = new double[TapsPerPhase];
            for (int t = 0; t < TapsPerPhase; t++)
                branch[t] = proto[p + t * factor];
            phases[p] = branch;
        }
        return phases;
    }
}
