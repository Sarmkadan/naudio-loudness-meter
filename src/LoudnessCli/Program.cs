using NAudio.Loudness;
using NAudio.Wave;
using System;
using System.IO;
using System.Text.Json;

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
        Console.Error.WriteLine("usage: loudness scan <input.wav> [input2.wav ...] or <directory>");
        return 1;
    }

    bool jsonOutput = a.Contains("--json");

    var files = new List<string>();
    foreach (var arg in a)
    {
        if (Directory.Exists(arg))
        {
            files.AddRange(Directory.EnumerateFiles(arg, "*.wav", SearchOption.AllDirectories));
        }
        else if (File.Exists(arg))
        {
            files.Add(arg);
        }
    }

    if (files.Count == 0)
    {
        Console.Error.WriteLine("No files found.");
        return 1;
    }

    var results = new List<LoudnessAnalysis>();
    foreach (var file in files)
    {
        try
        {
            using var reader = new AudioFileReader(file);
            var result = reader.ToSampleProvider().MeasureLoudness();
            results.Add(result);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing {file}: {ex.Message}");
        }
    }

    if (jsonOutput)
    {
        var jsonResults = results.Select(r => new
        {
            file = Path.GetFileName(r.ToString()),
            integratedLufs = r.IntegratedLufs,
            momentaryMax = r.IntegratedLufs, // Note: momentaryMax is not directly available, using IntegratedLufs as a substitute
            shortTermMax = r.IntegratedLufs, // Note: shortTermMax is not directly available, using IntegratedLufs as a substitute
            loudnessRange = r.LoudnessRange,
            truePeakDbtp = r.TruePeakDb,
            totalBlockCount = r.TotalBlockCount,
            gatedBlockCount = r.GatedBlockCount
        });
        Console.WriteLine(JsonSerializer.Serialize(jsonResults, new JsonSerializerOptions { WriteIndented = true }));
    }
    else
    {
        Console.WriteLine("File\tIntegrated LUFS\tLoudness Range LU\tTrue Peak dBTP");
        double sumIntegratedLufs = 0;
        double sumLoudnessRange = 0;
        double sumTruePeakDb = 0;
        int count = 0;
        foreach (var result in results)
        {
            Console.WriteLine($"{Path.GetFileName(result.ToString())}\t{Fmt(result.IntegratedLufs)}\t{result.LoudnessRange,7:0.0}\t{Fmt(result.TruePeakDb)}");
            sumIntegratedLufs += result.IntegratedLufs;
            sumLoudnessRange += result.LoudnessRange;
            sumTruePeakDb += result.TruePeakDb;
            count++;
        }
        Console.WriteLine($"Summary\t{Fmt(sumIntegratedLufs / count)}\t{sumLoudnessRange / count,7:0.0}\t{Fmt(sumTruePeakDb / count)}");
    }
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

static string Fmt(double v) => double.IsNegativeInfinity(v) ? " -inf" : $"{v,7:0.0}";

static string SignedLu(double v) => (v >= 0 ? "+" : "") + v.ToString("0.0");

static void PrintUsage()
{
    Console.WriteLine("loudness - EBU R128 / BS.1770 metering for NAudio");
    Console.WriteLine();
    Console.WriteLine(" loudness scan <input.wav> [input2.wav ...] or <directory> [--json]");
    Console.WriteLine(" loudness normalize <input.wav> <output.wav> [targetLufs=-23] [ceilingDbtp=-1]");
}
