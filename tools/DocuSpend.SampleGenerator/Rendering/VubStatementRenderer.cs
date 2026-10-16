using System.Globalization;
using DocuSpend.SampleGenerator.Model;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>
/// Renders a scenario in the layout of a VÚB monthly statement, carrying the identity of <see cref="FictionalBank"/>.
/// Positions and fonts follow
/// work/001-synthetic-statements/layout.md; all coordinates are points from the top-left corner.
/// </summary>
public static class VubStatementRenderer
{
    public const string SyntheticMarking = "SYNTHETIC TEST DATA – NOT A REAL STATEMENT";

    public const double PageWidth = 595.275;
    public const double PageHeight = 841.889;

    /// <summary>The lowest top a transaction's last line may have.</summary>
    public const double TableBottom = 792.0;

    private const double Page1HeaderY = 350.6;
    private const double Page1FirstRowY = 376.4;
    private const double NextHeaderY = 187.3;
    private const double NextFirstRowY = 213.1;

    private const double SecondLine = 11.3;
    private const double DetailsStart = 12.9;
    private const double DetailStep = 9.6;
    public const double RowGap = 15.6;

    // Columns.
    private const double DateX = 45.4;
    private const double ValueDateX = 80.8;
    private const double AccountX = 121.9;
    private const double DescriptionX = 121.4;
    private const double CardX = 221.9;
    private const double VsX = 334.5;
    private const double KsX = 391.2;
    private const double SsX = 419.5;
    private const double FeeTypeX = 479.1;
    private const double FeeX = 487.0;
    private const double AmountRight = 565.9;

    private const double FeeBreakdownHeight = 145.0;

    private static readonly (string Pictogram, string Label)[] FeeTypes =
    [
        ("X", "Prijaté prevody z iných bánk"),
        ("E", "Prijaté jednorazové elektronické prevody"),
        ("1", "Prijaté SEPA Okamžité platby"),
        ("2", "Prijaté elektronické prevody cez TP/AP"),
    ];

    public static byte[] Render(StatementScenario scenario)
    {
        Validate(scenario);
        var pages = Paginate(scenario.Transactions);
        var total = pages.Count + 1; // plus the final notices page

        var doc = SyntheticPdf.Create(scenario.IssueDate);
        for (var i = 0; i < total; i++)
        {
            var (gfx, c) = SyntheticPdf.AddPage(doc);
            using var _ = gfx;

            DrawHeader(c, scenario, i + 1, total);
            if (i == pages.Count)
            {
                DrawNotices(c, scenario);
                continue;
            }

            var content = pages[i];
            if (i == 0)
            {
                DrawAddress(c, scenario);
                DrawSummary(c, scenario);
            }
            if (content.HasTable)
                DrawTableHeader(c, i == 0 ? Page1HeaderY : NextHeaderY);
            foreach (var (tx, y) in content.Rows)
                DrawTransaction(c, tx, y);
            if (content.FeeBreakdownY is { } feeY)
                DrawFeeBreakdown(c, scenario, feeY);
        }

        return SyntheticPdf.Save(doc, scenario.Name);
    }

    /// <summary>Characters allowed in the second-line description: up to the reference column, keeping at least one character of space before it.</summary>
    public const int DescriptionWidth = 41;

    /// <summary>Distance from a transaction's first line to its last line.</summary>
    public static double BlockHeight(Transaction tx)
    {
        var details = Details(tx).Count;
        return SecondLine + (details > 0 ? DetailsStart + DetailStep * (details - 1) : 0);
    }

    private static void Validate(StatementScenario scenario)
    {
        foreach (var tx in scenario.Transactions)
        {
            if (tx.Description is { Length: > DescriptionWidth } d)
                throw new ArgumentException($"Description is wider than its column ({d.Length} > {DescriptionWidth}): {d}");
        }
    }

    private sealed record PageContent(bool HasTable)
    {
        public List<(Transaction Tx, double Y)> Rows { get; } = [];
        public double? FeeBreakdownY { get; set; }
    }

    /// <summary>Places transactions on pages, never splitting one; a page ends early when the next one doesn't fit.</summary>
    private static List<PageContent> Paginate(IReadOnlyList<Transaction> transactions)
    {
        var pages = new List<PageContent> { new(HasTable: true) };
        var y = Page1FirstRowY;
        foreach (var tx in transactions)
        {
            if (y + BlockHeight(tx) > TableBottom)
            {
                pages.Add(new PageContent(HasTable: true));
                y = NextFirstRowY;
            }
            pages[^1].Rows.Add((tx, y));
            y += BlockHeight(tx) + RowGap;
        }

        if (y + FeeBreakdownHeight > TableBottom)
        {
            pages.Add(new PageContent(HasTable: false));
            y = NextHeaderY;
        }
        pages[^1].FeeBreakdownY = y;
        return pages;
    }

    private static void DrawHeader(Canvas c, StatementScenario s, int page, int total)
    {
        c.Mono(340.2, 27.5, "Por. číslo:", 8);
        c.Mono(340.2, 37.2, "Strana:", 8);
        c.Mono(340.2, 47.3, "Zo dňa:", 8);
        c.Mono(340.2, 57.0, "Pobočka:", 8);
        c.Mono(340.2, 66.9, "Distribúcia:", 8);
        c.MonoRight(565.5, 25.0, s.StatementNumber, 8);
        c.MonoRight(565.5, 35.0, $"{page}/{total}", 8);
        c.MonoRight(565.5, 45.5, VubFormat.FullDate(s.IssueDate), 8);
        c.MonoRight(565.5, 55.6, s.BranchCode, 10, bold: true);
        c.MonoRight(565.5, 65.5, s.Distribution, 8);

        c.Sans(45.4, 60.8, FictionalBank.LegalLines[0], 7);
        c.Sans(45.4, 67.8, FictionalBank.LegalLines[1], 7);

        c.Mono(45.7, 82.9, "VÝPIS Z ÚČTU", 12);

        string[] labels = ["Typ:", "Názov:", "Číslo:", "BIC:", "Mena:", "Pobočka:"];
        string[] values = [s.ProductName, s.AccountHolder, s.Iban, s.Bic, s.Currency, s.BranchAddress];
        for (var i = 0; i < labels.Length; i++)
        {
            var y = 99.3 + 14.0 * i;
            c.Mono(45.4, y, labels[i], 12);
            c.Mono(113.4, y + 1.0, values[i], 10, bold: true);
        }
    }

    private static void DrawAddress(Canvas c, StatementScenario s)
    {
        c.Mono(340.2, 179.2, s.AccountHolder, 11, bold: true);
        for (var i = 0; i < s.HolderAddress.Count; i++)
        {
            var last = i == s.HolderAddress.Count - 1;
            c.Mono(340.2, 188.0 + 12.9 * i, s.HolderAddress[i], last ? 12 : 11, bold: true);
        }
    }

    private static void DrawSummary(Canvas c, StatementScenario s)
    {
        var t = s.Totals;
        const double valueRight = 215.9;

        c.Mono(45.4, 188.2, "Vlastné použiteľné prostriedky", 8, bold: true);
        c.Mono(195.4, 188.2, "v EUR", 8, bold: true);
        c.Mono(45.4, 195.6, $"k {VubFormat.FullDate(s.PeriodStart.AddDays(-1))}", 8, bold: true);
        c.MonoRight(valueRight, 196.7, VubFormat.Amount(t.OpeningBalance), 8);

        (string Label, decimal Value)[] rows =
        [
            ("Vklady spolu", t.TotalDeposits),
            ("Výbery spolu", t.TotalWithdrawals),
            ("Kreditné úroky", 0m),
            ("Daň", 0m),
            ("Debetné úroky", 0m),
            ("Poplatky spolu", t.TotalFees),
        ];
        for (var i = 0; i < rows.Length; i++)
        {
            var y = 209.5 + 17.9 * i;
            c.Mono(45.4, y, rows[i].Label, 8, bold: true);
            c.MonoRight(valueRight, y + 4.9, VubFormat.Amount(rows[i].Value), 8);
        }

        c.Mono(45.4, 317.2, $"Vlast. použit. prostr. k {VubFormat.FullDate(s.PeriodEnd)}", 8, bold: true);
        c.Mono(195.4, 317.2, "v EUR", 8, bold: true);
        c.MonoRight(valueRight - 0.5, 323.8, VubFormat.Amount(t.ClosingBalance), 8, bold: true);
    }

    private static void DrawTableHeader(Canvas c, double y)
    {
        c.Rule(45.4, y - 3.0, AmountRight);
        c.Mono(DateX, y, "Zaúčt.", bold: true);
        c.Mono(ValueDateX, y, "Valuta");
        c.Mono(AccountX, y, "Číslo účtu", bold: true);
        c.Mono(VsX, y, "VS");
        c.Mono(KsX, y, "KS");
        c.Mono(SsX, y, "ŠS");
        c.Mono(546.9, y, "Suma", bold: true);

        var y2 = y + 9.2;
        c.Mono(DateX, y2, "Realiz.", bold: true);
        c.Mono(AccountX, y2, "Popis transakcie");
        c.Mono(VsX, y2, "Referencia");
        c.Mono(447.9, y2, "Typ poplatku");
        c.Mono(526.9, y2, "Poplatok");
        c.Rule(45.4, y2 + 12.0, AmountRight);
    }

    private static void DrawTransaction(Canvas c, Transaction tx, double y)
    {
        var y2 = y + SecondLine;
        c.Mono(DateX, y, VubFormat.TableDate(tx.BookingDate), bold: true);

        if (tx.IsCard)
        {
            c.Mono(AccountX, y, tx.Type == TransactionType.WalletCardPayment ? "PLATBA KARTOU A-PAY" : "PLATBA KARTOU", bold: true);
            c.Mono(CardX, y, $"KARTA:*{tx.Card}", bold: true);
            c.MonoRight(AmountRight, y, VubFormat.Amount(tx.Amount), bold: true);
            c.Mono(DateX + 0.2, y2, VubFormat.TableDate(tx.ValueDate));
        }
        else
        {
            if (tx.ValueDate != tx.BookingDate)
                c.Mono(ValueDateX, y, VubFormat.TableDate(tx.ValueDate));
            if (tx.CounterpartyAccount is not null)
                c.Mono(AccountX, y, tx.CounterpartyAccount, bold: true);
            c.Mono(VsX, y, tx.VariableSymbol ?? "");
            c.Mono(KsX, y, tx.ConstantSymbol ?? "");
            c.Mono(SsX, y, tx.SpecificSymbol ?? "");
            if (!tx.IsFee)
                c.MonoRight(AmountRight, y, VubFormat.Amount(tx.Amount), bold: true);
        }

        c.Mono(DescriptionX, y2, tx.Description ?? "");
        c.Mono(VsX, y2, tx.Reference ?? "");
        if (tx.FeeType is not null)
            c.Sans(FeeTypeX, y2, tx.FeeType, 12);
        if (tx.IsFee && tx.FeeAmount is { } fee)
            c.Mono(FeeX, y2, VubFormat.Amount(fee));

        var details = Details(tx);
        for (var i = 0; i < details.Count; i++)
        {
            var line = details[i];
            var lineY = y2 + DetailsStart + DetailStep * i;
            if (line.Label is not null)
                c.Labeled(DateX, lineY, line.Label, line.Text);
            else
                c.Mono(line.X, lineY, line.Text);
        }
    }

    /// <summary>One printed line of a details block: a labelled line, or a continuation of a wrapped value.</summary>
    private sealed record DetailLine(string? Label, string Text, double X);

    private static List<DetailLine> Details(Transaction tx)
    {
        var lines = new List<DetailLine>();
        if (!tx.IsTransfer)
            return lines;

        void Add(string label, string value)
        {
            // Assumption (not seen on the real statement): long values wrap under the value, breaking at spaces.
            var x = DateX + (label.Length + 1) * Canvas.Advance(10);
            var width = (int)((AmountRight - x) / Canvas.Advance(10));
            var wrapped = Wrap(value, width);
            lines.Add(new DetailLine(label, wrapped[0], x));
            lines.AddRange(wrapped.Skip(1).Select(t => new DetailLine(null, t, x)));
        }

        if (tx.CounterpartyName is not null) Add("Názov:", tx.CounterpartyName);
        if (tx.CounterpartyBic is not null) Add("BIC:", tx.CounterpartyBic);
        Add("Referencia platiteľa:", tx.PayerReference ?? "Neuvedené");
        if (tx.PaymentPurpose is not null) Add("Účel platby:", tx.PaymentPurpose);
        if (tx.PurposeCategoryCode is not null) Add("Kód kategórie účelu platby:", tx.PurposeCategoryCode);
        if (tx.ShowsCurrencyLine) Add("Mena/suma/kurz:", $"{tx.Currency}/{VubFormat.Amount(Math.Abs(tx.Amount))}/1,0000000");
        return lines;
    }

    private static List<string> Wrap(string text, int width)
    {
        var lines = new List<string>();
        var rest = text;
        while (rest.Length > width)
        {
            var cut = rest.LastIndexOf(' ', width);
            if (cut <= 0)
                cut = width;
            lines.Add(rest[..cut].TrimEnd());
            rest = rest[cut..].TrimStart();
        }
        lines.Add(rest);
        return lines;
    }

    private static void DrawFeeBreakdown(Canvas c, StatementScenario s, double y)
    {
        var txs = s.Transactions;
        var cards = txs.Count(t => t.IsCard);
        var outgoing = txs.Count(t => t.IsTransfer && t.Amount < 0);
        // Fees charged are shown positive, reversals negative, as on the real statement.
        var accountFee = -txs.Where(t => t.Type == TransactionType.AccountFee).Sum(t => t.Amount);
        var reversal = -txs.Where(t => t.Type == TransactionType.FeeReversal).Sum(t => t.Amount);

        c.Mono(45.4, y, "ROZPIS POPLATKOV", bold: true);
        c.Mono(326.4, y, "POČET", bold: true);
        c.Mono(411.4, y, "CENA", bold: true);
        c.Mono(546.9, y, "SUMA", bold: true);
        c.Mono(45.4, y + 8.6, $"(OD {VubFormat.FullDate(s.PeriodStart)} DO {VubFormat.FullDate(s.PeriodEnd)})", bold: true);
        c.Mono(541.9, y + 8.6, "V EUR", bold: true);

        var row = y + 24.8;
        void Row(string label, int? count, string? price, decimal? sum, bool bold = false, string? pictogram = null)
        {
            if (pictogram is not null)
            {
                c.Sans(45.4, row, pictogram, 12);
                c.Mono(74.4, row, label);
            }
            else
            {
                c.Mono(45.4, row, label, bold: bold);
            }
            if (count is { } n) c.MonoRight(346.4, row, n.ToString(CultureInfo.InvariantCulture));
            if (price is not null) c.Mono(409.4, row, price);
            if (sum is { } v) c.MonoRight(AmountRight, row, VubFormat.Amount(v), bold: true);
            row += 11.5;
        }

        Row("Platba platobnou kartou u obchodníka", cards, null, 0m);
        Row("Online prevody/SEPA Okamžité platby elektronické", outgoing, null, 0m);
        Row("Poplatky za platby v rámci konta spolu", cards + outgoing, null, 0m, bold: true);
        foreach (var (pictogram, label) in FeeTypes)
            Row(label, txs.Count(t => t.FeeType == pictogram), "0,000", null, pictogram: pictogram);
        Row("Vedenie konta", null, null, accountFee);
        Row("Storno vypočítaných poplatkov", null, null, reversal);
        Row("Ostatné poplatky spolu", null, null, accountFee + reversal, bold: true);
        Row("SUMÁR ZAPLATENÝCH POPLATKOV", null, null, s.Totals.TotalFees, bold: true);
    }

    private static void DrawNotices(Canvas c, StatementScenario s)
    {
        var stars = new string('*', 99);
        var y = 184.8;
        c.Mono(45.4, y, "Vyhodnotenie Odmeny za vernosť:", bold: true);
        c.Mono(45.4, y += 10.1, stars, bold: true);
        c.Mono(45.4, y += 9.8, "Obdobie", bold: true);
        c.Mono(330.4, y, s.PeriodStart.AddMonths(-1).ToString("MM'/'yy", CultureInfo.InvariantCulture), bold: true);
        c.Mono(45.4, y += 9.9, "Podmienky aktívneho využívania účtu", bold: true);

        (string Condition, bool Met)[] conditions =
        [
            ("Pravidelný mesačný príjem na účet", true),
            ("Využívanie služby Internet banking", true),
            ("Aspoň 3 platby kartou za mesiac", true),
            ("Sporiaci alebo investičný produkt v banke", false),
            ("Poistenie k účtu", false),
        ];
        foreach (var (condition, met) in conditions)
        {
            c.Mono(45.4, y += 9.8, condition);
            c.Mono(330.4, y, met ? "Áno" : "Nie");
        }
        c.Mono(45.4, y += 9.8, "Nárok na Odmenu za vernosť", bold: true);
        c.Mono(330.4, y, "Áno", bold: true);
        c.Mono(45.4, y += 10.1, stars, bold: true);

        string[] notice =
        [
            "Na vklady na tomto účte sa vzťahuje ochrana vkladov podľa zákona o ochrane vkladov.",
            "Podrobnejšie informácie o systéme ochrany vkladov nájdete v informačnom formulári,",
            "ktorý je k dispozícii na obchodných miestach banky a na jej internetovej stránke.",
        ];
        y += 19.0;
        foreach (var line in notice)
            c.Mono(45.4, y += 10.1, line);

        c.Mono(106.4, 800.5, "S otázkami a prípadnými nezrovnalosťami sa obráťte na našu telefonickú");
        c.Mono(206.4, 810.5, "službu, ktorá je k dispozícii 24 hodín denne.");
    }
}
