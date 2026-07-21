using NAudio.Loudness;
using NAudio.Wave;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

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
        Console.Error.WriteLine("usage: loudness scan <input.wav> [input2.wav ...] or <directory> [--json]");
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
        // Build a single JSON object where each property is the file name
        // and its value is an object containing the full analysis.
        var jsonObject = new Dictionary<string, object>();
        for (int i = 0; i < files.Count; i++)
        {
            var r = results[i];
            var fileName = Path.GetFileName(files[i]);
            jsonObject[fileName] = new
            {
                integratedLufs = r.IntegratedLufs,
                momentaryMax = r.MomentaryMax,
                shortTermMax = r.ShortTermMax,
                truePeakDb = r.TruePeakDb
            };
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonLoudnessConverter() }
        };
        Console.WriteLine(JsonSerializer.Serialize(jsonObject, options));
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
        Console.Error.WriteLine("usage: loudness normalize <input.wav> <output.wav> [targetLufs=-23|spotify|ebu|youtube|podcast] [ceilingDbtp=-1]");
        return 1;
    }

    string input = a[0], output = a[1];
    
    double target;
    if (a.Length > 2)
    {
        string t = a[2].ToLower();
        if (t == "spotify" || t == "youtube") target = -14.0;
        else if (t == "ebu") target = -23.0;
        else if (t == "podcast") target = -16.0;
        else if (!double.TryParse(t, out target))
        {
            Console.Error.WriteLine($"Invalid target: '{t}'. Must be a number or one of: spotify, ebu, youtube, podcast.");
            return 1;
        }
    }
    else
    {
        target = -23.0;
    }
    
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
    Console.WriteLine(" loudness normalize <input.wav> <output.wav> [targetLufs=-23|spotify|ebu|youtube|podcast] [ceilingDbtp=-1]");
}

// Custom JSON converter to handle negative infinity values
public class JsonLoudnessConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDouble();

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNegativeInfinity(value))
            writer.WriteStringValue("-inf");
        else
            writer.WriteNumberValue(value);
    }
}
