using System.Text.RegularExpressions;
using DocuSpend.Statements.Model;
using DocuSpend.Statements.Reading.Layout;

namespace DocuSpend.Statements.Reading.Sections;

/// <summary>
/// The transaction table on every page. Each transaction is a block of lines:
/// <list type="number">
/// <item>its main line: booking date, then a card or an account number, symbols and the amount;</item>
/// <item>its second line, directly below: a card payment's value date, the description, reference, fee type and fee;</item>
/// <item>for transfers, a details block of labelled lines, where a long value continues on lines of its own.</item>
/// </list>
/// See work/001-synthetic-statements/layout.md.
/// </summary>
internal static partial class TransactionSection
{
    /// <summary>The details labels, longest first so "Referencia platiteľa:" isn't taken for a shorter one.</summary>
    private static readonly string[] DetailLabels =
        ["Kód kategórie účelu platby:", "Referencia platiteľa:", "Mena/suma/kurz:", "Účel platby:", "Názov:", "BIC:"];

    private const string NotStated = "Neuvedené";
    private const string InstantPayment = "SEPA Okamžitá platba";

    /// <summary>Below this the page holds only its footer.</summary>
    private const double TableEnd = 800.0;

    public static IReadOnlyList<Transaction> Read(IReadOnlyList<IReadOnlyList<Line>> pages, DateOnly periodStart, DateOnly periodEnd, string currency)
    {
        var transactions = new List<Transaction>();
        foreach (var page in pages)
        {
            var blocks = Blocks(TableLines(page));
            for (var i = 0; i < blocks.Count; i++)
                transactions.Add(blocks[i].ToTransaction(new Where(page[0].Page, i + 1), periodStart, periodEnd, currency));
        }
        return transactions;
    }

    /// <summary>The lines between the table header and the end of the table, or none if the page has no table.</summary>
    private static IEnumerable<Line> TableLines(IReadOnlyList<Line> page)
    {
        var header = page.ToList().FindIndex(l => l.Contains("Zaúčt.", "Valuta"));
        if (header < 0)
            return [];
        var start = page.ToList().FindIndex(header, l => l.StartingAt(VubLayout.DateX)?.Text == "Realiz.");
        if (start < 0)
            throw new UnreadableException($"The transaction table on page {page[0].Page} could not be read: its header is incomplete.");

        return page.Skip(start + 1)
            .TakeWhile(l => !l.Contains("ROZPIS", "POPLATKOV") && l.Top < TableEnd);
    }

    private static List<Block> Blocks(IEnumerable<Line> lines)
    {
        var blocks = new List<Block>();
        foreach (var line in lines)
        {
            var current = blocks.Count > 0 ? blocks[^1] : null;
            if (current is { Second: null, Details.Count: 0 } && line.Top - current.Main.Top <= VubLayout.SecondLineMaxGap)
                current.Second = line;
            else if (line.StartingAt(VubLayout.DateX) is { } first && VubText.IsTableDate(first.Text))
                blocks.Add(new Block(line));
            else if (current is not null && DetailLabel(line) is { } label)
                current.Details.Add(new Detail(label, ValueAfter(line, label)));
            else if (current is { Details.Count: > 0 } && line.X > VubLayout.DateX + VubLayout.ColumnTolerance)
                current.Details[^1].Continue(line.Text);
            else
                // Most likely a transaction whose booking date is damaged, so it couldn't be told apart from the one before.
                throw new UnreadableException(
                    $"Transaction {blocks.Count + 1} on page {line.Page} could not be read: a line in the table doesn't belong to any transaction, so the booking date may be missing or not a date.");
        }
        return blocks;
    }

    private static string? DetailLabel(Line line)
    {
        if (line.StartingAt(VubLayout.DateX) is null)
            return null;
        return DetailLabels.FirstOrDefault(label => line.Text.StartsWith(label + " ", StringComparison.Ordinal) || line.Text == label);
    }

    private static string ValueAfter(Line line, string label) => line.Text[label.Length..].Trim();

    private sealed record Where(int Page, int Number)
    {
        public UnreadableException Fail(string what) =>
            new($"Transaction {Number} on page {Page} could not be read: {what}.");
    }

    private sealed class Detail(string label, string value)
    {
        public string Label { get; } = label;
        public string Value { get; private set; } = value;

        /// <summary>A wrapped value continues on the next line; the parts are joined with a single space.</summary>
        public void Continue(string text) => Value = Value.Length == 0 ? text : $"{Value} {text}";
    }

    private sealed class Block(Line main)
    {
        public Line Main { get; } = main;
        public Line? Second { get; set; }
        public List<Detail> Details { get; } = [];

        public Transaction ToTransaction(Where where, DateOnly periodStart, DateOnly periodEnd, string currency)
        {
            DateOnly Date(Word? word, string what) =>
                VubText.TableDate(word?.Text, periodStart, periodEnd) ?? throw where.Fail($"{what} is missing or not a date");

            var booking = Date(Main.StartingAt(VubLayout.DateX), "the booking date");
            var typeText = Main.TextBetween(VubLayout.AccountX, VubLayout.CardX);
            var cardWord = Main.StartingAt(VubLayout.CardX);
            var account = Main.TextBetween(VubLayout.AccountX, VubLayout.VsX);
            var amountWord = Main.EndingAt(VubLayout.AmountRight);
            var description = Second?.TextBetween(VubLayout.DescriptionX, VubLayout.VsX);
            var feeWord = Second?.StartingAt(VubLayout.FeeX);
            var feeTypeWord = Second?.StartingAt(VubLayout.FeeTypeX);

            TransactionType type;
            string? card = null;
            if (typeText is "PLATBA KARTOU" or "PLATBA KARTOU A-PAY")
            {
                type = typeText == "PLATBA KARTOU" ? TransactionType.CardPayment : TransactionType.WalletCardPayment;
                card = CardDigits(cardWord?.Text) ?? throw where.Fail("the card number is missing");
                account = null;
            }
            else if (account is not null && AccountNumber().IsMatch(account))
            {
                type = description == InstantPayment ? TransactionType.InstantTransfer : TransactionType.Transfer;
            }
            else if (account is null && amountWord is null && feeWord is not null)
            {
                var fee = VubText.Amount(feeWord.Text) ?? throw where.Fail("the fee is not an amount");
                type = fee < 0 ? TransactionType.AccountFee : TransactionType.FeeReversal;
            }
            else
            {
                throw where.Fail("its kind isn't one this reader knows");
            }

            var isFee = type is TransactionType.AccountFee or TransactionType.FeeReversal;
            var isCard = type is TransactionType.CardPayment or TransactionType.WalletCardPayment;
            decimal? feeAmount = isFee ? VubText.Amount(feeWord!.Text) : null;
            var amount = isFee ? feeAmount!.Value
                : VubText.Amount(amountWord?.Text) ?? throw where.Fail("the amount is missing or not an amount");

            var value = isCard
                ? Date(Second?.StartingAt(VubLayout.DateX), "the value date")
                : Main.StartingAt(VubLayout.ValueDateX) is { } printed ? Date(printed, "the value date") : booking;

            string? Detail(string label) => Details.FirstOrDefault(d => d.Label == label)?.Value is { Length: > 0 } v ? v : null;
            var payerReference = Detail("Referencia platiteľa:");

            return new Transaction
            {
                BookingDate = booking,
                ValueDate = value,
                Type = type,
                Amount = amount,
                Currency = currency,
                Card = card,
                Description = description,
                CounterpartyName = Detail("Názov:"),
                CounterpartyAccount = isCard || isFee ? null : account,
                CounterpartyBic = Detail("BIC:"),
                VariableSymbol = isCard ? null : Main.StartingAt(VubLayout.VsX)?.Text,
                ConstantSymbol = isCard ? null : Main.StartingAt(VubLayout.KsX)?.Text,
                SpecificSymbol = isCard ? null : Main.StartingAt(VubLayout.SsX)?.Text,
                PayerReference = payerReference == NotStated ? null : payerReference,
                PaymentPurpose = Detail("Účel platby:"),
                PurposeCategoryCode = Detail("Kód kategórie účelu platby:"),
                Reference = Second?.StartingAt(VubLayout.VsX)?.Text,
                FeeType = feeTypeWord is { Text.Length: 1 } ? feeTypeWord.Text : null,
                FeeAmount = feeAmount,
            };
        }

        private static string? CardDigits(string? text) =>
            text is not null && text.StartsWith("KARTA:*", StringComparison.Ordinal) && text.Length == 11 ? text[7..] : null;
    }

    /// <summary>
    /// An IBAN as printed: country, check digits, then groups of four letters or digits, the last one possibly shorter
    /// (a German IBAN ends with a group of two).
    /// </summary>
    [GeneratedRegex(@"^[A-Z]{2}\d{2}( [A-Z0-9]{4})* [A-Z0-9]{1,4}$")]
    private static partial Regex AccountNumber();
}
