namespace DocuSpend.Presentation;

/// <summary>A synthetic sample statement the application can show.</summary>
public sealed record SampleItem(string Name, string Title, string Path);

/// <summary>
/// The valid synthetic samples, 01 to 05, which travel with the application in a folder next to the program. The
/// spike never opens anything else: no file picker, no samples/private (spec: starting and choosing a sample).
/// </summary>
public static class SampleCatalog
{
    private static readonly (string Name, string Title)[] Known =
    [
        ("01-typical-month", Texts.SampleTypicalMonth),
        ("02-busy-month", Texts.SampleBusyMonth),
        ("03-awkward-month", Texts.SampleAwkwardMonth),
        ("04-empty-month", Texts.SampleEmptyMonth),
        ("05-single-transaction-month", Texts.SampleSingleTransactionMonth),
    ];

    /// <summary>The known samples present in the folder, in order; the first is shown at start.</summary>
    public static IReadOnlyList<SampleItem> Available(string folder) => Known
        .Select(k => new SampleItem(k.Name, k.Title, System.IO.Path.Combine(folder, k.Name + ".pdf")))
        .Where(s => File.Exists(s.Path))
        .ToList();
}
