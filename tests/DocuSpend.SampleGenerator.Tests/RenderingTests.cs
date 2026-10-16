using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Rendering;

namespace DocuSpend.SampleGenerator.Tests;

public class RenderingTests
{
    private static readonly DateOnly Start = new(2026, 9, 1);
    private static readonly DateOnly End = new(2026, 9, 30);

    private static Transaction Card(int day, decimal amount, string merchant = "OBCHOD TEST MESTO") => new()
    {
        BookingDate = new DateOnly(2026, 9, day),
        ValueDate = new DateOnly(2026, 9, Math.Max(1, day - 1)),
        Type = TransactionType.CardPayment,
        Amount = amount,
        Card = "4321",
        Description = merchant,
        Reference = $"{day:00}09000000001",
    };

    private static Transaction Transfer(int day, decimal amount) => new()
    {
        BookingDate = new DateOnly(2026, 9, day),
        ValueDate = new DateOnly(2026, 9, day),
        Type = TransactionType.Transfer,
        Amount = amount,
        Description = "Platba za služby",
        CounterpartyName = "Ján Testovací",
        CounterpartyAccount = "SK12 1100 0000 0012 3456 7890",
        CounterpartyBic = "TATRSKBX",
        VariableSymbol = "1234567",
        ConstantSymbol = "0308",
        PaymentPurpose = "Platba za služby",
        Reference = "2609010ABCDEF",
    };

    private static StatementScenario Scenario(params Transaction[] transactions) => new()
    {
        Name = "render-test",
        AccountHolder = "JANA VZOROVÁ",
        HolderAddress = ["Lipová 7", "917 01 Trnava"],
        Iban = "SK31 0200 0000 0012 3456 7890",
        PeriodStart = Start,
        PeriodEnd = End,
        OpeningBalance = 1000.00m,
        Transactions = transactions,
    };

    private static IReadOnlyList<PdfWord> Words(StatementScenario s) => PdfText.Words(VubStatementRenderer.Render(s));

    /// <summary>The amount in the table's "Suma" column (the same value may also appear in the balance summary).</summary>
    private static PdfWord TableAmount(IEnumerable<PdfWord> words, string text) =>
        words.Single(w => w.Text == text && Math.Abs(w.Right - 565.9) < 1);

    [Fact]
    public void Pages_are_a4()
    {
        using var doc = UglyToad.PdfPig.PdfDocument.Open(VubStatementRenderer.Render(Scenario(Card(3, -10m))));
        var page = doc.GetPage(1);
        Assert.Equal(595.3, page.Width, 1);
        Assert.Equal(841.9, page.Height, 1);
    }

    [Fact]
    public void Every_page_has_the_header_with_page_numbers()
    {
        var pdf = VubStatementRenderer.Render(Scenario(Card(3, -10m)));
        var words = PdfText.Words(pdf);
        var pages = PdfText.PageCount(pdf);

        for (var p = 1; p <= pages; p++)
        {
            Assert.Contains(words, w => w.Page == p && w.Text == "VÝPIS");
            Assert.Contains(words, w => w.Page == p && w.Text == "Strana:");
            Assert.Contains(words, w => w.Page == p && w.Text == $"{p}/{pages}");
            Assert.Contains(words, w => w.Page == p && w.Text == "9/2026");
        }
    }

    [Fact]
    public void Bank_identity_lines_end_before_the_header_box()
    {
        var words = Words(Scenario(Card(3, -10m)));
        var identity = words.Where(w => w.Page == 1 && w.Top > 58 && w.Top < 72 && w.Size < 7.5);

        Assert.NotEmpty(identity);
        Assert.All(identity, w => Assert.True(w.Right < 335, $"'{w.Text}' ends at {w.Right:F1}"));
    }

    [Fact]
    public void Text_sits_on_a_five_point_grid_at_ten_points()
    {
        var words = Words(Scenario(Card(3, -10m)));
        var label = words.First(w => w.Text == "Zaúčt.");
        var number = words.First(w => w.Text == "účtu" && w.Page == 1 && w.Top > 300);

        Assert.Equal(10.0, label.Size, 1);
        Assert.Equal(45.4, label.X, 0);
        Assert.Equal(151.9, number.X, 0);
        Assert.Equal(30.0, label.Right - label.X, 0); // 6 characters × 5 pt
    }

    [Fact]
    public void Card_payment_has_two_lines_with_amount_right_aligned()
    {
        var words = Words(Scenario(Card(3, -48.15m)));
        var amount = TableAmount(words, "-48,15");
        var main = words.Line(amount);
        var second = words.Where(w => w.Page == 1 && w.Top > amount.Top + 8 && w.Top < amount.Top + 14).OrderBy(w => w.X).ToList();

        Assert.Equal(565.9, amount.Right, 0);
        Assert.True(amount.Bold);
        Assert.Equal(["03/09", "PLATBA", "KARTOU", "KARTA:*4321", "-48,15"], main.Select(w => w.Text));
        Assert.Equal("02/09", second[0].Text);
        Assert.Equal(45.6, second[0].X, 0);
        Assert.Contains(second, w => w.Text == "OBCHOD" && Math.Abs(w.X - 121.4) < 1);
        Assert.Contains(second, w => w.Text == "0309000000001" && Math.Abs(w.X - 334.5) < 1);
    }

    [Fact]
    public void Wallet_card_payment_says_a_pay()
    {
        var words = Words(Scenario(Card(3, -5m) with { Type = TransactionType.WalletCardPayment }));
        var amount = TableAmount(words, "-5,00");

        Assert.Contains(words.Line(amount), w => w.Text == "A-PAY" && Math.Abs(w.X - 191.9) < 1);
        Assert.Contains(words.Line(amount), w => w.Text == "KARTA:*4321" && Math.Abs(w.X - 221.9) < 1);
    }

    [Fact]
    public void Transfer_has_symbols_and_a_details_block()
    {
        var words = Words(Scenario(Transfer(5, 1234.50m)));
        var amount = TableAmount(words, "1.234,50");
        var main = words.Line(amount);

        Assert.Contains(main, w => w.Text == "SK12" && Math.Abs(w.X - 121.9) < 1);
        Assert.Contains(main, w => w.Text == "1234567" && Math.Abs(w.X - 334.5) < 1);
        Assert.Contains(main, w => w.Text == "0308" && Math.Abs(w.X - 391.2) < 1);

        var blockEnd = amount.Top + VubStatementRenderer.BlockHeight(Transfer(5, 1234.50m)) + 2;
        var labels = words
            .Where(w => w.Page == 1 && w.Top > amount.Top && w.Top < blockEnd && Math.Abs(w.X - 45.4) < 1 && w.Bold)
            .OrderBy(w => w.Top)
            .Select(w => w.Text)
            .ToList();
        Assert.Equal(["Názov:", "BIC:", "Referencia", "Účel"], labels);
        Assert.Contains(words, w => w.Text == "Neuvedené" && Math.Abs(w.X - 155.4) < 1);
    }

    [Fact]
    public void Long_details_values_wrap_under_the_value()
    {
        var purpose = "Úhrada faktúry číslo 2026-0417 za opravu strechy, materiál a prácu podľa zmluvy o dielo uzavretej v januári";
        var tx = Transfer(5, -890.00m) with { PaymentPurpose = purpose };
        var words = Words(Scenario(tx));

        var label = words.Single(w => w.Text == "Účel");
        var continuation = words.Where(w => w.Page == 1 && w.Top > label.Top + 5 && w.Top < label.Top + 14).OrderBy(w => w.X).ToList();

        Assert.NotEmpty(continuation);
        Assert.Equal(110.4, continuation[0].X, 0);
        var printed = string.Join(" ", words.Line(label).Skip(2).Concat(continuation).Select(w => w.Text));
        Assert.Equal(purpose, printed);
        Assert.Equal(VubStatementRenderer.BlockHeight(Transfer(5, 1m)) + 9.6, VubStatementRenderer.BlockHeight(tx), 1);
    }

    [Fact]
    public void Descriptions_wider_than_their_column_are_rejected()
    {
        var tx = Card(3, -1m, new string('A', VubStatementRenderer.DescriptionWidth + 1));

        Assert.Throws<ArgumentException>(() => VubStatementRenderer.Render(Scenario(tx)));
    }

    [Fact]
    public void Fee_rows_print_the_fee_in_the_fee_column()
    {
        var fee = new Transaction
        {
            BookingDate = End, ValueDate = End, Type = TransactionType.AccountFee, Amount = -7.00m, FeeAmount = -7.00m,
            Description = "Vedenie konta Účet Štandard", VariableSymbol = "0926", ConstantSymbol = "0558", Reference = "3009AB0000001",
        };
        var words = Words(Scenario(fee, fee with { Type = TransactionType.FeeReversal, Amount = 7.00m, FeeAmount = 7.00m, Description = "Storno-vedenie konta" }));

        var charged = words.Single(w => w.Text == "-7,00" && w.Top < 600 && w.X > 480 && w.X < 500);
        var reversed = words.Single(w => w.Text == "7,00" && w.Top < 600 && w.X > 480 && w.X < 500);
        Assert.Equal(487.0, charged.X, 0);
        Assert.Equal(487.0, reversed.X, 0);
        Assert.DoesNotContain(words.Line(charged), w => w.X > 520);
    }

    [Fact]
    public void Fee_breakdown_shows_the_fee_its_reversal_and_a_zero_net()
    {
        var fee = new Transaction
        {
            BookingDate = End, ValueDate = End, Type = TransactionType.AccountFee, Amount = -7.00m, FeeAmount = -7.00m,
            Description = "Vedenie konta Účet Štandard",
        };
        var words = Words(Scenario(fee, fee with { Type = TransactionType.FeeReversal, Amount = 7.00m, FeeAmount = 7.00m, Description = "Storno-vedenie konta" }));

        string SumOf(string firstWord, string secondWord)
        {
            var label = words.Single(w => w.Text == firstWord && Math.Abs(w.X - 45.4) < 1 && words.Line(w).Any(x => x.Text == secondWord));
            return words.Line(label).Single(w => Math.Abs(w.Right - 565.9) < 1).Text;
        }

        Assert.Equal("7,00", SumOf("Vedenie", "konta"));
        Assert.Equal("-7,00", SumOf("Storno", "vypočítaných"));
        Assert.Equal("0,00", SumOf("Ostatné", "poplatky"));
        Assert.Equal("0,00", SumOf("SUMÁR", "ZAPLATENÝCH"));
    }

    [Fact]
    public void Page_one_has_the_balance_summary()
    {
        var words = Words(Scenario(Card(3, -10m), Transfer(5, 50m)));

        var opening = words.Single(w => w.Text == "1.000,00" && w.Page == 1);
        var closing = words.Single(w => w.Text == "1.040,00" && w.Page == 1);
        Assert.Equal(215.9, opening.Right, 0);
        Assert.True(closing.Bold);
        Assert.Contains(words, w => w.Text == "31.08.2026");
        Assert.Contains(words, w => w.Text == "Vklady");
        Assert.Contains(words, w => w.Text == "Výbery");
    }

    [Fact]
    public void Transactions_are_never_split_across_pages_and_pages_end_early_when_needed()
    {
        var txs = Enumerable.Range(0, 60).Select(i => i % 3 == 0 ? Transfer(1 + i % 28, 10m + i) : Card(2 + i % 27, -1m - i)).ToArray();
        var words = Words(Scenario(txs));

        foreach (var tx in txs)
        {
            var amount = TableAmount(words, VubFormat.Amount(tx.Amount));
            var block = VubStatementRenderer.BlockHeight(tx);
            Assert.True(amount.Top + block <= VubStatementRenderer.TableBottom + 2, $"{tx.Amount} overflows the page");
        }
    }

    [Fact]
    public void Statement_ends_with_fee_breakdown_and_a_notices_page()
    {
        var pdf = VubStatementRenderer.Render(Scenario(Card(3, -10m)));
        var words = PdfText.Words(pdf);
        var last = PdfText.PageCount(pdf);

        Assert.Contains(words, w => w.Text == "ROZPIS" && w.Page < last);
        Assert.Contains(words, w => w.Text == "SUMÁR");
        Assert.Contains(words, w => w.Page == last && w.Text == "Vyhodnotenie");
        Assert.DoesNotContain(words, w => w.Page == last && w.Text == "Zaúčt.");
    }

    [Fact]
    public void Every_page_is_marked_synthetic()
    {
        var pdf = VubStatementRenderer.Render(Scenario(Card(3, -10m)));

        Assert.All(PdfText.MarginTexts(pdf), t => Assert.Contains("SYNTHETIC TEST DATA", t));
        Assert.Contains("SYNTHETIC TEST DATA", PdfText.Info(pdf)["Title"]);
    }

    [Fact]
    public void Rendering_is_byte_identical_across_runs()
    {
        var first = VubStatementRenderer.Render(Scenario(Card(3, -10m), Transfer(5, 50m)));
        var second = VubStatementRenderer.Render(Scenario(Card(3, -10m), Transfer(5, 50m)));

        Assert.Equal(first, second);
    }
}
