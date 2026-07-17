using System.Globalization;

namespace NAudio.Loudness;

/// <summary>
/// Extension methods for <see cref="LoudnessMeter"/> that provide convenient ways to access
/// loudness measurements and status information.
/// </summary>
public static class LoudnessMeterExtensions
{
    /// <summary>
    /// Gets all current loudness measurements in a single call.
    /// </summary>
    /// <param name="meter">The loudness meter instance.</param>
    /// <returns>A tuple containing (MomentaryLufs, ShortTermLufs, IntegratedLufs).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public static (double MomentaryLufs, double ShortTermLufs, double IntegratedLufs) GetLufsLevels(
        this LoudnessMeter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        return (
            meter.MomentaryLufs,
            meter.ShortTermLufs,
            meter.IntegratedLufs
        );
    }

    /// <summary>
    /// Gets a formatted status string showing the current loudness measurements.
    /// Useful for logging, debugging, or real-time monitoring displays.
    /// </summary>
    /// <param name="meter">The loudness meter instance.</param>
    /// <param name="format">Optional format string for numeric values (default: "0.0").</param>
    /// <returns>A formatted status string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public static string GetCurrentLufsStatus(
        this LoudnessMeter meter,
        string format = "0.0")
    {
        ArgumentNullException.ThrowIfNull(meter);
        ArgumentException.ThrowIfNullOrEmpty(format);

        return $"Momentary: {meter.MomentaryLufs.ToString(format, CultureInfo.InvariantCulture)} LUFS, " +
               $"Short-term: {meter.ShortTermLufs.ToString(format, CultureInfo.InvariantCulture)} LUFS, " +
               $"Integrated: {meter.IntegratedLufs.ToString(format, CultureInfo.InvariantCulture)} LUFS";
    }

    /// <summary>
    /// Checks if the audio being measured is silent (below the absolute gate threshold of -70 LUFS).
    /// </summary>
    /// <param name="meter">The loudness meter instance.</param>
    /// <returns><see langword="true"/> if the integrated loudness is silent; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public static bool IsSilent(this LoudnessMeter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        return meter.IntegratedLufs == double.NegativeInfinity;
    }

    /// <summary>
    /// Gets the loudness range (LRA) measurement.
    /// </summary>
    /// <param name="meter">The loudness meter instance.</param>
    /// <returns>The loudness range in LU, or 0 if not enough data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="meter"/> is <see langword="null"/>.</exception>
    public static double GetLoudnessRange(this LoudnessMeter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        return meter.LoudnessRange;
    }
}