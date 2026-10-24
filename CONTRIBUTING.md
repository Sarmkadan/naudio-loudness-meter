# Contributing to NAudio.Loudness

Thanks for your interest in improving NAudio.Loudness. This guide covers how to build, test, and submit changes.

## Prerequisites

- **.NET SDK 10.x** (CI uses `10.0.x`). The library targets `net8.0`, the CLI and tests target `net10.0`, and the .NET 10 SDK builds all of them. There is no `global.json`.
- **Git**

Do not commit audio samples; `*.wav`, `*.flac`, and `*.mp3` are gitignored.

## Clone and build

```bash
git clone https://github.com/Sarmkadan/naudio-loudness-meter.git
cd naudio-loudness-meter
dotnet build                      # whole solution (LoudnessMeter.slnx)
dotnet build src/NAudio.Loudness  # library only
dotnet build src/LoudnessCli      # CLI (assembly name: loudness)
```

## Run tests

```bash
dotnet test                                                  # all test projects
dotnet test tests/NAudio.Loudness.Tests                      # unit tests only
dotnet test --filter "FullyQualifiedName~LoudnessMeterTests" # a single test class
```

Tests are xUnit (`[Fact]` / `[Theory]`) and do not use mocks. Reference values come from synthetic signals (EBU Tech 3341/3342 test signals, a full-scale 1 kHz sine measuring -3.01 LUFS in mono). Assert with `Assert.InRange` tolerances rather than exact magic numbers.

## Project layout

- `src/NAudio.Loudness/`: the library (namespace `NAudio.Loudness`).
- `src/LoudnessCli/`: the `loudness` CLI (`scan`, `normalize`, `compare`).
- `tests/NAudio.Loudness.Tests/`: unit tests, plus shared helpers (`SignalGenerator.cs`, `ArraySampleProvider.cs`, `EbuTech3341TestSignals.cs`, `EbuTech3342TestSignals.cs`).
- `tests/NAudio.Loudness.IntegrationTests/`: integration tests.

Do not add files to the stray locations left over from earlier AI-assisted editing: `src/*.cs` at the `src/` root, `src/NAudio.Loudness.Tests/`, and the root-level `test_signal.cs`, `Commit message`, and `aider_buildcmd.py`. Canonical sources live in `src/NAudio.Loudness/` and `tests/`.

## Code style

There is no `.editorconfig` and no analyzer ruleset in the repository, and the build does not treat warnings as errors. Follow the conventions below and match the surrounding code.

- `Nullable` and `ImplicitUsings` are enabled in every project. Keep new code null-safe.
- The build should not gain new warnings. The solution currently has two existing `CA2264` warnings in `LoudnessMeterTestsExtensions.cs`; do not add to them.
- File-scoped namespaces. One public type per file, named after the type.
- Public classes are `sealed`. Helper extension classes are `static` and named `<Target><Purpose>Extensions`. `partial` split files use `Name.Partial.cs`.
- Loudness values are `double`. Put units in member names: `IntegratedLufs`, `TruePeakDbtp`, `targetLufs`, `ceilingDbtp`. Put constants in `LoudnessConstants`.
- Constructors and `AddSamples` validate arguments eagerly (`ArgumentOutOfRangeException` / `ArgumentException`), and tests cover that validation.
- The metering hot path must not allocate per block. Use ring buffers, not per-call arrays.
- Cite standards (BS.1770, EBU Tech 3341/3342) in XML doc comments next to the code they justify.
- Test files are named `<Type>Tests.cs`, or `<Type><Aspect>Tests.cs` for compliance, gating, lifecycle, or edge cases. Test method names describe the expected behaviour in words.

## Branch naming

Create a branch from `main` for each change. There is no enforced scheme, but we recommend `<type>/<short-kebab-description>`, where `<type>` matches the commit prefix:

- `feat/true-peak-oversampling`
- `fix/gating-at-silence-boundary`
- `docs/contributing-guide`

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/) prefixes and an imperative subject line:

```
fix: clamp short-term loudness when the window is not yet full
feat: add --json output to the scan command
docs: document integrated loudness gating
```

Common prefixes are `feat:`, `fix:`, `docs:`, and `chore:`. Keep the subject short, and explain the why in the body when it is not obvious.

## Pull request process

1. Push your branch to your fork or to the repository.
2. Open a pull request **against `main`**.
3. CI must pass. The workflow in `.github/workflows/ci.yml` runs on every pull request to `main`. It restores, runs `dotnet build -c Release`, and then runs `dotnet test -c Release`. Fix any failure before requesting review.
4. In the PR description, cover:
   - what changed and why;
   - which standard or behaviour the change relates to, if any (for example, a BS.1770 gating rule);
   - how you tested it, including any new or updated tests;
   - any user-visible change to the public API, the CLI, or the README.
5. Keep PRs focused. One logical change per PR is easier to review.

Thank you for contributing!
