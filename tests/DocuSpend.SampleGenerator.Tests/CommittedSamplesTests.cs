using DocuSpend.SampleGenerator.Records;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;

namespace DocuSpend.SampleGenerator.Tests;

/// <summary>
/// The files committed in samples/public must be exactly what the generator produces, so they can't drift.
/// After a deliberate change, regenerate with: dotnet run --project tools/DocuSpend.SampleGenerator -- generate
/// </summary>
public class CommittedSamplesTests
{
    private static readonly string Dir = FindSamplesDir();

    public static TheoryData<string> ValidNames => new(ValidSamples.All().Select(s => s.Name));
    public static TheoryData<string> InvalidNames => new(InvalidSamples.All().Select(s => s.Name));

    [Fact]
    public void All_nine_samples_and_their_records_are_committed()
    {
        var expected = ValidSamples.All().Select(s => s.Name).Concat(InvalidSamples.All().Select(s => s.Name))
            .SelectMany(n => new[] { n + ".expected.json", n + ".pdf" })
            .Order(StringComparer.Ordinal);
        var actual = Directory.EnumerateFiles(Dir).Select(Path.GetFileName).Where(f => f != "README.md")
            .Order(StringComparer.Ordinal);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ValidNames))]
    public void Committed_valid_sample_matches_the_generator(string name)
    {
        var scenario = ValidSamples.All().Single(s => s.Name == name);

        Assert.Equal(VubStatementRenderer.Render(scenario), File.ReadAllBytes(Path.Combine(Dir, name + ".pdf")));
        Assert.Equal(RecordWriter.Serialize(ExpectedRecord.ForValid(scenario)), File.ReadAllText(Path.Combine(Dir, name + ".expected.json")));
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Committed_invalid_sample_matches_the_generator(string name)
    {
        var sample = InvalidSamples.All().Single(s => s.Name == name);

        Assert.Equal(sample.Content, File.ReadAllBytes(Path.Combine(Dir, name + ".pdf")));
        Assert.Equal(RecordWriter.Serialize(sample.Record), File.ReadAllText(Path.Combine(Dir, name + ".expected.json")));
    }

    [Theory]
    [MemberData(nameof(ValidNames))]
    public void Committed_record_describes_its_pdf(string name)
    {
        var record = RecordWriter.Deserialize(File.ReadAllText(Path.Combine(Dir, name + ".expected.json")));
        var words = PdfText.Words(File.ReadAllBytes(Path.Combine(Dir, name + ".pdf"))).Select(w => w.Text).ToHashSet();

        Assert.Equal("parse", record.Expectation);
        Assert.Contains(VubFormat.Amount(record.Statement!.ClosingBalance), words);
        Assert.All(record.Transactions!, t => Assert.Contains(VubFormat.Amount(t.FeeAmount ?? t.Amount), words));
    }

    private static string FindSamplesDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return Path.Combine(dir.FullName, "samples", "public");
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
