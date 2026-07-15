namespace NAudio.Loudness.Tests;

/// <summary>Test signal helpers - all interleaved float, range [-1, 1].</summary>
internal static class SignalGenerator
{
    public static float[] Sine(double freq, double amplitude, int sampleRate, double seconds, int channels, double phase = 0.0)
    {
        int frames = (int)(sampleRate * seconds);
        var buffer = new float[frames * channels];
        double w = 2.0 * Math.PI * freq / sampleRate;
        for (int n = 0; n < frames; n++)
        {
            float v = (float)(amplitude * Math.Sin(w * n + phase));
            for (int c = 0; c < channels; c++)
                buffer[n * channels + c] = v;
        }
        return buffer;
    }

    public static float[] Silence(int sampleRate, double seconds, int channels)
        => new float[(int)(sampleRate * seconds) * channels];
}
