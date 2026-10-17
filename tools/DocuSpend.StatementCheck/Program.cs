using DocuSpend.Statements.Reading;
using UglyToad.PdfPig;

// Runs the statement reader over real statements, locally. Prints per file only its name and either the counts
// or the rejection; never names, amounts or any other content. Rejection messages never contain content either.

var files = args.Length > 0 ? args.SelectMany(Expand).ToList() : Expand(DefaultDir()).ToList();
if (files.Count == 0)
{
    Console.Error.WriteLine("No PDF files found. Usage: StatementCheck [<file.pdf | directory>...] (default: samples/private)");
    return 2;
}

var reader = new VubStatementReader();
var failures = 0;
foreach (var file in files)
{
    var bytes = File.ReadAllBytes(file);
    var result = reader.Read(bytes);
    if (result.Statement is { } statement)
    {
        Console.WriteLine($"{Path.GetFileName(file)}: OK ({Pages(bytes)} pages, {statement.Transactions.Count} transactions)");
    }
    else
    {
        failures++;
        Console.WriteLine($"{Path.GetFileName(file)}: REJECTED ({result.Rejection!.Kind}) {result.Rejection.Message}");
    }
}
return failures == 0 ? 0 : 1;

static IEnumerable<string> Expand(string path) =>
    Directory.Exists(path)
        ? Directory.EnumerateFiles(path, "*.pdf").Order(StringComparer.Ordinal)
        : [path];

static int Pages(byte[] pdf)
{
    using var doc = PdfDocument.Open(pdf);
    return doc.NumberOfPages;
}

/// <summary>The repository's samples/private, found by walking up from the current directory to DocuSpend.slnx.</summary>
static string DefaultDir()
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
            return Path.Combine(dir.FullName, "samples", "private");
    }
    throw new InvalidOperationException("Run inside the DocuSpend repository, or pass files or directories.");
}
