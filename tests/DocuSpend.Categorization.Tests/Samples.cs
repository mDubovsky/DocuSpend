using DocuSpend.Statements.Model;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Categorization.Tests;

/// <summary>The committed synthetic samples in samples/public, read with the real statement reader.</summary>
internal static class Samples
{
    public static readonly string[] Valid =
        ["01-typical-month", "02-busy-month", "03-awkward-month", "04-empty-month", "05-single-transaction-month"];

    private static readonly string Dir = FindDir();

    public static Statement Read(string name)
    {
        var result = new VubStatementReader().Read(File.ReadAllBytes(Path.Combine(Dir, name + ".pdf")));
        Assert.True(result.IsSuccess, result.Rejection?.Message);
        return result.Statement!;
    }

    private static string FindDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return Path.Combine(dir.FullName, "samples", "public");
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
