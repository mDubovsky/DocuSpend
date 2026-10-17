using DocuSpend.Statements.Reading;

namespace DocuSpend.Statements.Tests;

/// <summary>Statement header, summary totals and period (spec: What a statement contains).</summary>
public class HeaderTests
{
    private static readonly VubStatementReader Reader = new();

    public static TheoryData<string> Valid => new(Samples.Valid);

    [Theory]
    [MemberData(nameof(Valid))]
    public void Header_totals_and_period_match_the_record(string name)
    {
        var result = Reader.Read(Samples.Pdf(name));
        Assert.True(result.IsSuccess, result.Rejection?.Message);

        Assert.Equal(
            RecordMapping.CanonicalJson(Samples.Record(name).Statement!),
            RecordMapping.CanonicalJson(RecordMapping.StatementOnly(result.Statement!)));
    }

    [Fact]
    public void The_empty_month_reads_to_exactly_its_record()
    {
        var result = Reader.Read(Samples.Pdf("04-empty-month"));

        Assert.True(result.IsSuccess, result.Rejection?.Message);
        Assert.Equal(RecordMapping.Canonical(Samples.Record("04-empty-month")), RecordMapping.ToCanonicalJson("04-empty-month", result.Statement!));
    }
}
