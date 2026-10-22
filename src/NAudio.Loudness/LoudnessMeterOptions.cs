namespace NAudio.Loudness;

/// <summary>
/// Configuration for a <see cref="LoudnessMeter"/> created through the options constructor.
/// The defaults match ITU-R BS.1770 / EBU R 128 (400 ms momentary, 3 s short-term,
/// -70 LUFS absolute gate, -10 LU relative gate). Call <see cref="Validate"/> to check the values;
/// the meter constructor does so automatically.
/// </summary>
public sealed class LoudnessMeterOptions
{
    private const int MinChannels = LoudnessConstants.MinChannels;

    // BS.1770 channel weights are defined up to 5 channels (L R C Ls Rs).
    private const int MaxChannels = LoudnessConstants.MaxChannels;

    /// <summary>Sample rate in Hz. Must be between 8000 and 384000.</summary>
    public int SampleRate { get; init; } = LoudnessConstants.DefaultSampleRate;

    /// <summary>Number of interleaved channels. Must be between 1 and 5.</summary>
    public int Channels { get; init; } = 2;

    /// <summary>
    /// Momentary window in milliseconds (400 ms by default). Must be positive and not longer than
    /// <see cref="ShortTermWindowMs"/>. The meter measures in 100 ms sub-blocks, so the value is
    /// rounded to the nearest 100 ms (minimum one sub-block). This window also sets the gating block
    /// length, which BS.1770 fixes at 400 ms; changing it departs from the standard.
    /// </summary>
    public int MomentaryWindowMs { get; init; } =
        (int)Math.Round(LoudnessConstants.MomentaryBlockSeconds * 1000.0);

    /// <summary>
    /// Short-term window in milliseconds (3000 ms by default). Must be positive and at least
    /// <see cref="MomentaryWindowMs"/>. Rounded to 100 ms sub-blocks, as for the momentary window.
    /// </summary>
    public int ShortTermWindowMs { get; init; } =
        (int)Math.Round(LoudnessConstants.ShortTermBlockSeconds * 1000.0);

    /// <summary>
    /// Absolute gate in LUFS (-70 by default). Blocks below this level are excluded from the
    /// integrated and loudness-range calculations. Must be finite and negative.
    /// </summary>
    public double AbsoluteGateLufs { get; init; } = LoudnessConstants.AbsoluteGateLufs;

    /// <summary>
    /// Relative gate in LU (-10 by default), applied below the mean of the absolute-gated blocks.
    /// Must be finite, negative, and greater than <see cref="AbsoluteGateLufs"/>.
    /// </summary>
    public double RelativeGateLu { get; init; } = LoudnessConstants.RelativeGateLu;

    /// <summary>
    /// Checks every option and throws if one is out of range.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="SampleRate"/> is outside 8000–384000 Hz, <see cref="Channels"/> is outside 1–5,
    /// a window is not positive, or a gate is not a finite negative value.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="ShortTermWindowMs"/> is shorter than <see cref="MomentaryWindowMs"/>, or
    /// <see cref="RelativeGateLu"/> is not greater than <see cref="AbsoluteGateLufs"/>.
    /// </exception>
    public void Validate()
    {
        LoudnessConstants.EnsureSupportedSampleRate(SampleRate, nameof(SampleRate));

        if (Channels < MinChannels || Channels > MaxChannels)
        {
            throw new ArgumentOutOfRangeException(nameof(Channels), Channels,
                $"Channels must be between {MinChannels} and {MaxChannels} (BS.1770 channel weights " +
                $"are defined up to {MaxChannels} channels), but was {Channels}.");
        }

        if (MomentaryWindowMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MomentaryWindowMs), MomentaryWindowMs,
                $"MomentaryWindowMs must be greater than zero, but was {MomentaryWindowMs}.");
        }

        if (ShortTermWindowMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ShortTermWindowMs), ShortTermWindowMs,
                $"ShortTermWindowMs must be greater than zero, but was {ShortTermWindowMs}.");
        }

        if (ShortTermWindowMs < MomentaryWindowMs)
        {
            throw new ArgumentException(
                $"ShortTermWindowMs ({ShortTermWindowMs} ms) must be at least MomentaryWindowMs ({MomentaryWindowMs} ms).",
                nameof(ShortTermWindowMs));
        }

        // NaN and infinities are rejected here too: they are not "negative" in a useful sense.
        if (!double.IsFinite(AbsoluteGateLufs) || AbsoluteGateLufs >= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(AbsoluteGateLufs), AbsoluteGateLufs,
                $"AbsoluteGateLufs must be a finite negative value, but was {AbsoluteGateLufs}.");
        }

        if (!double.IsFinite(RelativeGateLu) || RelativeGateLu >= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(RelativeGateLu), RelativeGateLu,
                $"RelativeGateLu must be a finite negative value, but was {RelativeGateLu}.");
        }

        if (RelativeGateLu <= AbsoluteGateLufs)
        {
            throw new ArgumentException(
                $"RelativeGateLu ({RelativeGateLu}) must be greater than AbsoluteGateLufs ({AbsoluteGateLufs}).",
                nameof(RelativeGateLu));
        }
    }
}
