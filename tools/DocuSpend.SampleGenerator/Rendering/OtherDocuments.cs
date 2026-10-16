using System.Globalization;

namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>PDFs that are not VÚB statements, for testing rejection. The bank and every value are invented.</summary>
public static class OtherDocuments
{
    /// <summary>A statement from an invented bank, with a layout and number format unlike VÚB's.</summary>
    public static byte[] OtherBankStatement()
    {
        var doc = SyntheticPdf.Create(new DateOnly(2026, 4, 30));
        var (gfx, c) = SyntheticPdf.AddPage(doc);
        using (gfx)
        {
            c.Sans(60, 50, "Fiktívna Banka, a.s.", 16);
            c.Sans(60, 70, "Námestie Príkladov 1, 811 01 Bratislava  |  www.fiktivnabanka.example", 8);
            c.Sans(60, 110, "Výpis z bežného účtu", 14);
            c.Sans(60, 135, "Majiteľ účtu: Róbert Ukážkový", 10);
            c.Sans(60, 150, "IBAN: SK72 9999 0000 0012 3456 7801", 10);
            c.Sans(60, 165, "Obdobie: 01.04.2026 – 30.04.2026", 10);

            c.Sans(60, 200, "Dátum", 9);
            c.Sans(130, 200, "Popis", 9);
            c.Sans(330, 200, "Protistrana", 9);
            c.Sans(480, 200, "Suma (EUR)", 9);
            c.Rule(60, 213, 540);

            (string Date, string Text, string Party, decimal Amount)[] rows =
            [
                ("02.04.2026", "Nákup kartou", "Obchod Na Rohu", -18.20m),
                ("05.04.2026", "Prijatá platba", "Príkladová Firma s.r.o.", 1320.00m),
                ("09.04.2026", "Trvalý príkaz", "Spoločenstvo vlastníkov", -95.00m),
                ("15.04.2026", "Nákup kartou", "Lekáreň Pri Parku", -7.45m),
                ("21.04.2026", "Odoslaná platba", "Ukážková Energia a.s.", -60.00m),
                ("28.04.2026", "Nákup kartou", "Kníhkupectvo Strana", -23.90m),
            ];
            var y = 222.0;
            foreach (var row in rows)
            {
                c.Sans(60, y, row.Date, 9);
                c.Sans(130, y, row.Text, 9);
                c.Sans(330, y, row.Party, 9);
                c.Sans(480, y, Spaced(row.Amount), 9);
                y += 16;
            }

            c.Rule(60, y + 4, 540);
            c.Sans(60, y + 14, "Počiatočný zostatok: 512,30", 10);
            c.Sans(60, y + 30, "Konečný zostatok: 1 627,75", 10);
            c.Sans(330, y + 30, "Zostatok k 30.04.2026", 10);
        }
        return SyntheticPdf.Save(doc, "06-other-bank-statement");
    }

    /// <summary>A valid PDF that is not a bank statement at all.</summary>
    public static byte[] Invoice()
    {
        var doc = SyntheticPdf.Create(new DateOnly(2026, 6, 12));
        var (gfx, c) = SyntheticPdf.AddPage(doc);
        using (gfx)
        {
            c.Sans(60, 50, "FAKTÚRA", 20);
            c.Sans(60, 78, "číslo 2026-0042", 11);
            c.Sans(60, 115, "Dodávateľ: Fiktívna Dielňa s.r.o., Remeselná 8, 900 01 Pezinok", 10);
            c.Sans(60, 130, "Odberateľ: Ivana Ukážková, Tichá 3, 902 01 Modra", 10);
            c.Sans(60, 145, "Dátum vystavenia: 12.06.2026     Dátum splatnosti: 26.06.2026", 10);

            c.Sans(60, 185, "Položka", 9);
            c.Sans(350, 185, "Množstvo", 9);
            c.Sans(470, 185, "Cena", 9);
            c.Rule(60, 198, 540);
            (string Item, string Quantity, string Price)[] items =
            [
                ("Oprava kuchynskej linky", "1", "180,00"),
                ("Materiál", "1", "54,00"),
                ("Doprava", "1", "15,00"),
            ];
            var y = 207.0;
            foreach (var (item, quantity, price) in items)
            {
                c.Sans(60, y, item, 9);
                c.Sans(350, y, quantity, 9);
                c.Sans(470, y, price, 9);
                y += 16;
            }
            c.Rule(60, y + 4, 540);
            c.Sans(330, y + 14, "Spolu k úhrade: 249,00 EUR", 11);
            c.Sans(60, y + 50, "Ďakujeme za objednávku.", 10);
        }
        return SyntheticPdf.Save(doc, "07-not-a-statement");
    }

    /// <summary>"1 234,56": space as thousands separator, unlike VÚB's dot.</summary>
    private static string Spaced(decimal value) =>
        value.ToString("#,##0.00", CultureInfo.InvariantCulture).Replace(',', ' ').Replace('.', ',');
}
