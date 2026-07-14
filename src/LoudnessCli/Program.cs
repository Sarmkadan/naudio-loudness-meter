using NAudio.Loudness;
using NAudio.Wave;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return 0;
}

switch (args[0])
{
    case "scan":
        return Scan(args.Skip(1).ToArray());
    case "normalize":
        return Normalize(args.Skip(1).ToArray());
    default:
        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
        PrintUsage();
        return 1;
}

static int Scan(string[] a)
{
    if (a.Length < 1)
    {
        Console.Error.WriteLine("usage: loudness scan <input.wav>");
        return 1;
    }

    using var reader = new AudioFileReader(a[0]);
    var result = reader.ToSampleProvider().MeasureLoudness();

    Console.WriteLine($"File:          {Path.GetFileName(a[0])}");
    Console.WriteLine($"Format:        {reader.WaveFormat.SampleRate} Hz, {reader.WaveFormat.Channels} ch");
    Console.WriteLine($"Integrated:    {Fmt(result.IntegratedLufs)} LUFS");
    Console.WriteLine($"Loudness range:{result.LoudnessRange,7:0.0} LU");
    Console.WriteLine($"True peak:     {Fmt(result.TruePeakDb)} dBTP");
    Console.WriteLine($"Sample peak:   {Fmt(result.SamplePeakDb)} dBFS");
    Console.WriteLine($"Gain to -23:   {SignedLu(result.GainToReach(-23.0))} LU");
    return 0;
}

static int Normalize(string[] a)
{
    if (a.Length < 2)
    {
        Console.Error.WriteLine("usage: loudness normalize <input.wav> <output.wav> [targetLufs=-23] [ceilingDbtp=-1]");
        return 1;
    }

    string input = a[0], output = a[1];
    double target = a.Length > 2 ? double.Parse(a[2]) : -23.0;
    double ceiling = a.Length > 3 ? double.Parse(a[3]) : -1.0;

    double gain;
    using (var probe = new AudioFileReader(input))
    {
        var measured = probe.ToSampleProvider().MeasureLoudness();
        gain = measured.GainToReach(target);
        Console.WriteLine($"Measured {Fmt(measured.IntegratedLufs)} LUFS -> applying {SignedLu(gain)} LU");
    }

    using (var reader = new AudioFileReader(input))
    {
        var normalized = new LoudnessNormalizingSampleProvider(
            reader.ToSampleProvider(), gain, ceiling);
        WaveFileWriter.CreateWaveFile16(output, normalized);
    }

    Console.WriteLine($"Wrote {output}");
    return 0;
}

static string Fmt(double v) => double.IsNegativeInfinity(v) ? "  -inf" : $"{v,7:0.0}";

static string SignedLu(double v) => (v >= 0 ? "+" : "") + v.ToString("0.0");

static void PrintUsage()
{
    Console.WriteLine("loudness - EBU R128 / BS.1770 metering for NAudio");
    Console.WriteLine();
    Console.WriteLine("  loudness scan <input.wav>");
    Console.WriteLine("  loudness normalize <input.wav> <output.wav> [targetLufs=-23] [ceilingDbtp=-1]");
}
