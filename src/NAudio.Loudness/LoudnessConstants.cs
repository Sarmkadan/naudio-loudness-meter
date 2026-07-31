using System;

/// <summary>
/// Provides constant values used throughout the NAudio.Loudness implementation.
/// The values are defined by the ITU‑R BS.1770 / EBU R 128 specifications.
/// </summary>
namespace NAudio.Loudness
{
    /// <summary>
    /// Collection of magic numbers for loudness measurement.
    /// </summary>
    public static class LoudnessConstants
    {
        /// <summary>
        /// Duration of a sub‑block in seconds (100 ms). Used as the base time unit for momentary
        /// and short‑term windows as defined by BS.1770.
        /// </summary>
        public const double SubBlockSeconds = 0.1;

        /// <summary>
        /// Number of sub‑blocks that constitute the momentary window (400 ms).
        /// </summary>
        public const int MomentaryBlocks = 4;

        /// <summary>
        /// Number of sub‑blocks that constitute the short‑term window (3 s).
        /// </summary>
        public const int ShortTermBlocks = 30;

        /// <summary>
        /// Overlap factor for gating blocks (75 %). A new 400 ms gating block starts every sub‑block.
        /// </summary>
        public const double GatingOverlapFactor = 0.75;

        /// <summary>
        /// Absolute gate threshold for integrated loudness (–70 LUFS) as defined by EBU R 128 / BS.1770.
        /// </summary>
        public const double AbsoluteGateLufs = -70.0;

        /// <summary>
        /// Relative gate offset for integrated loudness (–10 LU) as defined by EBU R 128 / BS.1770.
        /// </summary>
        public const double RelativeGateLu = -10.0;

        /// <summary>
        /// Absolute gate threshold for loudness range calculation (–70 LUFS).
        /// </summary>
        public const double LraAbsoluteGateThreshold = -70.0;

        /// <summary>
        /// Relative gate offset for loudness range calculation (–20 LU).
        /// </summary>
        public const double LraRelativeGateOffset = -20.0;
    }
}
