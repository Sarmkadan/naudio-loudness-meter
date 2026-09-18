# CLAUDE.md

NAudio.Loudness: pure-managed C# EBU R128 / ITU-R BS.1770 loudness metering (momentary, short-term, integrated LUFS, LRA, true peak) and normalization for NAudio `ISampleProvider`, plus a `loudness` CLI.

## Build

```
dotnet build                      # whole solution (LoudnessMeter.slnx)
dotnet build src/NAudio.Loudness  # library only (net8.0, packable, v0.4.0)
dotnet build src/LoudnessCli      # CLI (net10.0, assembly name `loudness`)
```

Requires .NET SDK 10.x (library targets net8.0; CLI and tests target net10.0). `build.sh` is just `dotnet build`.

## Test

```
dotnet test                                                  # all (~150 xUnit tests)
dotnet test tests/NAudio.Loudness.Tests                      # test project only
dotnet test --filter "FullyQualifiedName~LoudnessMeterTests" # single class
```

Tests are xUnit (`[Fact]`/`[Theory]`), no mocks. Reference values come from synthetic signals (EBU Tech 3341/3342 test signals, full-scale 1 kHz sine = -3.01 LUFS mono), asserted with `Assert.InRange` tolerances, not magic numbers.

## Lint / format

No `.editorconfig`, analyzers or CI. `Nullable` and `ImplicitUsings` are enabled in every project; keep the build at 0 warnings. XML doc warnings CS1591/CS1573 are suppressed in the library.

## Layout and entry points

```
LoudnessMeter.slnx                          solution (slnx format)
src/NAudio.Loudness/                        the library, namespace NAudio.Loudness
  LoudnessMeter.cs                          core streaming meter (AddSamples / Reset, momentary/short-term/integrated, gating)
  TruePeakMeter.cs                          4x oversampled true-peak meter
  LoudnessRangeCalculator.cs                LRA (EBU Tech 3342)
  LoudnessAnalysis.cs                       result record (+ GainToReach)
  LoudnessNormalizingSampleProvider.cs      two-pass static-gain normalizer (ISampleProvider)
  OnePassAdaptiveLoudnessNormalizingSampleProvider.cs  streaming normalizer
  SampleProviderLoudnessExtensions.cs       `MeasureLoudness()` entry point on ISampleProvider
  Filters/Biquad.cs, Filters/KWeightingFilter.cs       K-weighting re-derived per sample rate
  *JsonExtensions.cs, *FormattingExtensions.cs         System.Text.Json / ToString helpers
  LoudnessConstants.cs, ChannelWeights.cs, LoudnessMeterValidation.cs
src/LoudnessCli/Program.cs                  top-level statements; commands: scan, normalize, compare
tests/NAudio.Loudness.Tests/                xUnit; SignalGenerator.cs, ArraySampleProvider.cs, EbuTech334x TestSignals.cs are shared helpers
docs/                                       notes on test extension classes
```

Stray files not part of any project (leftovers from AI-assisted editing, do not extend them): `src/*.cs` at the `src/` root, `src/NAudio.Loudness.Tests/`, root-level `LoudnessAnalysisJsonExtensions.cs`, `test_signal.cs`, `Commit message`, `aider_buildcmd.py`. Canonical sources live in `src/NAudio.Loudness/` and `tests/NAudio.Loudness.Tests/`.

## Conventions

- File-scoped namespaces; one public type per file, file named after the type.
- Public classes are `sealed`; helpers are `static class` extension holders named `<Target><Purpose>Extensions` (e.g. `LoudnessMeterJsonExtensions`); `partial` split files use `Name.Partial.cs`.
- Loudness values are `double`; units in member names: `IntegratedLufs`, `TruePeakDbtp`, `targetLufs`, `ceilingDbtp`. Constants live in `LoudnessConstants`.
- Constructors and `AddSamples` validate arguments eagerly (`ArgumentOutOfRangeException` / `ArgumentException`); tests cover the validation.
- Hot path (metering) must be allocation-free per block; use ring buffers, not per-call arrays.
- Test files: `<Type>Tests.cs`, plus `<Type><Aspect>Tests.cs` for compliance/gating/lifecycle/edge cases; test method names describe the expected behaviour in words.
- Standards references (BS.1770, EBU Tech 3341/3342) go in XML doc comments next to the code they justify.
- Commits: conventional prefixes (`fix:`, `feat:`, `chore:`, `docs:`), imperative subject.
- Do not commit audio samples (`*.wav`, `*.flac`, `*.mp3` are gitignored).
