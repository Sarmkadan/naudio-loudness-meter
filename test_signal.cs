using NAudio.Wave;
using System;

// Simple test to generate a WAV file
var format = new WaveFormat(48000, 16, 2); // 48kHz, 16-bit, stereo
using (var writer = new WaveFileWriter("test_signal.wav", format))
{
    // Generate a simple sine wave
    var samples = new float[48000 * 2]; // 1 second of audio
    var random = new Random();

    for (int i = 0; i < samples.Length; i += 2)
    {
        // Simple sine wave at 440Hz
        double t = (double)i / format.SampleRate;
        double freq = 440.0;
        samples[i] = (float)(0.5 * Math.Sin(2 * Math.PI * freq * t));
        samples[i + 1] = samples[i]; // Stereo
    }

    writer.WriteSamples(samples);
}

Console.WriteLine("Created test_signal.wav");