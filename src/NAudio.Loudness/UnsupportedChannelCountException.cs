namespace NAudio.Loudness;

/// <summary>
/// Thrown when a channel count has no defined BS.1770-4 channel weights.
/// Derives from <see cref="ArgumentOutOfRangeException"/>, so existing handlers for that type still apply.
/// </summary>
public sealed class UnsupportedChannelCountException : ArgumentOutOfRangeException
{
    /// <summary>Initializes a new instance for the rejected channel count.</summary>
    /// <param name="channels">The channel count that was rejected.</param>
    public UnsupportedChannelCountException(int channels)
        : base("channels", channels,
            $"BS.1770-4 channel weights are defined for {LoudnessConstants.MinChannels} to " +
            $"{LoudnessConstants.MaxChannels} channels, but was {channels}.")
    {
    }
}
