using DocuSpend.SampleGenerator.Records;

namespace DocuSpend.Statements.Tests;

/// <summary>The committed synthetic samples in samples/public and their expected records.</summary>
public static class Samples
{
    public static readonly string Dir = FindDir();

    public static readonly string[] Valid =
        ["01-typical-month", "02-busy-month", "03-awkward-month", "04-empty-month", "05-single-transaction-month"];

    public static readonly string[] Invalid =
        ["06-other-bank-statement", "07-not-a-statement", "08-corrupted-statement", "09-empty-file"];

    public static byte[] Pdf(string name) => File.ReadAllBytes(Path.Combine(Dir, name + ".pdf"));

    public static string RecordJson(string name) => File.ReadAllText(Path.Combine(Dir, name + ".expected.json"));

    public static ExpectedRecord Record(string name) => RecordWriter.Deserialize(RecordJson(name));

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
