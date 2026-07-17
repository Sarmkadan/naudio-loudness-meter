# NAudio.Loudness

Broadcast loudness metering and normalization for [NAudio](https://github.com/naudio/NAudio),
implementing **EBU R128** / **ITU-R BS.1770**.

NAudio has excellent I/O, resampling and DSP building blocks, but it ships no
loudness metering. If you want LUFS numbers you normally reach for `ffmpeg
-af loudnorm` or a native `libebur128` binding. This library fills that gap in
pure managed C#: feed it the same `ISampleProvider` you already have and get
integrated / short-term / momentary loudness, loudness range (LRA) and
true-peak, plus a normalizing sample provider that hits a target LUFS.

```
Integrated:    -23.0 LUFS
Loudness range:   6.4 LU
True peak:      -1.2 dBTP
```

## What it measures

| Metric | Window | Standard |
| --- | --- | --- |
| Momentary loudness | 400 ms sliding | BS.1770 / EBU 3341 |
| Short-term loudness | 3 s sliding | EBU 3341 |
| Integrated loudness | whole program, gated | BS.1770 (abs -70 LUFS + rel -10 LU) |
| Loudness range (LRA) | short-term distribution | EBU 3342 |
| True peak | 4x oversampled | BS.1770 Annex 2 |
| Sample peak | raw | - |

The K-weighting pre-filter and RLB high-pass coefficients are **re-derived for
the actual sample rate** by bilinear transform of the 48 kHz analogue
prototype, so measurements stay correct at 44.1 / 48 / 96 kHz. (The fixed
coefficient tables printed in BS.1770 are only exact at 48 kHz.)

## Quick start

```csharp
using NAudio.Loudness;
using NAudio.Wave;

using var reader = new AudioFileReader("track.wav");
LoudnessAnalysis result = reader.ToSampleProvider().MeasureLoudness();

Console.WriteLine(result); // Integrated: -18.4 LUFS, LRA: 7.1 LU, True peak: -0.8 dBTP, ...
double gain = result.GainToReach(-23.0); // EBU R128 delivery target
```

## LoudnessMeter

`LoudnessMeter` provides real-time loudness metering by incrementally processing audio samples. It implements the EBU R128 / ITU-R BS.1770 algorithms with K-weighting and gated integrated loudness calculation. The meter maintains momentary, short-term, and integrated loudness measurements, along with loudness range (LRA) and true peak values.

Create an instance with your audio format's sample rate and channel count, feed it samples via `AddSamples()`, and read the current loudness metrics whenever needed (e.g., to update a UI meter). Call `Reset()` between programs or tracks to clear the integrated loudness history.

```csharp
using NAudio.Loudness;
using NAudio.Wave;

// Initialize with the audio format's sample rate and channels
var meter = new LoudnessMeter(sampleRate: 48000, channels: 2);

// Feed audio samples as they arrive (interleaved float array)
meter.AddSamples(audioBuffer.AsSpan());

// Read current loudness measurements
Console.WriteLine($"Momentary: {meter.MomentaryLufs:0.0} LUFS, " +
                $"Short-term: {meter.ShortTermLufs:0.0} LUFS, " +
                $"Integrated: {meter.IntegratedLufs:0.0} LUFS, " +
                $"LRA: {meter.LoudnessRange:0.0} LU, " +
                $"True peak: {meter.TruePeakDb:0.0} dBTP");

// Reset between programs/tracks
meter.Reset();
```

### Live/streaming metering

`LoudnessMeter` is incremental - push buffers as they arrive (e.g. from a
capture device) and read the properties whenever you want to update a UI:

```csharp
var meter = new LoudnessMeter(waveFormat.SampleRate, waveFormat.Channels);

void OnBuffer(float[] interleaved, int count)
{
    meter.AddSamples(interleaved.AsSpan(0, count));
    label.Text = $"M {meter.MomentaryLufs:0.0}  S {meter.ShortTermLufs:0.0}  I {meter.IntegratedLufs:0.0}";
}
```

### Normalizing to a target

```csharp
using var reader = new AudioFileReader("in.wav");
double gain = reader.ToSampleProvider().MeasureLoudness().GainToReach(-16.0);

using var src = new AudioFileReader("in.wav");
var normalized = new LoudnessNormalizingSampleProvider(src.ToSampleProvider(), gain, truePeakCeilingDb: -1.0);
WaveFileWriter.CreateWaveFile16("out.wav", normalized);
```

## LoudnessMeterTests

`LoudnessMeterTests` provides comprehensive unit tests that verify the loudness measurement behavior of the `LoudnessMeter` class according to EBU R128 and ITU-R BS.1770 standards. These tests validate fundamental audio processing assumptions and ensure measurement accuracy across different scenarios.

The test suite covers:

- **Reference measurements**: Full-scale sine waves at known loudness levels
- **Channel behavior**: Mono vs stereo energy summation
- **Amplitude scaling**: Precise loudness changes with amplitude adjustments
- **Silence handling**: Proper gating and negative infinity values
- **Time windowing**: Momentary, short-term, and integrated loudness windows
- **Sample rate independence**: Measurements remain consistent across 44.1 kHz, 48 kHz, and 96 kHz



```csharp
using NAudio.Loudness;
using NAudio.Loudness.Tests;
using Xunit;

// Initialize a meter for stereo audio at 48 kHz
var meter = new LoudnessMeter(sampleRate: 48000, channels: 2);

// Test 1: Full-scale 1 kHz sine wave should measure ~-3.01 LUFS mono
meter.AddSamples(SignalGenerator.Sine(1000, 1.0, 48000, 4, 1));
Assert.Equal(-3.01, meter.IntegratedLufs, 1);

// Test 2: Two coherent channels summing to ~0 LUFS
meter.Reset();
meter.AddSamples(SignalGenerator.Sine(1000, 1.0, 48000, 4, 2));
Assert.Equal(0.0, meter.IntegratedLufs, 1);

// Test 3: Halving amplitude results in -6.02 LU drop
var full = new LoudnessMeter(48000, 2);
var half = new LoudnessMeter(48000, 2);
full.AddSamples(SignalGenerator.Sine(1000, 1.0, 48000, 4, 2));
half.AddSamples(SignalGenerator.Sine(1000, 0.5, 48000, 4, 2));
Assert.Equal(6.02, full.IntegratedLufs - half.IntegratedLufs, 1);

// Test 4: Silence results in negative infinity
meter.Reset();
meter.AddSamples(SignalGenerator.Silence(48000, 2, 2));
Assert.Equal(double.NegativeInfinity, meter.IntegratedLufs);

// Test 5: Sample rate independence (44.1 kHz, 48 kHz, 96 kHz)
foreach (int rate in new[] { 44100, 48000, 96000 })
{
var rateMeter = new LoudnessMeter(rate, 2);
rateMeter.AddSamples(SignalGenerator.Sine(1000, 0.5, rate, 4, 2));
Assert.Equal(-6.0, rateMeter.IntegratedLufs, 1);
}

// Reset between test cases
meter.Reset();
```

## LoudnessMeterTestsExtensions

`LoudnessMeterTestsExtensions` is a collection of extension methods that simplify writing unit tests for loudness meter behavior. These methods provide convenient assertions for verifying momentary, short-term, and integrated loudness values, along with utilities for creating meters with test samples and calculating loudness differences.

The extensions handle edge cases like negative infinity values (silence) and provide tolerance-based assertions for floating-point comparisons.

```csharp
using NAudio.Loudness;
using NAudio.Loudness.Tests;
using Xunit;

// Create a meter and feed it some test samples
var meter = new LoudnessMeter(sampleRate: 48000, channels: 2);
meter.AddSamples(SignalGenerator.Sine(1000, 0.5, 48000, 4, 2));

// Assert integrated loudness is within tolerance of expected value
meter.AssertIntegratedLufs(-6.0, 0.01);

// Assert momentary loudness is within tolerance
meter.AssertMomentaryLufs(-6.0, 0.01);

// Assert short-term loudness is within tolerance
meter.AssertShortTermLufs(-6.0, 0.01);

// Assert loudness range is within expected bounds
meter.AssertLoudnessRange(0, 10);

// Calculate loudness difference between two values
double diff = meter.IntegratedLufs.LoudnessDifference(-12.0);
Assert.Equal(6.0, diff, 2);

// Assert two values are approximately equal
(-6.0).AssertApproximatelyEqual(-6.001, 0.01);

// Create a meter with samples in a fluent way
var testMeter = 48000.WithSamples(2, SignalGenerator.Sine(1000, 0.7, 48000, 4, 2));
testMeter.AssertIntegratedLufs(-3.0, 0.01);
```

## TruePeakMeterTestsExtensions

`TruePeakMeterTestsExtensions` provides a set of extension methods that simplify testing scenarios involving the `TruePeakMeter` class. These methods offer convenient assertions and formatting utilities for verifying true peak and sample peak measurements during unit tests.

```csharp
using NAudio.Loudness;
using NAudio.Loudness.Tests;

// Create a true peak meter for stereo audio
var meter = 2.CreateMeter();

// Feed some test samples (e.g., a sine wave at -6 dBFS)
float[] buffer = new float[1024];
for (int i = 0; i < buffer.Length; i++)
{
    buffer[i] = 0.5f; // -6 dBFS sine wave
}
meter.AddSamples(buffer);

// Assert that true peak is within expected range
meter.AssertTruePeakInRange(-6.1, -5.9);

// Assert that sample peak is within expected range  
meter.AssertSamplePeakInRange(-6.1, -5.9);

// Assert that true peak exceeds sample peak by at least 0.1 dB
meter.AssertTruePeakExceedsSamplePeakBy(0.1);

// Get formatted peak values
string truePeakStr = meter.GetTruePeakString();
string samplePeakStr = meter.GetSamplePeakString();

// Get linear true peak value (1.0 == 0 dBTP)
double truePeakLinear = meter.GetTruePeakLinear();

// Reset the meter for the next test case
meter.Reset();
```

## CLI

The `loudness` tool wraps the library for quick checks:

```
loudness scan track.wav
loudness normalize in.wav out.wav -16 -1
```

## Design notes / limitations

- **The normalizer applies a single static gain.** That is the correct model
  for EBU R128 program normalization (one offset per program), *not* a
  compressor. LRA is left intact.
- **The true-peak ceiling is a hard clip, not a look-ahead limiter.** With a
  sane gain the ceiling rarely engages; if you push gain hard past a peaky
  master you will clip. A proper limiter is out of scope - measure first, then
  choose a target that leaves peak headroom.
- True peak uses a 4x polyphase windowed-sinc oversampler (12 taps/phase).
  That matches BS.1770 within a few tenths of a dB for typical program
  material; it is not the exact 48-tap table from the spec.
- Gating buffers the per-block energies for the whole program, so integrated
  loudness of an arbitrarily long stream grows memory slowly (a few doubles
  per 100 ms). Fine for files and normal streams; call `Reset()` between
  programs.

## Layout

```
src/NAudio.Loudness   the library
src/LoudnessCli       the `loudness` command-line tool
tests/...Tests        xUnit tests with known-value references
```

## Building

```
dotnet build
dotnet test
```

The library targets `net8.0`; the tests and CLI run on the current SDK.

## Verification

Tests anchor against physically exact reference points rather than magic
numbers: a full-scale 1 kHz sine reads -3.01 LUFS mono / ~0 LUFS stereo,
halving amplitude shifts loudness by exactly 6.02 LU, an inter-sample tone
proves true peak exceeds sample peak, and a measure -> normalize -> re-measure
round trip lands on the requested LUFS target.

## License

MIT
