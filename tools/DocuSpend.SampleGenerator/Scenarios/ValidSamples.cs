using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Rendering;

namespace DocuSpend.SampleGenerator.Scenarios;

/// <summary>
/// The valid samples from the spec (1 to 5). Every name, account, merchant, reference and amount is invented.
/// Months deliberately differ from the real statement's, so period dates can't coincide with it.
/// </summary>
public static class ValidSamples
{
    public static IReadOnlyList<StatementScenario> All() =>
        [TypicalMonth(), BusyMonth(), AwkwardMonth(), EmptyMonth(), SingleTransactionMonth()];

    private const string CardA = "2178";
    private const string CardB = "8840";

    /// <summary>Sample 1: similar in kind and size to a real month. Exactly 25 transactions.</summary>
    public static StatementScenario TypicalMonth()
    {
        var t = new Tx();
        static DateOnly D(int day) => new(2026, 3, day);
        var employer = Iban.Slovak("1100", "000000", "2945871630");
        var landlord = Iban.Slovak("0900", "000000", "5038416227");

        Transaction[] transactions =
        [
            t.Outgoing(D(2), 640.00m, "Bytové družstvo Lipa", landlord, "GIBASKBX", "Najomne marec", vs: "320417", ks: "0308"),
            t.Card(D(2), 1, 31.47m, "MINIMARKET U LIPY TRENCIN", CardA, wallet: true),
            t.Card(D(3), 2, 12.80m, "PEKAREN KLAS TRENCIN", CardA, wallet: true),
            t.Card(D(4), 1, 54.12m, "CERPACIA STANICA SEVER TRENCIN", CardB),
            t.Incoming(D(5), 1712.36m, "Fiktívna Strojáreň s.r.o.", employer, "TATRSKBX", "MZDA 02/2026",
                vs: "22026", ks: "0138", category: "SALA", currencyLine: true),
            t.Card(D(6), 2, 8.49m, "LEKAREN ZLATA RYBKA TRENCIN", CardA, wallet: true),
            t.Card(D(7), 1, 13.99m, "NETFLIX.COM", CardB),
            t.Card(D(9), 2, 67.25m, "DROGERIA BIELA RUZA TRENCIN", CardA, wallet: true),
            t.Instant(D(10), -25.00m, "Katarína Vzorová", Iban.Slovak("7500", "000000", "4019283746"), "CEKOSKBX"),
            t.Card(D(11), 1, 22.60m, "BISTRO ZELENA LUKA TRENCIN", CardA, wallet: true),
            t.Card(D(12), 1, 41.08m, "MINIMARKET U LIPY TRENCIN", CardA, wallet: true),
            t.Outgoing(D(13), 21.90m, "Telekomunikácie Sever a.s.", Iban.Slovak(FictionalBank.BankCode, "000000", "1873465290"), FictionalBank.Bic,
                "PLATBA FAKTURY", vs: "61403952", ss: "2603117741"),
            t.Card(D(14), 2, 17.35m, "KAVIAREN POD HRADOM TRENCIN", CardB),
            t.Card(D(16), 1, 94.70m, "ELEKTRO SVET ONLINE", CardB),
            t.Incoming(D(17), 150.00m, "Pavol Vzor", Iban.Slovak(FictionalBank.BankCode, "000000", "3390175826"), FictionalBank.Bic,
                "Darcek k meninam", feeType: Tx.OneOffElectronic),
            t.Card(D(18), 1, 38.62m, "MINIMARKET U LIPY TRENCIN", CardA, wallet: true),
            t.Card(D(20), 2, 9.90m, "KNIHY A PAPIER TRENCIN", CardA, wallet: true),
            t.Instant(D(21), 42.50m, "Katarína Vzorová", Iban.Slovak("7500", "000000", "4019283746"), "CEKOSKBX"),
            t.Card(D(23), 1, 75.40m, "OPTIKA JASNY POHLAD TRENCIN", CardB),
            t.Card(D(24), 2, 29.15m, "MASIARSTVO U SUSEDA TRENCIN", CardA, wallet: true),
            t.Outgoing(D(25), 85.00m, "Fiktívna Energetika a.s.", Iban.Slovak("1111", "000000", "6602841937"), "UNCRSKBX",
                "ZALOHA ELEKTRINA", vs: "7710428863", ks: "0308"),
            t.Card(D(27), 1, 44.30m, "ZAHRADNICTVO HORTENZIA TRENCIN", CardA, wallet: true),
            t.Card(D(30), 2, 16.75m, "PEKAREN KLAS TRENCIN", CardA, wallet: true),
            .. t.AccountFeeReversed(D(31), 6.90m),
        ];

        return new StatementScenario
        {
            Name = "01-typical-month",
            AccountHolder = "MÁRIA KOVÁČIKOVÁ",
            HolderAddress = ["Javorová 14", "911 01 Trenčín"],
            Iban = Iban.Slovak(FictionalBank.BankCode, "000000", "4182736455"),
            BranchAddress = "MIEROVÉ NÁMESTIE 2, TRENČÍN",
            PeriodStart = D(1),
            PeriodEnd = D(31),
            OpeningBalance = 1486.20m,
            Transactions = transactions,
        };
    }

    /// <summary>Sample 2: exactly 70 transactions over many pages, generated from a fixed seed.</summary>
    public static StatementScenario BusyMonth()
    {
        var t = new Tx();
        var r = new SeededRandom(2026);
        string[] merchants =
        [
            "MINIMARKET U LIPY BANSKA BYSTRICA", "PEKAREN KLAS BANSKA BYSTRICA", "CERPACIA STANICA SEVER ZVOLEN",
            "LEKAREN ZLATA RYBKA BB", "DROGERIA BIELA RUZA BB", "BISTRO ZELENA LUKA BB", "KAVIAREN POD HRADOM BB",
            "ELEKTRO SVET ONLINE", "KNIHY A PAPIER BB", "OPTIKA JASNY POHLAD BB", "MASIARSTVO U SUSEDA BB",
            "ZAHRADNICTVO HORTENZIA BB", "SPORT A TURISTIKA BB", "NETFLIX.COM",
        ];
        (string Name, string Account, string Bic, string Purpose)[] payees =
        [
            ("Bytové družstvo Sídlisko", Iban.Slovak("0900", "000000", "7128450936"), "GIBASKBX", "Najomne a sluzby za byt"),
            ("Telekomunikácie Sever a.s.", Iban.Slovak(FictionalBank.BankCode, "000000", "1873465290"), FictionalBank.Bic, "PLATBA FAKTURY"),
            ("Fiktívna Energetika a.s.", Iban.Slovak("1111", "000000", "6602841937"), "UNCRSKBX", "ZALOHA PLYN A ELEKTRINA"),
            ("Základná umelecká škola Vzor", Iban.Slovak("8180", "000000", "5291736480"), "SPSRSKBA", "Skolne za druhy polrok skolskeho roka 2025/2026 - hudobny odbor, hra na klavir a zaklady hudobnej nauky"),
            ("Poisťovňa Pokoj a.s.", Iban.Slovak("1100", "000000", "4417285930"), "TATRSKBX", "Poistenie domacnosti a zodpovednosti za skodu, poistna zmluva c. 7740012986, splatka za obdobie maj 2026"),
        ];
        (string Name, string Account, string Bic)[] people =
        [
            ("Tomáš Vzorový", Iban.Slovak(FictionalBank.BankCode, "000000", "2201983746"), FictionalBank.Bic),
            ("Eva Príkladová", Iban.Slovak("7500", "000000", "6650129384"), "CEKOSKBX"),
            ("Martin Testovací", Iban.Slovak("5600", "000000", "8873012465"), "KOMASK2X"),
        ];

        var transactions = new List<Transaction>();
        for (var i = 0; i < 68; i++)
        {
            var date = new DateOnly(2026, 5, 1 + i * 30 / 68);
            var roll = r.Next(100);
            if (i == 3)
            {
                transactions.Add(t.Incoming(date, 1895.40m, "Fiktívna Strojáreň s.r.o.", Iban.Slovak("1100", "000000", "2945871630"),
                    "TATRSKBX", "MZDA 04/2026", vs: "42026", ks: "0138", category: "SALA", currencyLine: true));
            }
            else if (roll < 45)
            {
                transactions.Add(t.Card(date, r.Next(0, 3), r.Amount(2.50m, 120.00m), r.Pick(merchants), r.Next(2) == 0 ? CardA : CardB,
                    wallet: r.Next(3) > 0));
            }
            else if (roll < 75)
            {
                var p = r.Pick(payees);
                transactions.Add(t.Outgoing(date, r.Amount(15.00m, 480.00m), p.Name, p.Account, p.Bic, p.Purpose,
                    vs: r.Digits(8), ks: "0308", ss: r.Next(2) == 0 ? r.Digits(10) : null));
            }
            else if (roll < 88)
            {
                var p = r.Pick(people);
                var incoming = r.Next(2) == 0;
                transactions.Add(t.Instant(date, incoming ? r.Amount(5.00m, 200.00m) : -r.Amount(5.00m, 200.00m), p.Name, p.Account, p.Bic));
            }
            else
            {
                var p = r.Pick(people);
                transactions.Add(t.Incoming(date, r.Amount(10.00m, 300.00m), p.Name, p.Account, p.Bic, "Vratenie za listky",
                    feeType: r.Next(2) == 0 ? Tx.FromOtherBank : Tx.OneOffElectronic));
            }
        }
        transactions.AddRange(t.AccountFeeReversed(new DateOnly(2026, 5, 31), 6.90m));

        return new StatementScenario
        {
            Name = "02-busy-month",
            AccountHolder = "PETER HRONSKÝ",
            HolderAddress = ["Na Lúčkach 22", "974 01 Banská Bystrica"],
            Iban = Iban.Slovak(FictionalBank.BankCode, "000000", "6930184527"),
            BranchAddress = "NÁMESTIE SNP 9, BANSKÁ BYSTRICA",
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 31),
            OpeningBalance = 2318.75m,
            Transactions = transactions,
        };
    }

    /// <summary>Sample 3: the awkward cases that rarely show up in a typical month.</summary>
    public static StatementScenario AwkwardMonth()
    {
        var t = new Tx();
        static DateOnly D(int day) => new(2026, 2, day);

        Transaction[] transactions =
        [
            // First day of the period, value date in the previous month.
            t.Card(D(1), 3, 18.40m, "KAVIAREŇ ČIERNY ŽELUDOK ŽILINA", "5512"),
            // Description exactly as wide as its column.
            t.Card(D(3), 1, 129.99m, "ŠPORTOVÉ POTREBY ĽADOVÝ MEDVEĎ ŽILINA-JUH", "5512", wallet: true),
            // Transfer with every optional field missing.
            new Transaction
            {
                BookingDate = D(4),
                ValueDate = D(4),
                Type = TransactionType.Transfer,
                Amount = -60.00m,
                CounterpartyAccount = Iban.Slovak(FictionalBank.BankCode, "000000", "9046172835"),
            },
            // Booking and value date differ; long purpose that wraps; diacritics in the name.
            t.Outgoing(D(9), 1240.00m, "Ľubomír Šťastný – stavebné práce", Iban.Slovak("1100", "000000", "5183920647"), "TATRSKBX",
                "Úhrada faktúry číslo 2026-0117 za opravu strechy, materiál a prácu podľa zmluvy o dielo uzavretej v januári 2026",
                vs: "20260117", ks: "0308", valueDate: D(6)),
            t.Incoming(D(10), 1508.62m, "Želmíra Ďurčová – účtovníctvo a dane", Iban.Slovak("0900", "000000", "2817406395"), "GIBASKBX",
                "Odmena za január", vs: "12026", ks: "0138", payerReference: "/VS12026/SS/KS0138", currencyLine: true),
            t.Instant(D(14), -35.50m, "Ondrej Čížik", Iban.Slovak("7500", "000000", "3301948276"), "CEKOSKBX"),
            t.Card(D(17), 2, 6.20m, "PEKÁREŇ U ĎATELINY", "5512", wallet: true),
            t.Incoming(D(20), 75.00m, "Ľudmila Šťastná st.", Iban.Slovak(FictionalBank.BankCode, "000000", "4471029386"), FictionalBank.Bic,
                "Príspevok na dovolenku – ďakujem", feeType: Tx.OneOffElectronic),
            t.Card(D(26), 0, 47.83m, "LEKÁREŇ ŠŤASTNÁ RUKA ŽILINA", "5512"),
            // Last day of the period.
            t.Card(D(28), 0, 3.10m, "AUTOMAT NA KÁVU ŽILINA", "5512", wallet: true),
            .. t.AccountFeeReversed(D(28), 6.90m),
        ];

        return new StatementScenario
        {
            Name = "03-awkward-month",
            AccountHolder = "ĽUDMILA ŠŤASTNÁ",
            HolderAddress = ["Ľaliová 5", "010 01 Žilina"],
            Iban = Iban.Slovak(FictionalBank.BankCode, "000000", "8812047365"),
            BranchAddress = "NÁRODNÁ 18, ŽILINA",
            PeriodStart = D(1),
            PeriodEnd = D(28),
            OpeningBalance = 412.06m,
            Transactions = transactions,
        };
    }

    /// <summary>Sample 4: no transactions.</summary>
    public static StatementScenario EmptyMonth() => new()
    {
        Name = "04-empty-month",
        AccountHolder = "JURAJ TICHÝ",
        HolderAddress = ["Krátka 1", "040 01 Košice"],
        Iban = Iban.Slovak(FictionalBank.BankCode, "000000", "1029384756"),
        BranchAddress = "HLAVNÁ 41, KOŠICE",
        PeriodStart = new DateOnly(2026, 7, 1),
        PeriodEnd = new DateOnly(2026, 7, 31),
        OpeningBalance = 245.80m,
        Transactions = [],
    };

    /// <summary>Sample 5: exactly one transaction.</summary>
    public static StatementScenario SingleTransactionMonth()
    {
        var t = new Tx();
        return new StatementScenario
        {
            Name = "05-single-transaction-month",
            AccountHolder = "ANNA OSAMELÁ",
            HolderAddress = ["Dlhá 77", "080 01 Prešov"],
            Iban = Iban.Slovak(FictionalBank.BankCode, "000000", "5647382910"),
            BranchAddress = "MASARYKOVA 2, PREŠOV",
            PeriodStart = new DateOnly(2025, 11, 1),
            PeriodEnd = new DateOnly(2025, 11, 30),
            OpeningBalance = 96.15m,
            Transactions = [t.Card(new DateOnly(2025, 11, 14), 1, 23.45m, "MINIMARKET U LIPY PRESOV", "3306", wallet: true)],
        };
    }
}
