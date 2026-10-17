using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;

namespace DocuSpend.SampleGenerator.Tests;

/// <summary>Test-only rendering options for building inconsistent or damaged statements.</summary>
public class RenderOptionsTests
{
    private static readonly StatementScenario Single = ValidSamples.SingleTransactionMonth();

    private static IReadOnlyList<PdfWord> SummaryValues(byte[] pdf) =>
        PdfText.Words(pdf).Where(w => w.Page == 1 && w.Top < 330 && Math.Abs(w.Right - 215.9) < 1.5).ToList();

    [Fact]
    public void Printed_totals_can_differ_from_the_transactions()
    {
        var printed = Single.Totals with { TotalWithdrawals = -99.99m };
        var pdf = VubStatementRenderer.Render(Single, new RenderOptions { PrintedTotals = printed });

        Assert.Contains(SummaryValues(pdf), w => w.Text == "-99,99");
        Assert.DoesNotContain(SummaryValues(pdf), w => w.Text == VubFormat.Amount(Single.Totals.TotalWithdrawals));
    }

    [Fact]
    public void A_text_transform_can_replace_text()
    {
        var pdf = VubStatementRenderer.Render(Single, new RenderOptions { TextTransform = t => t == "-23,45" ? "-23,4X" : t });
        var words = PdfText.Words(pdf);

        Assert.Contains(words, w => w.Text == "-23,4X");
        Assert.DoesNotContain(words, w => w.Text == "-23,45");
    }

    [Fact]
    public void A_text_transform_can_remove_text()
    {
        var pdf = VubStatementRenderer.Render(Single, new RenderOptions { TextTransform = t => t.StartsWith("Vklady", StringComparison.Ordinal) ? null : t });

        Assert.DoesNotContain(PdfText.Words(pdf), w => w.Text == "Vklady");
    }

    [Fact]
    public void The_synthetic_marking_is_never_transformed()
    {
        var pdf = VubStatementRenderer.Render(Single, new RenderOptions { TextTransform = _ => null });

        Assert.All(PdfText.MarginTexts(pdf), t => Assert.Contains(VubStatementRenderer.SyntheticMarking, t));
        Assert.Contains(PdfText.Words(pdf), w => w.Text == "SYNTHETIC" && w.Top > SyntheticPdf.FooterY - 2);
    }

    [Fact]
    public void Without_options_rendering_is_unchanged() =>
        Assert.Equal(VubStatementRenderer.Render(Single), VubStatementRenderer.Render(Single, new RenderOptions()));
}
