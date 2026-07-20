using System;
using System.Linq;

namespace NAudio.Loudness;

/// <summary>
/// Per-channel weighting coefficients G_i from ITU-R BS.1770. Front channels
/// weigh 1.0, the two surround channels 1.41 (~+1.5 dB), and LFE is ignored.
/// </summary>
public static class ChannelWeights
{
    /// <summary>Returns a sensible default weight vector for the given channel count.</summary>
    /// <remarks>
    /// 1 = mono, 2 = stereo, 5 = L/R/C/Ls/Rs, 6 = 5.1 (LFE at index 3 is dropped).
    /// Unknown layouts fall back to all-ones, which is correct for uncorrelated
    /// full-range channels and never over-counts.
    /// </remarks>
    public static double[] ForChannelCount(int channels) => channels switch
    {
        1 => new[] { 1.0 },
        2 => new[] { 1.0, 1.0 },
        5 => new[] { 1.0, 1.0, 1.0, 1.41, 1.41 },
        6 => new[] { 1.0, 1.0, 1.0, 0.0, 1.41, 1.41 }, // 5.1: L R C LFE Ls Rs
        _ => Enumerable.Repeat(1.0, channels).ToArray(),
    };
}
