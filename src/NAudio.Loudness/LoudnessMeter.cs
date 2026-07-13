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
    private const double SubBlockSeconds = 0.1;
    private const int MomentaryBlocks = 4;   // 400 ms
    private const int ShortTermBlocks = 30;  // 3 s
    private const double AbsoluteGateLufs = -70.0;
    private const double RelativeGateLu = -10.0;
    private const double Offset = -0.691;    // BS.1770 loudness offset

    private readonly int _channels;
    private readonly double[] _weights;
    private readonly KWeightingFilter[] _filters;
    private readonly int _subBlockSize;

    // Current (partial) 100 ms sub-block.
    private readonly double[] _sumSquares;
    private int _subBlockSampleCount;

    // Per-channel mean-square of every completed sub-block.
    private readonly List<double[]> _subBlocks = new();

    // Channel-weighted energy of every completed 400 ms gating block that
    // passed the absolute gate, together with its loudness.
    private readonly List<double[]> _gatingBlockEnergy = new();

    public LoudnessMeter(int sampleRate, int channels)
        : this(sampleRate, channels, ChannelWeights.ForChannelCount(channels))
    {
    }

    public LoudnessMeter(int sampleRate, int channels, double[] channelWeights)
    {
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
        if (channelWeights.Length != channels)
            throw new ArgumentException("Weight vector length must match channel count.", nameof(channelWeights));

        SampleRate = sampleRate;
        _channels = channels;
        _weights = (double[])channelWeights.Clone();
        _subBlockSize = (int)Math.Round(sampleRate * SubBlockSeconds);
        _filters = new KWeightingFilter[channels];
        for (int c = 0; c < channels; c++)
            _filters[c] = new KWeightingFilter(sampleRate);
        _sumSquares = new double[channels];
    }

    public int SampleRate { get; }

    /// <summary>Feeds a block of interleaved samples in the range [-1, 1].</summary>
    public void AddSamples(ReadOnlySpan<float> interleaved)
    {
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
        var means = new double[_channels];
        for (int c = 0; c < _channels; c++)
        {
            means[c] = _sumSquares[c] / _subBlockSize;
            _sumSquares[c] = 0.0;
        }
        _subBlockSampleCount = 0;
        _subBlocks.Add(means);

        // A completed sub-block closes one 400 ms gating block (75 % overlap
        // means a new gating block every 100 ms).
        if (_subBlocks.Count >= MomentaryBlocks)
        {
            var energy = AverageEnergy(_subBlocks.Count - MomentaryBlocks, MomentaryBlocks);
            if (LoudnessFromEnergy(energy) >= AbsoluteGateLufs)
                _gatingBlockEnergy.Add(energy);
        }
    }

    /// <summary>Loudness of the most recent 400 ms, or -inf if not enough audio yet.</summary>
    public double MomentaryLufs => WindowLoudness(MomentaryBlocks);

    /// <summary>Loudness of the most recent 3 s, or -inf if not enough audio yet.</summary>
    public double ShortTermLufs => WindowLoudness(ShortTermBlocks);

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
            double relativeThreshold = LoudnessFromEnergy(mean) + RelativeGateLu;

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

    public void Reset()
    {
        Array.Clear(_sumSquares);
        _subBlockSampleCount = 0;
        _subBlocks.Clear();
        _gatingBlockEnergy.Clear();
        foreach (var f in _filters) f.Reset();
    }

    private double WindowLoudness(int blocks)
    {
        if (_subBlocks.Count < blocks)
            return double.NegativeInfinity;
        var energy = AverageEnergy(_subBlocks.Count - blocks, blocks);
        return LoudnessFromEnergy(energy);
    }

    // Per-channel mean-square averaged across `count` consecutive sub-blocks.
    private double[] AverageEnergy(int start, int count)
    {
        var acc = new double[_channels];
        for (int i = 0; i < count; i++)
        {
            var block = _subBlocks[start + i];
            for (int c = 0; c < _channels; c++)
                acc[c] += block[c];
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
        if (_subBlocks.Count < ShortTermBlocks)
            return 0.0;

        var shortTerm = new List<double>();
        for (int end = ShortTermBlocks; end <= _subBlocks.Count; end++)
        {
            double l = LoudnessFromEnergy(AverageEnergy(end - ShortTermBlocks, ShortTermBlocks));
            if (l >= AbsoluteGateLufs)
                shortTerm.Add(l);
        }

        if (shortTerm.Count == 0)
            return 0.0;

        double meanLoudness = shortTerm.Average();
        double relGate = meanLoudness - 20.0;
        var gated = shortTerm.Where(l => l >= relGate).OrderBy(l => l).ToList();
        if (gated.Count < 2)
            return 0.0;

        return Percentile(gated, 95.0) - Percentile(gated, 10.0);
    }

    private static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        double rank = (p / 100.0) * (sorted.Count - 1);
        int lo = (int)Math.Floor(rank);
        int hi = (int)Math.Ceiling(rank);
        if (lo == hi)
            return sorted[lo];
        double frac = rank - lo;
        return sorted[lo] + frac * (sorted[hi] - sorted[lo]);
    }
}
