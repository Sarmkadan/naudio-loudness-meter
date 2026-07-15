using Xunit;

namespace NAudio.Loudness.Tests;

public class TruePeakMeterTests
{
    private const int Fs = 48000;

    [Fact]
    public void FullScaleSine_TruePeakNearZeroDb()
    {
        var meter = new TruePeakMeter(2);
        meter.AddSamples(SignalGenerator.Sine(1000, 1.0, Fs, 1, 2));
        Assert.InRange(meter.TruePeakDb, -0.5, 0.5);
    }

    [Fact]
    public void SamplePeak_TracksRawMaximum()
    {
        var meter = new TruePeakMeter(1);
        meter.AddSamples(SignalGenerator.Sine(1000, 0.5, Fs, 1, 1));
        Assert.Equal(-6.02, meter.SamplePeakDb, 1);
    }

    // A tone at Nyquist/2 sampled 45 degrees off peak: samples top out near
    // -3 dBFS, but the true waveform reaches full scale between them.
    [Fact]
    public void InterSamplePeak_ExceedsSamplePeak()
    {
        var meter = new TruePeakMeter(1);
        meter.AddSamples(SignalGenerator.Sine(Fs / 4.0, 1.0, Fs, 1, 1, Math.PI / 4.0));
        Assert.True(meter.TruePeakDb > meter.SamplePeakDb + 1.5,
            $"true={meter.TruePeakDb:0.00} sample={meter.SamplePeakDb:0.00}");
        Assert.InRange(meter.TruePeakDb, -1.0, 0.5);
    }

    [Fact]
    public void Silence_PeaksAreNegativeInfinity()
    {
        var meter = new TruePeakMeter(2);
        meter.AddSamples(SignalGenerator.Silence(Fs, 1, 2));
        Assert.Equal(double.NegativeInfinity, meter.TruePeakDb);
    }
}
