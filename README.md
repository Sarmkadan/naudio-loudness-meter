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

## Getting started

### Install

```bash
dotnet add package NAudio.Loudness
dotnet add package NAudio          # AudioFileReader and WaveInEvent live here
```

> The package is not yet published to nuget.org. Until it is, reference the
> project directly from a checkout:
> `dotnet add reference path/to/src/NAudio.Loudness/NAudio.Loudness.csproj`

Requires .NET 8 or later (the library targets `net8.0`).

### Measure a WAV file

`LoudnessMeter` takes the sample rate and channel count of the audio it will
receive: `new LoudnessMeter(int sampleRate, int channels)`. Pass the values from
the file's `WaveFormat`, as the example does. The meter has no max properties, so
the example polls it after each 100 ms block and keeps the highest readings.

```csharp
using System;
using NAudio.Loudness;
using NAudio.Wave;

using var reader = new AudioFileReader("track.wav");
int channels = reader.WaveFormat.Channels;

var meter = new LoudnessMeter(reader.WaveFormat.SampleRate, channels);

// 100 ms of audio per block: the meter updates its readings every 100 ms,
// so polling after each block captures every momentary/short-term value.
var buffer = new float[reader.WaveFormat.SampleRate / 10 * channels];
double momentaryMax = double.NegativeInfinity;
double shortTermMax = double.NegativeInfinity;

int read;
while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
{
    meter.AddSamples(buffer.AsSpan(0, read));
    momentaryMax = Math.Max(momentaryMax, meter.MomentaryLufs);
    shortTermMax = Math.Max(shortTermMax, meter.ShortTermLufs);
}

Console.WriteLine($"Integrated:     {meter.IntegratedLufs:0.0} LUFS");
Console.WriteLine($"Momentary max:  {momentaryMax:0.0} LUFS");
Console.WriteLine($"Short-term max: {shortTermMax:0.0} LUFS");
```

Expected output for a 10 s stereo 48 kHz 16-bit test tone (1 kHz sine, 5 s at
-20 dBFS followed by 5 s at -26 dBFS). The integrated value matches ffmpeg's
`ebur128` filter. Your numbers will differ for other files:

```
Integrated:     -25.1 LUFS
Momentary max:  -23.1 LUFS
Short-term max: -23.1 LUFS
```

### Real-time metering from a microphone (Windows)

`WaveInEvent` uses the Windows multimedia API, so this example runs on Windows
only. It captures 48 kHz mono 16-bit PCM in 100 ms buffers, and prints the
momentary loudness for each buffer.

```csharp
using System;
using NAudio.Loudness;
using NAudio.Wave;

const int sampleRate = 48000;
const int channels = 1;

var meter = new LoudnessMeter(sampleRate, channels);

using var waveIn = new WaveInEvent
{
    WaveFormat = new WaveFormat(sampleRate, 16, channels),
    BufferMilliseconds = 100,
};

waveIn.DataAvailable += (_, e) =>
{
    // WaveInEvent delivers 16-bit PCM bytes; convert them to floats in [-1, 1].
    var samples = new float[e.BytesRecorded / 2];
    for (int i = 0; i < samples.Length; i++)
        samples[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;

    meter.AddSamples(samples);
    Console.WriteLine($"Momentary: {meter.MomentaryLufs,6:0.0} LUFS");
};

waveIn.StartRecording();
Console.WriteLine("Listening. Press Enter to stop.");
Console.ReadLine();
waveIn.StopRecording();
```

If the device rejects 48 kHz mono 16-bit, pick a format it supports and pass the
same sample rate and channel count to `LoudnessMeter`.

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

## LoudnessMeterExtensionsTests

`LoudnessMeterExtensionsTests` exercises the convenience extension methods
built on top of `LoudnessMeter` - pulling a snapshot of momentary/short-term
LUFS, formatting a human-readable status line, checking silence, and reading
back the loudness range. It confirms the extensions handle both a freshly
created meter (still silent, no finite range yet) and a meter that has been
fed real signal via `SignalGenerator` or a raw sample buffer, and that a
`null` meter throws rather than producing garbage values.

```csharp
using NAudio.Loudness;

var meter = new LoudnessMeter(sampleRate: 48000, channels: 1);

// A quiet meter reports silent and stays that way until enough
// signal clears the absolute gate (-70 LUFS).
Console.WriteLine(meter.IsSilent()); // True

var samples = SignalGenerator.Sine(frequency: 1000, amplitude: 0.5,
    sampleRate: 48000, seconds: 5, channels: 1);
meter.AddSamples(samples);

var levels = meter.GetLufsLevels();
Console.WriteLine($"{levels.MomentaryLufs:0.00} / {levels.ShortTermLufs:0.00} LUFS");

Console.WriteLine(meter.GetCurrentLufsStatus("0.00"));
// e.g. "Momentary: -6.02 LUFS | Short-term: -6.02 LUFS | Integrated: -6.02 LUFS"

Console.WriteLine(meter.IsSilent());        // False
Console.WriteLine(meter.GetLoudnessRange()); // >= 0.0
```

## LoudnessMeterGatingTests

`LoudnessMeterGatingTests` verifies `LoudnessMeter`'s implementation of the
EBU R128 / BS.1770 gating rules: the absolute gate at -70 LUFS and the
relative gate at -10 LU below the mean of ungated blocks. It checks that
signals below the absolute gate never contribute to `IntegratedLufs` or
`GatedBlockCount`, that quiet passages following loud ones are suppressed by
the relative gate while still counting toward `TotalBlockCount`, and that
`Reset()` returns the meter to its initial, silent state.

```csharp
using NAudio.Loudness;

var meter = new LoudnessMeter(sampleRate: 48000, channels: 1);

// Loud section clears both gates and drives the integrated value.
double loudAmp = Math.Pow(10.0, -20.0 / 20.0);
meter.AddSamples(SignalGenerator.Sine(1000, loudAmp, 48000, seconds: 3, channels: 1));

// Quiet section clears the absolute gate but falls below the relative
// gate (mean - 10 LU), so it is excluded from IntegratedLufs even though
// it still increments GatedBlockCount and TotalBlockCount.
double quietAmp = Math.Pow(10.0, -40.0 / 20.0);
meter.AddSamples(SignalGenerator.Sine(1000, quietAmp, 48000, seconds: 3, channels: 1));

Console.WriteLine(meter.IntegratedLufs);   // stays close to -20 LUFS
Console.WriteLine(meter.TotalBlockCount);  // counts all blocks, loud and quiet
Console.WriteLine(meter.GatedBlockCount);  // counts blocks past the absolute gate

meter.Reset();
Console.WriteLine(meter.IntegratedLufs);   // double.NegativeInfinity again
```

## BiquadTests

`BiquadTests` exercises the direct-form I `Biquad` filter section in isolation, independent
of the loudness/gating pipeline built on top of it. It covers DC and steady-state behaviour
(low-pass passing DC, a high-pass attenuating it), boundedness on sine waves and arbitrary
bounded inputs, `Reset()` clearing internal state, and coefficient edge cases such as an
identity filter (`b0 = 1`, rest zero) or a simple scaling filter (`b0 = 0.5`, rest zero).

```csharp
using NAudio.Loudness.Filters;

// Simple scaling filter: y[n] = 0.5 * x[n]
var filter = new Biquad(b0: 0.5, b1: 0.0, b2: 0.0, a1: 0.0, a2: 0.0);

double output = filter.Process(1.0);   // 0.5
output = filter.Process(-0.5);         // -0.25

// A filter with feedback (b1 != 0) maintains state across calls
var stateful = new Biquad(b0: 0.5, b1: 0.5, b2: 0.0, a1: 0.0, a2: 0.0);
double o1 = stateful.Process(1.0);
double o2 = stateful.Process(0.5);     // differs from o1 due to accumulated state

// Reset clears the internal delay lines
stateful.Reset();
```

## Advanced usage

`LoudnessMeter` is built from two public building blocks in `NAudio.Loudness.Filters`.
You can use them directly when you need the K-weighted signal itself rather than a LUFS number.

- **`Biquad`** is a direct-form I second-order section, `Process(double)` / `Reset()`. It holds
  its own delay line, so use one instance per channel.
- **`KWeightingFilter`** is the BS.1770 K-weighting: the high-shelf pre-filter followed by the RLB
  high-pass, each a `Biquad`. Its coefficients are derived for the sample rate you pass to the constructor.
  It also keeps per-instance state, so use one instance per channel.

### K-weighted RMS of a float buffer

This example builds a stereo test tone, runs each channel through its own `KWeightingFilter`
in place, and computes the RMS of the weighted signal.

```csharp
using System;
using NAudio.Loudness.Filters;

const int sampleRate = 48000;
const int channels = 2;
const int frames = sampleRate; // one second

// Interleaved test signal: 1 kHz sine at -20 dBFS on both channels.
var buffer = new float[frames * channels];
for (int n = 0; n < frames; n++)
{
    float v = 0.1f * (float)Math.Sin(2.0 * Math.PI * 1000.0 * n / sampleRate);
    for (int c = 0; c < channels; c++)
        buffer[n * channels + c] = v;
}

// One filter per channel: the filter state must not be shared across channels.
var filters = new KWeightingFilter[channels];
for (int c = 0; c < channels; c++)
    filters[c] = new KWeightingFilter(sampleRate);

// Filter in place, interleaved.
for (int i = 0; i < buffer.Length; i++)
    buffer[i] = (float)filters[i % channels].Process(buffer[i]);

// RMS of the weighted signal.
double sumOfSquares = 0.0;
foreach (float s in buffer)
    sumOfSquares += (double)s * s;

double rms = Math.Sqrt(sumOfSquares / buffer.Length);
Console.WriteLine($"K-weighted RMS: {rms:0.0000} ({20.0 * Math.Log10(rms):0.0} dBFS)");
```

`KWeightingFilter.ProcessSamples(double[])` does the same for a `double[]` of one channel.
It returns a new array rather than filtering in place.

### The BS.1770 filter stages

`KWeightingFilter` is the cascade of two `Biquad` stages. The nominal parameters below are the
48 kHz values from BS.1770. The code re-derives them for other sample rates by bilinear transform.

| Stage | Type | Nominal frequency | Gain | Q | Purpose |
| --- | --- | --- | --- | --- | --- |
| 1. Pre-filter | 2nd-order high shelf | 1681.97 Hz | +4.0 dB | 0.7072 | Models the acoustic effect of the head |
| 2. RLB high-pass | 2nd-order high-pass | 38.14 Hz | 0 dB | 0.5003 | Removes sub-audible content (revised low-frequency B-curve) |

### `LoudnessConstants`

`LoudnessConstants` is a public static class with the values the meter uses:

- **Window sizes:** `SubBlockSeconds` (0.1 s), `MomentaryBlocks` (4, so 400 ms),
  `ShortTermBlocks` (30, so 3 s), and `GatingOverlapFactor` (0.75).
- **Integrated gating:** `AbsoluteGateLufs` (-70 LUFS) and `RelativeGateLu` (-10 LU).
- **Loudness range gating:** `LraAbsoluteGateThreshold` (-70 LUFS) and `LraRelativeGateOffset` (-20 LU).

Reference them when your own code needs to match the meter's windows or gates. They are `const`, so
they cannot be changed at runtime.

### Normalization to -23 LUFS (EBU R128)

This example measures a buffer, computes the gain as `target - measured` in dB, and applies that gain
to the samples. It converts the gain to a linear factor first.

```csharp
using System;
using NAudio.Loudness;

const int sampleRate = 48000;
const int channels = 2;
const double targetLufs = -23.0; // EBU R128 programme loudness

// Stand-in for real audio: a 1 kHz sine at -20 dBFS, stereo, 10 s.
var samples = new float[sampleRate * channels * 10];
for (int n = 0; n < sampleRate * 10; n++)
{
    float v = 0.1f * (float)Math.Sin(2.0 * Math.PI * 1000.0 * n / sampleRate);
    samples[n * channels] = v;
    samples[n * channels + 1] = v;
}

// 1. Measure.
var meter = new LoudnessMeter(sampleRate, channels);
meter.AddSamples(samples);
double measuredLufs = meter.IntegratedLufs;
if (double.IsNegativeInfinity(measuredLufs))
    throw new InvalidOperationException("Input is silent or below the -70 LUFS gate; nothing to normalize.");

// 2. Gain in dB.
double gainDb = targetLufs - measuredLufs;

// 3. Apply to the samples (dB -> linear factor).
float linearGain = (float)Math.Pow(10.0, gainDb / 20.0);
for (int i = 0; i < samples.Length; i++)
    samples[i] *= linearGain;

// Re-measure to confirm the target.
var check = new LoudnessMeter(sampleRate, channels);
check.AddSamples(samples);
Console.WriteLine($"Measured {measuredLufs:0.00} LUFS, gain {gainDb:+0.00;-0.00} dB, now {check.IntegratedLufs:0.00} LUFS");
```

Check the true peak before writing the result. A large gain can push peaks over full scale,
and the scaling loop above does not limit them. To normalize with a true-peak ceiling, use
`LoudnessNormalizingSampleProvider` with `truePeakCeilingDb: -1.0` instead.

## License

MIT
