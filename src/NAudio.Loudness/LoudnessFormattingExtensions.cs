using System;

namespace NAudio.Loudness
{
    public static class LoudnessFormattingExtensions
    {
        public static string FormatLufs(double lufs)
        {
            if (double.IsNegativeInfinity(lufs))
            {
                return "-inf";
            }
            return lufs.ToString("0.0");
        }

        public static string FormatDbtp(double dbtp)
        {
            return dbtp.ToString("0.0");
        }

        public static string FormatDeltaToTarget(double delta)
        {
            return delta.ToString("0.0");
        }
    }
}
