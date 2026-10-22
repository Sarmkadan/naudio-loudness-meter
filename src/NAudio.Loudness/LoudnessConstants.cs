using System;

// Single source of truth for the ITU-R BS.1770-4 / EBU R 128 constants used by NAudio.Loudness.
// Every literal that a measurement depends on lives here, with the clause it comes from.
namespace NAudio.Loudness
{
    /// <summary>
    /// Constants and validation helpers for loudness measurement, defined by ITU-R BS.1770-4 and EBU R 128.
    /// </summary>
    public static class LoudnessConstants
    {
        // ------------------------------------------------------------------
        // Timing (BS.1770-4 section 2.2 and EBU R 128 section 2)
        // ------------------------------------------------------------------

        /// <summary>
        /// Duration of a sub-block in seconds (100 ms). Base time unit for the momentary and
        /// short-term windows; the meter accumulates energy in sub-blocks of this length.
        /// </summary>
        public const double SubBlockSeconds = 0.1;

        /// <summary>
        /// Number of sub-blocks in a momentary window and in a gating block (4 x 100 ms = 400 ms).
        /// BS.1770-4 section 2.2 defines the gating block as 400 ms.
        /// </summary>
        public const int MomentaryBlocks = 4;

        /// <summary>
        /// Number of sub-blocks in a short-term window (30 x 100 ms = 3 s). EBU R 128 section 2 defines
        /// the short-term loudness window as 3 s.
        /// </summary>
        public const int ShortTermBlocks = 30;

        /// <summary>Length of a gating block in seconds (400 ms). BS.1770-4 section 2.2.</summary>
        public const double MomentaryBlockSeconds = 0.4;

        /// <summary>Length of a short-term window in seconds (3 s). EBU R 128 section 2.</summary>
        public const double ShortTermBlockSeconds = 3.0;

        /// <summary>
        /// Overlap between consecutive 400 ms gating blocks (75 %). BS.1770-4 section 2.2 specifies
        /// the overlap used for integrated loudness.
        /// </summary>
        public const double GatingOverlapFactor = 0.75;

        /// <summary>
        /// Hop between consecutive gating blocks, in sub-blocks. With 400 ms blocks and 75 % overlap the
        /// hop is 25 % of 400 ms = 100 ms = one sub-block. Equals
        /// <c>MomentaryBlocks * (1 - GatingOverlapFactor)</c>.
        /// </summary>
        public const int GatingHopBlocks = 1;

        // ------------------------------------------------------------------
        // Gating (BS.1770-4 section 2.3 and EBU R 128 section 2.2 / Tech 3342)
        // ------------------------------------------------------------------

        /// <summary>Absolute gate for integrated loudness, in LUFS (-70). BS.1770-4 section 2.3, step 1.</summary>
        public const double AbsoluteGateLufs = -70.0;

        /// <summary>
        /// Relative gate for integrated loudness, in LU (-10), applied below the mean of the
        /// absolute-gated blocks. BS.1770-4 section 2.3, step 2.
        /// </summary>
        public const double RelativeGateLu = -10.0;

        /// <summary>Absolute gate for loudness range, in LUFS (-70). EBU Tech 3342 section 2.</summary>
        public const double LraAbsoluteGateThreshold = -70.0;

        /// <summary>Relative gate offset for loudness range, in LU (-20). EBU Tech 3342 section 2.</summary>
        public const double LraRelativeGateOffset = -20.0;

        /// <summary>Lower percentile of gated short-term values for loudness range (0.10). EBU Tech 3342.</summary>
        public const double LraLowPercentile = 0.10;

        /// <summary>Upper percentile of gated short-term values for loudness range (0.95). EBU Tech 3342.</summary>
        public const double LraHighPercentile = 0.95;

        // ------------------------------------------------------------------
        // Loudness equation (BS.1770-4 section 2.2, equation 2)
        // ------------------------------------------------------------------

        /// <summary>
        /// Offset added to <c>10 * log10(sum of G_i * z_i)</c> to give loudness in LKFS / LUFS
        /// (-0.691). BS.1770-4 section 2.2, equation 2.
        /// </summary>
        public const double LoudnessOffsetLkfs = -0.691;

        // ------------------------------------------------------------------
        // Channel weights (BS.1770-4 section 2.2, Table 4)
        // ------------------------------------------------------------------

        /// <summary>Minimum number of channels accepted by <see cref="ChannelWeights"/>.</summary>
        public const int MinChannels = 1;

        /// <summary>
        /// Maximum number of channels with a defined BS.1770-4 weight: L, R, C, Ls, Rs.
        /// </summary>
        public const int MaxChannels = 5;

        /// <summary>
        /// Weight G for the front channels L, R and C (1.0). BS.1770-4 section 2.2, Table 4.
        /// </summary>
        public const double FrontChannelWeight = 1.0;

        /// <summary>
        /// Weight G for the surround channels Ls and Rs (1.41, about +1.5 dB). BS.1770-4 section 2.2, Table 4.
        /// </summary>
        public const double SurroundChannelWeight = 1.41;

        /// <summary>
        /// Weight G for the LFE channel (0.0). BS.1770-4 section 2.2 excludes LFE from the measurement.
        /// Used only by the legacy 5.1 layout in <see cref="ChannelWeights"/> for <c>ChannelWeights.ForChannelCount</c>.
        /// </summary>
        public const double LfeChannelWeight = 0.0;

        // ------------------------------------------------------------------
        // K-weighting prototype (BS.1770-4 Annex 1, 48 kHz analogue prototype)
        // ------------------------------------------------------------------

        /// <summary>Centre frequency of the K-weighting high-shelf stage, in Hz (BS.1770-4 Annex 1).</summary>
        public const double KShelfFrequencyHz = 1681.974450955533;

        /// <summary>Gain of the K-weighting high-shelf stage, in dB (BS.1770-4 Annex 1).</summary>
        public const double KShelfGainDb = 3.999843853973347;

        /// <summary>Quality factor of the K-weighting high-shelf stage (BS.1770-4 Annex 1).</summary>
        public const double KShelfQ = 0.7071752369554196;

        /// <summary>
        /// Exponent applied to the shelf gain to obtain the shelf's bandwidth gain (BS.1770-4 Annex 1).
        /// </summary>
        public const double KShelfVbExponent = 0.4996667741545416;

        /// <summary>Cut-off frequency of the RLB high-pass stage, in Hz (BS.1770-4 Annex 1).</summary>
        public const double KHighPassFrequencyHz = 38.13547087602444;

        /// <summary>Quality factor of the RLB high-pass stage (BS.1770-4 Annex 1).</summary>
        public const double KHighPassQ = 0.5003270373238773;

        // ------------------------------------------------------------------
        // Sample rates and defaults
        // ------------------------------------------------------------------

        /// <summary>Lowest sample rate accepted by <see cref="IsSupportedSampleRate"/>, in Hz.</summary>
        public const int MinSampleRate = 8000;

        /// <summary>Highest sample rate accepted by <see cref="IsSupportedSampleRate"/>, in Hz.</summary>
        public const int MaxSampleRate = 384000;

        /// <summary>Default sample rate for <see cref="LoudnessMeterOptions"/>, in Hz.</summary>
        public const int DefaultSampleRate = 48000;

        // ------------------------------------------------------------------
        // Normalization and true peak
        // ------------------------------------------------------------------

        /// <summary>Factor converting an amplitude ratio to decibels (20 * log10). Used for dBTP and dBFS.</summary>
        public const double DbPerAmplitudeDecade = 20.0;

        /// <summary>Default true-peak ceiling for normalization, in dBTP (-1). EBU R 128 section 3.</summary>
        public const double DefaultTruePeakCeilingDbtp = -1.0;

        /// <summary>
        /// Margin subtracted from the true-peak ceiling in static gain limiting, in dB. This is a library
        /// design value that covers inter-sample overshoot, not a clause value from BS.1770-4 Annex 2.
        /// </summary>
        public const double NormalizationSafetyMarginDb = 1.1;

        /// <summary>
        /// Sample peak assumed when limiting static gain, as a fraction of full scale. This is a library
        /// design value chosen to be conservative for typical material.
        /// </summary>
        public const double ConservativeSamplePeak = 0.95;

        /// <summary>
        /// Time constant of the adaptive normalizer's gain smoothing, in seconds. Library design value.
        /// </summary>
        public const double AdaptiveGainSmoothingSeconds = 1.0;

        // ------------------------------------------------------------------
        // Validation helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the BS.1770-4 channel weights G_i for the first <paramref name="channels"/> channels in
        /// the canonical order L, R, C, Ls, Rs. The returned array is a new instance each call.
        /// </summary>
        /// <param name="channels">The channel count, from <see cref="MinChannels"/> to <see cref="MaxChannels"/>.</param>
        /// <returns>One weight per channel.</returns>
        /// <exception cref="UnsupportedChannelCountException">
        /// <paramref name="channels"/> is outside 1 to 5.
        /// </exception>
        public static double[] ChannelWeights(int channels)
        {
            if (channels < MinChannels || channels > MaxChannels)
                throw new UnsupportedChannelCountException(channels);

            // Canonical BS.1770-4 order: L, R, C, Ls, Rs.
            var weights = new double[channels];
            for (int i = 0; i < channels; i++)
                weights[i] = i < 3 ? FrontChannelWeight : SurroundChannelWeight;
            return weights;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="sampleRate"/> is between <see cref="MinSampleRate"/> and
        /// <see cref="MaxSampleRate"/> inclusive.
        /// </summary>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        public static bool IsSupportedSampleRate(int sampleRate) =>
            sampleRate >= MinSampleRate && sampleRate <= MaxSampleRate;

        /// <summary>
        /// Throws if <paramref name="sampleRate"/> is outside the supported range.
        /// </summary>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        /// <param name="paramName">The name of the argument, used in the exception.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sampleRate"/> is outside <see cref="MinSampleRate"/> to <see cref="MaxSampleRate"/>.
        /// </exception>
        public static void EnsureSupportedSampleRate(int sampleRate, string paramName = "sampleRate")
        {
            if (!IsSupportedSampleRate(sampleRate))
            {
                throw new ArgumentOutOfRangeException(paramName, sampleRate,
                    $"Sample rate must be between {MinSampleRate} and {MaxSampleRate} Hz, but was {sampleRate}.");
            }
        }
    }
}
