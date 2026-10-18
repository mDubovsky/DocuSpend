using DocuSpend.Categorization;
using DocuSpend.Statements.Reading;
using UglyToad.PdfPig;

// Runs the statement reader over real statements, locally. Prints per file only its name and either the counts
// or the rejection; never names, amounts or any other content. Rejection messages never contain content either.
// With --categorize it also prints how many transactions each built-in category got: counts only, no amounts,
// no merchant or counterparty text. The category names are the product's own.

var categorize = args.Contains("--categorize");
var paths = args.Where(a => a != "--categorize").ToList();
var files = paths.Count > 0 ? paths.SelectMany(Expand).ToList() : Expand(DefaultDir()).ToList();
if (files.Count == 0)
{
    Console.Error.WriteLine("No PDF files found. Usage: StatementCheck [--categorize] [<file.pdf | directory>...] (default: samples/private)");
    return 2;
}

var reader = new VubStatementReader();
var config = CategorizationConfig.Default;
var failures = 0;
foreach (var file in files)
{
    var bytes = File.ReadAllBytes(file);
    var result = reader.Read(bytes);
    if (result.Statement is { } statement)
    {
        Console.WriteLine($"{Path.GetFileName(file)}: OK ({Pages(bytes)} pages, {statement.Transactions.Count} transactions)");
        if (categorize)
            PrintCategories(Categorizer.Categorize(statement, config));
    }
    else
    {
        failures++;
        Console.WriteLine($"{Path.GetFileName(file)}: REJECTED ({result.Rejection!.Kind}) {result.Rejection.Message}");
    }
}
return failures == 0 ? 0 : 1;

static void PrintCategories(DocuSpend.Categorization.Results.CategorizedStatement result)
{
    var total = result.Transactions.Count;
    var byRules = result.Transactions.Count(t => t.Rule is not null);
    var share = total == 0 ? 0 : 100 * byRules / total;
    Console.WriteLine($"  by default rules: {byRules} of {total} ({share}%); uncategorized: {total - byRules}; adds up: {(result.IsComplete ? "yes" : "NO")}");
    foreach (var category in result.Categories.Where(c => c.Total.Count > 0))
        Console.WriteLine($"  {category.Category.Name}: {category.Total.Count}");
}

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
