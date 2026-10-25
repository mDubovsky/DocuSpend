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

    // Opening a statement (spec: starting; opening a statement)
    public const string StartHeading = "Otvorte výpis z účtu";
    public const string StartHint = "Vyberte mesačný výpis z účtu VÚB vo formáte PDF, alebo ho sem pretiahnite.";
    public const string StartPrivacy = "Výpis sa číta len vo vašom počítači. DocuSpend ho nikam neposiela ani neukladá.";
    public const string OpenStatement = "Otvoriť výpis…";
    public const string TryExample = "Vyskúšať na ukážke";
    public const string ExampleLabel = "Ukážka s vymyslenými údajmi";
    public const string Reading = "Výpis sa číta…";
    public const string DropHere = "Pustite výpis sem";
    public const string PickerTitle = "Otvoriť výpis";
    public const string PickerPdfFiles ="Výpisy vo formáte PDF";
    public const string PickerAllFiles = "Všetky súbory";
    public const string Close = "Zavrieť";

    // When a file can't be opened (spec: the messages table)
    public const string NotOpened = "Výpis sa neotvoril.";
    public const string PreviousStatementKept = "Zobrazený zostáva predchádzajúci výpis.";
    public const string DropSeveralFiles = "Pretiahnite naraz len jeden výpis.";
    public const string DropNotAFile = "Sem môžete pretiahnuť len súbor s výpisom.";

    /// <summary>What is wrong with the file and what to do, in the user's terms. Never the reader's own reason.</summary>
    public static string Problem(OpenProblem problem) => problem switch
    {
        OpenProblem.Empty => "Súbor je prázdny. Stiahnite výpis z internet bankingu znova.",
        OpenProblem.NotAPdf => "Súbor nie je PDF, alebo je poškodený či chránený heslom. Stiahnite výpis z internet bankingu znova, bez hesla.",
        OpenProblem.NotAVubStatement => "Tento súbor nie je mesačný výpis z účtu VÚB. DocuSpend zatiaľ vie čítať len tie.",
        OpenProblem.Unreadable => "Časť výpisu sa nedá prečítať. Výpis má možno podobu, ktorú DocuSpend ešte nepozná.",
        OpenProblem.DoesNotReconcile => "Pohyby vo výpise nesedia s jeho súčtami, preto by grafy neboli správne.",
        OpenProblem.CannotOpen => "Súbor sa nedá otvoriť. Možno bol presunutý alebo ho práve používa iný program.",
        OpenProblem.TooLarge => "Súbor je príliš veľký na mesačný výpis.",
        _ => throw new ArgumentOutOfRangeException(nameof(problem), problem, null),
    };

    /// <summary>The whole message for a file that wasn't opened; it says so when the previous statement stays.</summary>
    public static string NotOpenedMessage(OpenProblem problem, bool previousStatementKept) =>
        previousStatementKept
            ? $"{NotOpened} {Problem(problem)} {PreviousStatementKept}"
            : $"{NotOpened} {Problem(problem)}";

    // Column headers
    public const string Date = "Dátum";
    public const string Description = "Popis";
    public const string Subcategory = "Podkategória";
    public const string Amount = "Suma";
    public const string Count = "Počet";

    // The look
    public const string Look = "Vzhľad";
    public const string LookSystem = "Podľa systému";
    public const string LookLight = "Svetlý";
    public const string LookDark = "Tmavý";

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
