using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Records;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;

namespace DocuSpend.SampleGenerator.Tests;

/// <summary>Checks the valid samples against the acceptance criteria in work/001-synthetic-statements/spec.md.</summary>
public partial class ValidSamplesTests
{
    private static readonly Lazy<Dictionary<string, (StatementScenario Scenario, byte[] Pdf)>> Rendered = new(() =>
        ValidSamples.All().ToDictionary(s => s.Name, s => (s, VubStatementRenderer.Render(s))));

    public static TheoryData<string> Names => new(ValidSamples.All().Select(s => s.Name));

    private static (StatementScenario Scenario, byte[] Pdf) Sample(string name) => Rendered.Value[name];

    [Fact]
    public void The_five_valid_samples_exist_with_their_spec_names() =>
        Assert.Equal(
            ["01-typical-month", "02-busy-month", "03-awkward-month", "04-empty-month", "05-single-transaction-month"],
            ValidSamples.All().Select(s => s.Name));

    [Fact]
    public void Typical_month_has_25_transactions_on_4_pages()
    {
        var (s, pdf) = Sample("01-typical-month");

        Assert.Equal(25, s.Transactions.Count);
        Assert.Equal(4, PdfText.PageCount(pdf));
    }

    [Fact]
    public void Typical_month_mixes_every_kind_seen_including_the_fee_and_its_reversal()
    {
        var txs = Sample("01-typical-month").Scenario.Transactions;

        Assert.Contains(txs, t => t.Type == TransactionType.CardPayment);
        Assert.Contains(txs, t => t.Type == TransactionType.WalletCardPayment);
        Assert.Contains(txs, t => t.Type == TransactionType.Transfer && t.Amount > 0);
        Assert.Contains(txs, t => t.Type == TransactionType.Transfer && t.Amount < 0);
        Assert.Contains(txs, t => t.Type == TransactionType.InstantTransfer && t.Amount > 0);
        Assert.Contains(txs, t => t.Type == TransactionType.InstantTransfer && t.Amount < 0);
        Assert.Single(txs, t => t.Type == TransactionType.AccountFee);
        Assert.Single(txs, t => t.Type == TransactionType.FeeReversal);
    }

    [Fact]
    public void Busy_month_has_70_transactions_on_at_least_8_pages()
    {
        var (s, pdf) = Sample("02-busy-month");

        Assert.Equal(70, s.Transactions.Count);
        Assert.True(PdfText.PageCount(pdf) >= 8, $"{PdfText.PageCount(pdf)} pages");
    }

    [Fact]
    public void Busy_month_has_a_page_that_ends_early_because_the_next_transaction_did_not_fit()
    {
        var (s, pdf) = Sample("02-busy-month");
        var rows = TableRows(s, pdf);

        // Early: the next transaction's first two lines would still fit, but its whole block (with details) would not.
        var earlyEnds = rows.Zip(rows.Skip(1))
            .Where(pair => pair.First.Word.Page != pair.Second.Word.Page)
            .Where(pair =>
            {
                var room = VubStatementRenderer.TableBottom
                    - (pair.First.Word.Top + VubStatementRenderer.BlockHeight(pair.First.Tx))
                    - VubStatementRenderer.RowGap;
                return room >= VubStatementRenderer.BlockHeight(pair.First.Tx with { Type = TransactionType.CardPayment })
                    && room < VubStatementRenderer.BlockHeight(pair.Second.Tx);
            });

        Assert.NotEmpty(earlyEnds);
    }

    [Fact]
    public void Awkward_month_has_slovak_diacritics_in_the_pdf()
    {
        var words = PdfText.Words(Sample("03-awkward-month").Pdf);

        Assert.Contains(words, w => w.Text == "ŽELUDOK");
        Assert.Contains(words, w => w.Text == "Ľubomír");
        Assert.Contains(words, w => w.Text == "Ďurčová");
    }

    [Fact]
    public void Awkward_month_has_a_description_as_wide_as_its_column_and_a_wrapped_value()
    {
        var txs = Sample("03-awkward-month").Scenario.Transactions;

        Assert.Contains(txs, t => t.Description?.Length == VubStatementRenderer.DescriptionWidth);
        Assert.Contains(txs, t => t.PaymentPurpose?.Length > 91);
    }

    [Fact]
    public void A_full_width_description_stays_separate_from_the_reference()
    {
        var (s, pdf) = Sample("03-awkward-month");
        var tx = s.Transactions.First(t => t.Description?.Length == VubStatementRenderer.DescriptionWidth);
        var words = PdfText.Words(pdf);

        Assert.Contains(words, w => w.Text == tx.Description!.Split(' ')[^1]);
        Assert.Contains(words, w => w.Text == tx.Reference);
    }

    [Fact]
    public void Awkward_month_has_a_transfer_with_every_optional_field_missing()
    {
        var txs = Sample("03-awkward-month").Scenario.Transactions;

        Assert.Contains(txs, t => t.IsTransfer && t.CounterpartyName is null && t.CounterpartyBic is null
            && t.Description is null && t.PaymentPurpose is null && t.VariableSymbol is null && t.ConstantSymbol is null
            && t.SpecificSymbol is null && t.PayerReference is null && t.Reference is null);
    }

    [Fact]
    public void Awkward_month_covers_differing_dates_and_both_ends_of_the_period()
    {
        var s = Sample("03-awkward-month").Scenario;

        Assert.Contains(s.Transactions, t => t.IsTransfer && t.ValueDate != t.BookingDate);
        Assert.Contains(s.Transactions, t => t.BookingDate == s.PeriodStart);
        Assert.Contains(s.Transactions, t => t.BookingDate == s.PeriodEnd);
    }

    [Fact]
    public void Empty_month_has_no_transactions_and_an_unchanged_balance()
    {
        var s = Sample("04-empty-month").Scenario;

        Assert.Empty(s.Transactions);
        Assert.Equal(s.Totals.OpeningBalance, s.Totals.ClosingBalance);
    }

    [Fact]
    public void Single_transaction_month_has_exactly_one() =>
        Assert.Single(Sample("05-single-transaction-month").Scenario.Transactions);

    [Theory]
    [MemberData(nameof(Names))]
    public void Record_reconciles(string name)
    {
        var record = RecordWriter.Deserialize(RecordWriter.Serialize(ExpectedRecord.ForValid(Sample(name).Scenario)));
        var st = record.Statement!;
        var txs = record.Transactions!;

        Assert.Equal(st.ClosingBalance, st.OpeningBalance + st.TotalDeposits + st.TotalWithdrawals - st.TotalFees);
        Assert.Equal(st.TotalDeposits, txs.Where(t => t.FeeAmount is null && t.Amount > 0).Sum(t => t.Amount));
        Assert.Equal(st.TotalWithdrawals, txs.Where(t => t.FeeAmount is null && t.Amount < 0).Sum(t => t.Amount));
        Assert.Equal(st.TotalFees, -txs.Where(t => t.FeeAmount is not null).Sum(t => t.Amount));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Pdf_shows_the_records_amounts_in_order(string name)
    {
        var (s, pdf) = Sample(name);
        var printed = TableAmounts(pdf).Select(w => w.Text);
        var expected = s.Transactions.Where(t => !t.IsFee).Select(t => VubFormat.Amount(t.Amount));

        Assert.Equal(expected, printed);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Pdf_shows_the_summary_balances(string name)
    {
        var (s, pdf) = Sample(name);
        var page1 = PdfText.Words(pdf).Where(w => w.Page == 1 && w.Top < 330 && Math.Abs(w.Right - 215.9) < 1.5).Select(w => w.Text).ToList();

        Assert.Contains(VubFormat.Amount(s.Totals.OpeningBalance), page1);
        Assert.Contains(VubFormat.Amount(s.Totals.ClosingBalance), page1);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void No_transaction_is_split_across_pages(string name)
    {
        var (s, pdf) = Sample(name);

        foreach (var (tx, word) in TableRows(s, pdf))
            Assert.True(word.Top + VubStatementRenderer.BlockHeight(tx) <= VubStatementRenderer.TableBottom + 2);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Every_page_is_marked_synthetic(string name)
    {
        var pdf = Sample(name).Pdf;

        Assert.All(PdfText.MarginTexts(pdf), t => Assert.Contains(VubStatementRenderer.SyntheticMarking, t));
        Assert.Equal(VubStatementRenderer.SyntheticMarking, PdfText.Info(pdf)["Title"]);

        var footers = PdfText.Words(pdf).Where(w => w.Top > SyntheticPdf.FooterY - 2 && w.Text == "SYNTHETIC").Select(w => w.Page);
        Assert.Equal(Enumerable.Range(1, PdfText.PageCount(pdf)), footers);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void No_sample_carries_the_real_banks_identity(string name)
    {
        var (s, pdf) = Sample(name);
        var text = string.Join(' ', PdfText.Words(pdf).Select(w => w.Text))
            + RecordWriter.Serialize(ExpectedRecord.ForValid(s));
        string[] identity = ["VÚB", "VUB", "SUBASKBX", "vub.sk", "Mlynské", "31320155", "Klasik"];

        Assert.All(identity, marker => Assert.DoesNotContain(marker, text, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(" 0200 ", text);
        Assert.Contains(FictionalBank.Bic, text);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Only_transaction_kinds_from_the_intent_are_used(string name)
    {
        TransactionType[] allowed =
        [
            TransactionType.CardPayment, TransactionType.WalletCardPayment, TransactionType.Transfer,
            TransactionType.InstantTransfer, TransactionType.AccountFee, TransactionType.FeeReversal,
        ];

        Assert.All(Sample(name).Scenario.Transactions, t => Assert.Contains(t.Type, allowed));
        Assert.All(Sample(name).Scenario.Transactions, t => Assert.Equal("EUR", t.Currency));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Account_numbers_are_structurally_valid(string name)
    {
        var s = Sample(name).Scenario;

        Assert.True(Iban.IsValid(s.Iban), s.Iban);
        Assert.All(s.Transactions.Where(t => t.CounterpartyAccount is not null), t => Assert.True(Iban.IsValid(t.CounterpartyAccount!), t.CounterpartyAccount));
    }

    /// <summary>Each non-fee transaction with the PDF word of its amount, in table order.</summary>
    private static List<(Transaction Tx, PdfWord Word)> TableRows(StatementScenario s, byte[] pdf)
    {
        var amounts = TableAmounts(pdf);
        Assert.Equal(s.Transactions.Count(t => !t.IsFee), amounts.Count);
        return s.Transactions.Where(t => !t.IsFee).Zip(amounts).ToList();
    }

    /// <summary>Amounts in the table's "Suma" column, in reading order, excluding the fee breakdown below the table.</summary>
    private static List<PdfWord> TableAmounts(byte[] pdf)
    {
        var words = PdfText.Words(pdf);
        var breakdown = words.Single(w => w.Text == "ROZPIS");
        return words
            .Where(w => w.Bold && Math.Abs(w.Right - 565.9) < 1 && AmountFormat().IsMatch(w.Text))
            .Where(w => w.Page < breakdown.Page || (w.Page == breakdown.Page && w.Top < breakdown.Top))
            .OrderBy(w => w.Page).ThenBy(w => w.Top)
            .ToList();
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^-?\d{1,3}(\.\d{3})*,\d{2}$")]
    private static partial System.Text.RegularExpressions.Regex AmountFormat();
}
