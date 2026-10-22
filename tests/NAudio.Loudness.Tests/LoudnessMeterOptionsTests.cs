using Xunit;

namespace NAudio.Loudness.Tests;

/// <summary>
/// Unit tests for <see cref="LoudnessMeterOptions"/> defaults, each validation rule,
/// and the <see cref="LoudnessMeter"/> options constructor.
/// </summary>
public class LoudnessMeterOptionsTests
{
    [Fact]
    public void Defaults_MatchBs1770Values()
    {
        var options = new LoudnessMeterOptions();

        Assert.Equal(48000, options.SampleRate);
        Assert.Equal(2, options.Channels);
        Assert.Equal(400, options.MomentaryWindowMs);
        Assert.Equal(3000, options.ShortTermWindowMs);
        Assert.Equal(-70.0, options.AbsoluteGateLufs);
        Assert.Equal(-10.0, options.RelativeGateLu);
    }

    [Fact]
    public void Defaults_AreTakenFromLoudnessConstants()
    {
        var options = new LoudnessMeterOptions();

        Assert.Equal(LoudnessConstants.AbsoluteGateLufs, options.AbsoluteGateLufs);
        Assert.Equal(LoudnessConstants.RelativeGateLu, options.RelativeGateLu);
        Assert.Equal((int)Math.Round(LoudnessConstants.MomentaryBlocks * LoudnessConstants.SubBlockSeconds * 1000.0), options.MomentaryWindowMs);
        Assert.Equal((int)Math.Round(LoudnessConstants.ShortTermBlocks * LoudnessConstants.SubBlockSeconds * 1000.0), options.ShortTermWindowMs);
    }

    [Fact]
    public void Defaults_PassValidation()
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions().Validate());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(44100)]
    [InlineData(384000)]
    public void Validate_AcceptsSampleRate_WithinRange(int sampleRate)
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions { SampleRate = sampleRate }.Validate());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7999)]
    [InlineData(384001)]
    [InlineData(-48000)]
    public void Validate_RejectsSampleRate_OutsideRange(int sampleRate)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new LoudnessMeterOptions { SampleRate = sampleRate }.Validate());

        Assert.Equal("SampleRate", ex.ParamName);
        Assert.Contains("8000", ex.Message);
        Assert.Contains("384000", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Validate_AcceptsChannels_WithinRange(int channels)
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions { Channels = channels }.Validate());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Validate_RejectsChannels_OutsideRange(int channels)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new LoudnessMeterOptions { Channels = channels }.Validate());

        Assert.Equal("Channels", ex.ParamName);
        Assert.Contains("BS.1770", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(400)]
    public void Validate_AcceptsPositiveMomentaryWindow(int windowMs)
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions { MomentaryWindowMs = windowMs }.Validate());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-400)]
    public void Validate_RejectsNonPositiveMomentaryWindow(int windowMs)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new LoudnessMeterOptions { MomentaryWindowMs = windowMs }.Validate());

        Assert.Equal("MomentaryWindowMs", ex.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3000)]
    public void Validate_AcceptsPositiveShortTermWindow(int windowMs)
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions
        {
            MomentaryWindowMs = 1,
            ShortTermWindowMs = windowMs
        }.Validate());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3000)]
    public void Validate_RejectsNonPositiveShortTermWindow(int windowMs)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new LoudnessMeterOptions { ShortTermWindowMs = windowMs }.Validate());

        Assert.Equal("ShortTermWindowMs", ex.ParamName);
    }

    [Fact]
    public void Validate_AcceptsShortTermWindow_EqualToMomentary()
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions
        {
            MomentaryWindowMs = 1000,
            ShortTermWindowMs = 1000
        }.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_RejectsShortTermWindow_ShorterThanMomentary()
    {
        var ex = Assert.Throws<ArgumentException>(() => new LoudnessMeterOptions
        {
            MomentaryWindowMs = 400,
            ShortTermWindowMs = 300
        }.Validate());

        Assert.Equal("ShortTermWindowMs", ex.ParamName);
        Assert.Contains("MomentaryWindowMs", ex.Message);
    }

    [Theory]
    [InlineData(-70.0)]
    [InlineData(-23.0)]
    public void Validate_AcceptsNegativeAbsoluteGate(double gate)
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions { AbsoluteGateLufs = gate }.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_AcceptsAbsoluteGate_CloseToZero_WhenRelativeGateIsAlsoAboveIt()
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions
        {
            AbsoluteGateLufs = -0.2,
            RelativeGateLu = -0.1
        }.Validate());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.0)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_RejectsNonNegativeOrNonFiniteAbsoluteGate(double gate)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new LoudnessMeterOptions { AbsoluteGateLufs = gate }.Validate());

        Assert.Equal("AbsoluteGateLufs", ex.ParamName);
    }

    [Theory]
    [InlineData(-10.0)]
    [InlineData(-69.0)]
    public void Validate_AcceptsNegativeRelativeGate_AboveAbsoluteGate(double gate)
    {
        var exception = Record.Exception(() => new LoudnessMeterOptions { RelativeGateLu = gate }.Validate());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_RejectsNonNegativeOrNonFiniteRelativeGate(double gate)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new LoudnessMeterOptions { RelativeGateLu = gate }.Validate());

        Assert.Equal("RelativeGateLu", ex.ParamName);
    }

    [Theory]
    [InlineData(-70.0)]
    [InlineData(-71.0)]
    public void Validate_RejectsRelativeGate_NotAboveAbsoluteGate(double relativeGate)
    {
        var ex = Assert.Throws<ArgumentException>(() => new LoudnessMeterOptions
        {
            AbsoluteGateLufs = -70.0,
            RelativeGateLu = relativeGate
        }.Validate());

        Assert.Equal("RelativeGateLu", ex.ParamName);
        Assert.Contains("AbsoluteGateLufs", ex.Message);
    }

    [Fact]
    public void Constructor_WithOptions_CreatesMeterWithOptionsSampleRateAndChannels()
    {
        var meter = new LoudnessMeter(new LoudnessMeterOptions { SampleRate = 44100, Channels = 1 });

        Assert.Equal(44100, meter.SampleRate);
    }

    [Fact]
    public void Constructor_WithOptions_ThrowsForInvalidOptions_AtConstruction()
    {
        var options = new LoudnessMeterOptions { Channels = 6 };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LoudnessMeter(options));

        Assert.Equal("Channels", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithOptions_ThrowsArgumentNullException_WhenOptionsAreNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new LoudnessMeter((LoudnessMeterOptions)null!));

        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithDefaultOptions_MeasuresSameAsLegacyConstructor()
    {
        const int fs = 48000;
        var samples = SignalGenerator.Sine(1000, 0.1, fs, 5, 2);

        var legacy = new LoudnessMeter(fs, 2);
        legacy.AddSamples(samples);
        var configured = new LoudnessMeter(new LoudnessMeterOptions { SampleRate = fs, Channels = 2 });
        configured.AddSamples(samples);

        Assert.Equal(legacy.IntegratedLufs, configured.IntegratedLufs, 9);
        Assert.Equal(legacy.ShortTermLufs, configured.ShortTermLufs, 9);
        Assert.Equal(legacy.MomentaryLufs, configured.MomentaryLufs, 9);
    }

    [Fact]
    public void Constructor_WithShorterShortTermWindow_ReportsShortTermSooner()
    {
        const int fs = 48000;
        var meter = new LoudnessMeter(new LoudnessMeterOptions
        {
            SampleRate = fs,
            Channels = 1,
            ShortTermWindowMs = 1000
        });

        // 1.5 s is enough for a 1 s window but not for the default 3 s window.
        var defaultWindow = new LoudnessMeter(fs, 1);
        var samples = SignalGenerator.Sine(1000, 0.5, fs, 1.5, 1);
        meter.AddSamples(samples);
        defaultWindow.AddSamples(samples);

        Assert.True(double.IsFinite(meter.ShortTermLufs));
        Assert.Equal(double.NegativeInfinity, defaultWindow.ShortTermLufs);
    }
}
