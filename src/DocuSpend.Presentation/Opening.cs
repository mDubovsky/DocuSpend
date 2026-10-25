namespace DocuSpend.Presentation;

/// <summary>Asks the user for a statement file; the window implements it with Windows' open window.</summary>
public interface IStatementPicker
{
    /// <summary>The chosen file's path, or null when the user cancelled.</summary>
    Task<string?> PickAsync();
}

/// <summary>
/// What was dropped onto the window: the paths of the files in it, and how many other things it held (folders,
/// text, links). Only exactly one file and nothing else is opened (spec: when a file can't be opened).
/// </summary>
public sealed record DroppedContent(IReadOnlyList<string> Files, int Others);

/// <summary>The example statement that travels with DocuSpend, in a folder next to the program (intent: decisions).</summary>
public static class Example
{
    public const string FileName = "01-typical-month.pdf";

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "samples", FileName);
}
