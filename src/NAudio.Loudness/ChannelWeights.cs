using System;
using System.Linq;

namespace NAudio.Loudness;

/// <summary>
/// Per-channel weighting coefficients G_i from ITU-R BS.1770-4. Front channels
/// weigh 1.0, the two surround channels 1.41 (~+1.5 dB), and LFE is ignored.
/// The values come from <see cref="LoudnessConstants"/>.
/// </summary>
public static class ChannelWeights
{
    /// <summary>Returns a sensible default weight vector for the given channel count.</summary>
    /// <remarks>
    /// 1 = mono, 2 = stereo, 5 = L/R/C/Ls/Rs, 6 = 5.1 (LFE at index 3 is dropped).
    /// Unknown layouts fall back to all-ones, which is correct for uncorrelated
    /// full-range channels and never over-counts. For 1 to 5 channels that are not
    /// listed above (3 and 4), see <see cref="LoudnessConstants.ChannelWeights"/>, which
    /// assigns weights by position in the canonical L, R, C, Ls, Rs order.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="channels"/> is zero or negative.</exception>
    public static double[] ForChannelCount(int channels)
    {
        if (channels <= 0)
            throw new ArgumentException("Channel count must be greater than zero.", nameof(channels));
        return channels switch
        {
            1 or 2 or 5 => LoudnessConstants.ChannelWeights(channels),
            6 => new[]
            {
                LoudnessConstants.FrontChannelWeight, LoudnessConstants.FrontChannelWeight,
                LoudnessConstants.FrontChannelWeight, LoudnessConstants.LfeChannelWeight,
                LoudnessConstants.SurroundChannelWeight, LoudnessConstants.SurroundChannelWeight,
            }, // 5.1: L R C LFE Ls Rs
            _ => Enumerable.Repeat(LoudnessConstants.FrontChannelWeight, channels).ToArray(),
        };
    }
}
