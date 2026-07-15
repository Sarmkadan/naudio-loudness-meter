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
