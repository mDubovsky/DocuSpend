using DocuSpend.SampleGenerator.LeakChecking;
using DocuSpend.SampleGenerator.Records;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;
using UglyToad.PdfPig;

const string Usage = """
    Usage:
      generate [--out <dir>]                       Write all samples and their records (default: samples/public)
      leak-check <real.pdf>... [--samples <dir>]   Check samples for values from real statements (run locally only)
    """;

if (args.Length == 0)
{
    Console.Error.WriteLine(Usage);
    return 2;
}

var options = ParseOptions(args.Skip(1).ToArray(), out var positional);
var samplesDir = options.GetValueOrDefault("out") ?? options.GetValueOrDefault("samples") ?? DefaultSamplesDir();

switch (args[0])
{
    case "generate":
        Generate(samplesDir);
        return 0;

    case "leak-check" when positional.Count > 0:
        var report = LeakCheck.Run(positional.SelectMany(ReadWords), TemplateVocabulary.Words(), ReadSamples(samplesDir));
        Console.Write(report);
        return report.HasStrongMatches ? 1 : 0;

    default:
        Console.Error.WriteLine(Usage);
        return 2;
}

static void Generate(string dir)
{
    Directory.CreateDirectory(dir);
    foreach (var scenario in ValidSamples.All())
        Write(dir, scenario.Name, VubStatementRenderer.Render(scenario), ExpectedRecord.ForValid(scenario));
    foreach (var sample in InvalidSamples.All())
        Write(dir, sample.Name, sample.Content, sample.Record);
    Console.WriteLine($"Wrote {ValidSamples.All().Count + InvalidSamples.All().Count} samples to {Path.GetFullPath(dir)}");
}

static void Write(string dir, string name, byte[] pdf, ExpectedRecord record)
{
    File.WriteAllBytes(Path.Combine(dir, name + ".pdf"), pdf);
    File.WriteAllText(Path.Combine(dir, name + ".expected.json"), RecordWriter.Serialize(record));
}

/// <summary>Words of a real statement, kept in memory only.</summary>
static IEnumerable<string> ReadWords(string path)
{
    using var doc = PdfDocument.Open(path);
    return doc.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text).ToList();
}

static IEnumerable<SampleText> ReadSamples(string dir)
{
    foreach (var file in Directory.EnumerateFiles(dir).Order(StringComparer.Ordinal))
    {
        var name = Path.GetFileName(file);
        if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            yield return new SampleText(name, File.ReadAllText(file));
        }
        else if (file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            string? text = null;
            try
            {
                using var doc = PdfDocument.Open(file);
                text = string.Join(' ', doc.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text));
            }
            catch (Exception)
            {
                // Corrupted and empty samples can't be read; their content is covered by the samples they derive from.
            }
            if (text is not null)
                yield return new SampleText(name, text);
        }
    }
}

/// <summary>The repository's samples/public, found by walking up from the current directory to DocuSpend.slnx.</summary>
static string DefaultSamplesDir()
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
            return Path.Combine(dir.FullName, "samples", "public");
    }
    throw new InvalidOperationException("Run inside the DocuSpend repository, or pass --out/--samples.");
}

static Dictionary<string, string> ParseOptions(string[] rest, out List<string> positional)
{
    var options = new Dictionary<string, string>();
    positional = [];
    for (var i = 0; i < rest.Length; i++)
    {
        if (rest[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < rest.Length)
            options[rest[i][2..]] = rest[++i];
        else
            positional.Add(rest[i]);
    }
    return options;
}
