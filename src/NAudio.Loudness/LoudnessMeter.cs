using NAudio.Loudness.Filters;
namespace NAudio.Loudness;

/// <summary>
/// Incremental EBU R128 / BS.1770 loudness meter. Feed it interleaved float
/// samples (as produced by NAudio's <c>ISampleProvider</c>) and query
/// momentary, short-term and gated integrated loudness at any time.
/// </summary>
/// <remarks>
/// The meter keeps a sliding history of 100 ms sub-blocks. Momentary loudness
/// covers the last 400 ms, short-term the last 3 s, and the integrated value
/// applies the two-stage gating (absolute -70 LUFS, then relative -10 LU)
/// described in the standard.
/// </remarks>
public sealed class LoudnessMeter
{
    // Magic numbers moved to LoudnessConstants
    private const double SubBlockSeconds = LoudnessConstants.SubBlockSeconds;
    private const double Offset = LoudnessConstants.LoudnessOffsetLkfs;

    private readonly int _channels;
    private readonly double[] _weights;
    private readonly KWeightingFilter[] _filters;
    private readonly int _subBlockSize;
    private readonly int _momentaryBlocks;   // 400 ms by default
    private readonly int _shortTermBlocks;   // 3 s by default
    private readonly double _absoluteGateLufs;
    private readonly double _relativeGateLu;

    // Current (partial) 100 ms sub-block.
    private readonly double[] _sumSquares;
    private int _subBlockSampleCount;

    // Per-channel mean-square of the most recent completed sub-blocks.
    private readonly double[] _subBlocks;
    private int _subBlockStart;
    private int _completedSubBlockCount;

    // Short-term loudness history is retained for whole-program LRA.
    private readonly List<double> _shortTermLoudness = new();

    // Channel-weighted energy of every completed 400 ms gating block that
    // passed the absolute gate, together with its loudness.
    private readonly List<double[]> _gatingBlockEnergy = new();

    // Total count of 400ms blocks that entered the gating computation
    private int _totalBlockCount = 0;

    /// <summary>Initializes a loudness meter using the default weights for the channel count.</summary>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <param name="channels">The number of interleaved audio channels.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="sampleRate"/> or <paramref name="channels"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// No default channel weights are available for <paramref name="channels"/>.
    /// </exception>
    public LoudnessMeter(int sampleRate, int channels)
        : this(sampleRate, channels, ChannelWeights.ForChannelCount(channels))
    {
    }

    /// <summary>Initializes a loudness meter from validated options.</summary>
    /// <param name="options">The meter configuration. Its defaults are the BS.1770 / EBU R 128 values.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is outside its allowed range. See <see cref="LoudnessMeterOptions.Validate"/>.</exception>
    /// <exception cref="ArgumentException">Two related options are inconsistent. See <see cref="LoudnessMeterOptions.Validate"/>.</exception>
    public LoudnessMeter(LoudnessMeterOptions options)
        : this(options, ValidatedChannelWeights(options))
    {
    }

    private LoudnessMeter(LoudnessMeterOptions options, double[] channelWeights)
        : this(options.SampleRate, options.Channels, channelWeights,
               WindowBlocks(options.MomentaryWindowMs), WindowBlocks(options.ShortTermWindowMs),
               options.AbsoluteGateLufs, options.RelativeGateLu)
    {
    }

    private static double[] ValidatedChannelWeights(LoudnessMeterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return ChannelWeights.ForChannelCount(options.Channels);
    }

    // Converts a window length to whole 100 ms sub-blocks, with a minimum of one.
    private static int WindowBlocks(int windowMs) =>
        Math.Max(1, (int)Math.Round(windowMs / (SubBlockSeconds * 1000.0)));

    /// <summary>Initializes a loudness meter using the specified channel weights.</summary>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <param name="channels">The number of interleaved audio channels.</param>
    /// <param name="channelWeights">The loudness weight for each channel.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="sampleRate"/> or <paramref name="channels"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="channelWeights"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="channelWeights"/> does not contain one finite, non-negative weight per channel.
    /// </exception>
    public LoudnessMeter(int sampleRate, int channels, double[] channelWeights)
        : this(sampleRate, channels, channelWeights,
               LoudnessConstants.MomentaryBlocks, LoudnessConstants.ShortTermBlocks,
               LoudnessConstants.AbsoluteGateLufs, LoudnessConstants.RelativeGateLu)
    {
    }

    // Shared core for every public constructor. Takes window lengths in sub-blocks and
    // does not apply LoudnessMeterOptions' channel range, so the legacy constructors
    // keep accepting layouts such as 5.1 (6 channels).
    private LoudnessMeter(int sampleRate, int channels, double[] channelWeights,
                          int momentaryBlocks, int shortTermBlocks,
                          double absoluteGateLufs, double relativeGateLu)
    {
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
        ArgumentNullException.ThrowIfNull(channelWeights);
        if (channelWeights.Length != channels)
            throw new ArgumentException("Weight vector length must match channel count.", nameof(channelWeights));
        if (channelWeights.Any(weight => !double.IsFinite(weight) || weight < 0.0))
            throw new ArgumentException("Channel weights must be finite and non-negative.", nameof(channelWeights));

        SampleRate = sampleRate;
        _channels = channels;
        _weights = (double[])channelWeights.Clone();
        _subBlockSize = (int)Math.Round(sampleRate * SubBlockSeconds);
        _filters = new KWeightingFilter[channels];
        for (int c = 0; c < channels; c++)
            _filters[c] = new KWeightingFilter(sampleRate);
        _momentaryBlocks = momentaryBlocks;
        _shortTermBlocks = shortTermBlocks;
        _absoluteGateLufs = absoluteGateLufs;
        _relativeGateLu = relativeGateLu;
        _sumSquares = new double[channels];
        _subBlocks = new double[shortTermBlocks * channels];
    }

    public int SampleRate { get; }

    /// <summary>Feeds a block of interleaved samples in the range [-1, 1].</summary>
    /// <param name="interleaved">The samples, ordered by frame and then by channel.</param>
    /// <exception cref="ArgumentException">
    /// The sample count is not a multiple of the channel count.
    /// </exception>
    public void AddSamples(ReadOnlySpan<float> interleaved)
    {
        if (interleaved.Length % _channels != 0)
            throw new ArgumentException("Sample count must be a multiple of channel count.", nameof(interleaved));

        int frames = interleaved.Length / _channels;
        for (int f = 0; f < frames; f++)
        {
            int baseIdx = f * _channels;
            for (int c = 0; c < _channels; c++)
            {
                double weighted = _filters[c].Process(interleaved[baseIdx + c]);
                _sumSquares[c] += weighted * weighted;
            }

            if (++_subBlockSampleCount == _subBlockSize)
                CompleteSubBlock();
        }
    }

    private void CompleteSubBlock()
    {
        int destinationBlock;
        if (_completedSubBlockCount < _shortTermBlocks)
        {
            destinationBlock = (_subBlockStart + _completedSubBlockCount) % _shortTermBlocks;
            _completedSubBlockCount++;
        }
        else
        {
            destinationBlock = _subBlockStart;
            _subBlockStart = (_subBlockStart + 1) % _shortTermBlocks;
        }

        int destination = destinationBlock * _channels;
        for (int c = 0; c < _channels; c++)
        {
            _subBlocks[destination + c] = _sumSquares[c] / _subBlockSize;
            _sumSquares[c] = 0.0;
        }
        _subBlockSampleCount = 0;

        // A completed sub-block closes one 400 ms gating block. With 75 % overlap the
        // hop between gating blocks is GatingHopBlocks (one sub-block, 100 ms).
        if (_completedSubBlockCount >= _momentaryBlocks)
        {
            var energy = AverageEnergy(_completedSubBlockCount - _momentaryBlocks, _momentaryBlocks);
            _totalBlockCount++;
            if (LoudnessFromEnergy(energy) >= _absoluteGateLufs)
                _gatingBlockEnergy.Add(energy);
        }

        if (_completedSubBlockCount == _shortTermBlocks)
            _shortTermLoudness.Add(LoudnessFromEnergy(AverageEnergy(0, _shortTermBlocks)));
    }

    /// <summary>Loudness of the most recent 400 ms, or -inf if not enough audio yet.</summary>
    public double MomentaryLufs => WindowLoudness(_momentaryBlocks);

    /// <summary>Loudness of the most recent 3 s, or -inf if not enough audio yet.</summary>
    public double ShortTermLufs => WindowLoudness(_shortTermBlocks);

    /// <summary>
    /// Gated integrated loudness over everything fed so far. Returns
    /// <see cref="double.NegativeInfinity"/> when no block clears the gates
    /// (e.g. pure silence).
    /// </summary>
    public double IntegratedLufs
    {
        get
        {
            if (_gatingBlockEnergy.Count == 0)
                return double.NegativeInfinity;

            // Ungated mean over absolute-gated blocks -> relative threshold.
            var mean = MeanEnergy(_gatingBlockEnergy);
            double relativeThreshold = LoudnessFromEnergy(mean) + _relativeGateLu;

            var kept = new List<double[]>();
            foreach (var block in _gatingBlockEnergy)
            {
                if (LoudnessFromEnergy(block) >= relativeThreshold)
                    kept.Add(block);
            }

            if (kept.Count == 0)
                return double.NegativeInfinity;

            return LoudnessFromEnergy(MeanEnergy(kept));
        }
    }

    /// <summary>Loudness range (LRA) in LU per EBU Tech 3342, from short-term history.</summary>
    public double LoudnessRange => ComputeLoudnessRange();

    /// <summary>
    /// Total count of 400ms blocks that entered the gating computation (both passed and failed absolute gate).
    /// </summary>
    public int TotalBlockCount => _totalBlockCount;

    /// <summary>
    /// Count of 400ms blocks that survived the absolute gate (-70 LUFS) and are used for relative gating computation.
    /// </summary>
    public int GatedBlockCount => _gatingBlockEnergy.Count;

    /// <summary>
    /// Resets the internal state, discarding any previously buffered samples.
    /// </summary>
    /// <summary>
    /// Resets the internal state, discarding any previously buffered samples.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_sumSquares);
        Array.Clear(_subBlocks);
        _subBlockSampleCount = 0;
        _subBlockStart = 0;
        _completedSubBlockCount = 0;
        _shortTermLoudness.Clear();
        _gatingBlockEnergy.Clear();
        _totalBlockCount = 0;
        foreach (var f in _filters) f.Reset();
    }

    private double WindowLoudness(int blocks)
    {
        if (_completedSubBlockCount < blocks)
            return double.NegativeInfinity;
        var energy = AverageEnergy(_completedSubBlockCount - blocks, blocks);
        return LoudnessFromEnergy(energy);
    }

    // Per-channel mean-square averaged across `count` consecutive sub-blocks.
    private double[] AverageEnergy(int start, int count)
    {
        var acc = new double[_channels];
        for (int i = 0; i < count; i++)
        {
            int block = (_subBlockStart + start + i) % _shortTermBlocks;
            int source = block * _channels;
            for (int c = 0; c < _channels; c++)
                acc[c] += _subBlocks[source + c];
        }
        for (int c = 0; c < _channels; c++)
            acc[c] /= count;
        return acc;
    }

    private double[] MeanEnergy(List<double[]> blocks)
    {
        var acc = new double[_channels];
        foreach (var b in blocks)
            for (int c = 0; c < _channels; c++)
                acc[c] += b[c];
        for (int c = 0; c < _channels; c++)
            acc[c] /= blocks.Count;
        return acc;
    }

    private double LoudnessFromEnergy(double[] channelEnergy)
    {
        double weightedSum = 0.0;
        for (int c = 0; c < _channels; c++)
            weightedSum += _weights[c] * channelEnergy[c];
        if (weightedSum <= 0.0)
            return double.NegativeInfinity;
        return Offset + 10.0 * Math.Log10(weightedSum);
    }

    // EBU Tech 3342: LRA = P95 - P10 of short-term loudness values that clear
    // an absolute (-70 LUFS) then relative (-20 LU) gate.
    private double ComputeLoudnessRange()
    {
        if (_shortTermLoudness.Count == 0)
            return 0.0;

        var shortTerm = _shortTermLoudness.Where(l => l >= _absoluteGateLufs).ToList();

        if (shortTerm.Count == 0)
            return 0.0;

        double meanLoudness = shortTerm.Average();
        double relGate = meanLoudness + _relativeGateLu;
        var gated = shortTerm.Where(l => l >= relGate).OrderBy(l => l).ToList();
        if (gated.Count < 2)
            return 0.0;

        return Percentile(gated, LoudnessConstants.LraHighPercentile) - Percentile(gated, LoudnessConstants.LraLowPercentile);
    }

    // p is a fraction in [0, 1].
    private static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        double rank = p * (sorted.Count - 1);
        int lo = (int)Math.Floor(rank);
        int hi = (int)Math.Ceiling(rank);
        if (lo == hi)
            return sorted[lo];
        double frac = rank - lo;
        return sorted[lo] + frac * (sorted[hi] - sorted[lo]);
    }
}
