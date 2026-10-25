using DocuSpend.Statements.Model;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Presentation.Tests;

/// <summary>The committed synthetic samples in samples/public, read with the real statement reader.</summary>
internal static class Samples
{
    public static readonly string[] Valid =
        ["01-typical-month", "02-busy-month", "03-awkward-month", "04-empty-month", "05-single-transaction-month"];

    public static readonly string Dir = FindDir();

    public static readonly string[] Invalid =
        ["06-other-bank-statement", "07-not-a-statement", "08-corrupted-statement", "09-empty-file"];

    /// <summary>The sample's PDF file.</summary>
    public static string Path(string name) => System.IO.Path.Combine(Dir, name + ".pdf");

    public static Statement Read(string name)
    {
        var result = new VubStatementReader().Read(File.ReadAllBytes(Path(name)));
        Assert.True(result.IsSuccess, result.Rejection?.Message);
        return result.Statement!;
    }

    private static string FindDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return System.IO.Path.Combine(dir.FullName, "samples", "public");
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
