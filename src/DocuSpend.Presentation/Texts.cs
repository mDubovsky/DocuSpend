using DocuSpend.Categorization.Model;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation;

/// <summary>
/// Every text the interface shows, in Slovak (decision 0003), kept in one place rather than in the screens. Category
/// names come from the categorizer, which already holds them in Slovak.
/// </summary>
public static class Texts
{
    public const string AppTitle = "DocuSpend";

    // The statement view
    public const string Sample = "Ukážkový výpis";
    public const string Period = "Obdobie";
    public const string OpeningBalance = "Počiatočný zostatok";
    public const string ClosingBalance = "Konečný zostatok";
    public const string SpendingTotal = "Výdavky spolu";
    public const string SpendingChart = "Výdavky podľa kategórií";
    public const string BalanceChart = "Zostatok na účte";
    public const string Categories = "Kategórie";
    public const string Transactions = "Transakcie";
    public const string NoSpending = "V tomto mesiaci neboli žiadne výdavky.";
    public const string NoCategorySelected = "Vyberte kategóriu v grafe alebo v zozname.";
    public const string NoDescription = "(bez popisu)";
    public const string Balance = "Zostatok";
    public const string StatementUnreadable = "Výpis sa nepodarilo prečítať.";

    // Column headers
    public const string Date = "Dátum";
    public const string Description = "Popis";
    public const string Subcategory = "Podkategória";
    public const string Amount = "Suma";
    public const string Count = "Počet";

    // The rule form
    public const string NewRule = "Nové pravidlo";
    public const string Condition = "Podmienka";
    public const string KindText = "Text v popise";
    public const string KindAmount = "Suma";
    public const string KindType = "Typ transakcie";
    public const string ModeContains = "obsahuje";
    public const string ModeEqualTo = "rovná sa";
    public const string AmountFrom = "Od";
    public const string AmountTo = "Do";
    public const string TargetCategory = "Kategória";
    public const string TargetSubcategory = "Podkategória";
    public const string AddRule = "Pridať pravidlo";
    public const string Rules = "Moje pravidlá";
    public const string NoRules = "Zatiaľ žiadne pravidlá.";
    public const string AmountNotANumber = "Suma musí byť číslo, napríklad 12,50.";
    public const string RuleMatchedNothing = "Pravidlo je uložené, ale v tomto výpise mu nezodpovedá žiadna transakcia.";
    public const string RulesFileUnreadable = "Súbor s pravidlami sa nepodarilo prečítať, preto sa nepoužije. Kým sa to nevyrieši, nové pravidlá nemožno ukladať.";
    public const string RulesCannotBeSaved = "Pravidlo nemožno uložiť, lebo súbor s pravidlami sa nepodarilo prečítať.";
    public const string RuleNotSaved = "Pravidlo sa nepodarilo uložiť.";

    /// <summary>"Pravidlo zaradilo 1 transakciu / 3 transakcie / 5 transakcií."</summary>
    public static string RuleMatched(int count) => count switch
    {
        1 => "Pravidlo zaradilo 1 transakciu.",
        >= 2 and <= 4 => $"Pravidlo zaradilo {count} transakcie.",
        _ => $"Pravidlo zaradilo {count} transakcií.",
    };

    // Titles of the samples that travel with the application
    public const string SampleTypicalMonth = "Bežný mesiac";
    public const string SampleBusyMonth = "Rušný mesiac";
    public const string SampleAwkwardMonth = "Nezvyčajný mesiac";
    public const string SampleEmptyMonth = "Mesiac bez transakcií";
    public const string SampleSingleTransactionMonth = "Mesiac s jednou transakciou";

    public static string TypeName(TransactionType type) => type switch
    {
        TransactionType.CardPayment => "Platba kartou",
        TransactionType.WalletCardPayment => "Platba mobilom",
        TransactionType.Transfer => "Prevod",
        TransactionType.InstantTransfer => "Okamžitý prevod",
        TransactionType.AccountFee => "Poplatok za vedenie účtu",
        TransactionType.FeeReversal => "Vrátenie poplatku",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    /// <summary>Why the categorizer refused a change, for the user. Its own messages are English, for developers.</summary>
    public static string Refusal(RefusalKind kind) => kind switch
    {
        RefusalKind.NoTypeChosen => "Vyberte typ transakcie.",
        RefusalKind.EmptyText => "Zadajte text, ktorý obsahuje aspoň jedno písmeno alebo číslicu.",
        RefusalKind.EmptyAccount => "Zadajte číslo účtu.",
        RefusalKind.EmptySymbol => "Zadajte variabilný symbol.",
        RefusalKind.InvalidAmountRange => "Zadajte sumu od, sumu do alebo obe; nesmú byť záporné a suma od nesmie byť vyššia ako suma do.",
        RefusalKind.EmptyName => "Zadajte názov.",
        RefusalKind.DuplicateName => "Taký názov už existuje.",
        RefusalKind.UnknownCategory => "Taká kategória neexistuje.",
        RefusalKind.NoSubcategoriesAllowed => "Do tejto kategórie nemožno pridať podkategóriu.",
        RefusalKind.InvalidPriority => "Priorita musí byť celé číslo od 1.",
        RefusalKind.UnknownTarget => "Vyberte podkategóriu.",
        RefusalKind.UnknownRule => "Také pravidlo neexistuje.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
