using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// Every kind of condition, added through the form (work/024-edit-rules/spec.md: the form offers every kind of
/// condition; acceptance criteria, every kind of condition).
/// </summary>
public sealed class EveryKindTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static readonly SubcategoryId Target = new("leisure.games");

    public sealed record Case(string Name, Action<RuleFormViewModel> Fill, Condition Expected, string Description)
    {
        public override string ToString() => Name;
    }

    private static Action<RuleFormViewModel> Text(ConditionKind kind, TextMode mode, string text) => form =>
    {
        form.SelectedKind = kind;
        form.SelectedTextMode = form.TextModes.Single(m => m.Mode == mode);
        form.Text = text;
    };

    private static Action<RuleFormViewModel> Amount(string from, string to) => form =>
    {
        form.SelectedKind = ConditionKind.Amount;
        form.AmountFrom = from;
        form.AmountTo = to;
    };

    private static Action<RuleFormViewModel> Types(params TransactionType[] types) => form =>
    {
        form.SelectedKind = ConditionKind.Type;
        foreach (var type in types)
            form.Types.Single(t => t.Type == type).IsChecked = true;
    };

    private static string Euro(decimal amount) => SlovakFormat.Amount(amount);

    public static TheoryData<Case> Cases => new(
    (Case[])[
        new("text contains", Text(ConditionKind.Text, TextMode.Contains, "kino"),
            new TextCondition("kino", TextMode.Contains), "Text obsahuje „kino“"),
        new("text equals", Text(ConditionKind.Text, TextMode.EqualTo, "Kino Lumière"),
            new TextCondition("Kino Lumière", TextMode.EqualTo), "Text sa rovná „Kino Lumière“"),
        new("name contains", Text(ConditionKind.CounterpartyName, TextMode.Contains, "vzor"),
            new CounterpartyNameCondition("vzor", TextMode.Contains), "Protistrana obsahuje „vzor“"),
        new("name equals", Text(ConditionKind.CounterpartyName, TextMode.EqualTo, "Ján Vzorový"),
            new CounterpartyNameCondition("Ján Vzorový", TextMode.EqualTo), "Protistrana sa rovná „Ján Vzorový“"),
        new("account", form => { form.SelectedKind = ConditionKind.CounterpartyAccount; form.Account = "SK31 1200 0000 1987 4263 7541"; },
            new CounterpartyAccountCondition("SK31 1200 0000 1987 4263 7541"), "Účet protistrany: SK31 1200 0000 1987 4263 7541"),
        new("amount from", Amount("100", ""), new AmountCondition(100m, null), $"Suma od {Euro(100m)}"),
        new("amount to", Amount("", "12,50"), new AmountCondition(null, 12.50m), $"Suma do {Euro(12.50m)}"),
        new("amount from and to", Amount("10", "60,5"), new AmountCondition(10m, 60.5m), $"Suma od {Euro(10m)} do {Euro(60.5m)}"),
        new("one type", Types(TransactionType.CardPayment),
            new TypeCondition([TransactionType.CardPayment]), "Typ: Platba kartou"),
        new("several types", Types(TransactionType.Transfer, TransactionType.CardPayment),
            new TypeCondition([TransactionType.CardPayment, TransactionType.Transfer]), "Typ: Platba kartou, Prevod"),
        new("variable symbol", form => { form.SelectedKind = ConditionKind.VariableSymbol; form.Symbol = "0326"; },
            new VariableSymbolCondition("0326"), "Variabilný symbol: 0326"),
    ]);

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task A_rule_of_each_kind_added_through_the_form_is_the_rule_the_categorization_makes(Case @case)
    {
        var main = new MainViewModel(new RuleStore(Path.Combine(_folder, "rules.json")));
        await main.OpenFileAsync(Samples.Path("01-typical-month"));
        @case.Fill(main.RuleForm);
        main.RuleForm.SelectedCategory = main.RuleForm.Categories.Single(c => c.Subcategories.Any(s => s.Id == Target));
        main.RuleForm.SelectedSubcategory = main.RuleForm.Subcategories.Single(s => s.Id == Target);

        main.AddRuleCommand.Execute(null);

        Assert.Null(main.RuleForm.Error);
        var rule = Assert.Single(main.Statement.Config.UserRules);
        Assert.Equal(@case.Expected, rule.Condition);
        Assert.Equal(Target, rule.Target);
        Assert.Equal(@case.Description, Assert.Single(main.Rules).Condition);

        var direct = CategorizationConfig.Default.AddUserRuleOnTop(@case.Expected, Target).Value!;
        foreach (var sample in Samples.Valid)
        {
            await main.OpenFileAsync(Samples.Path(sample));
            Assert.Equal(
                Categorizer.Categorize(Samples.Read(sample), direct).Transactions.Select(t => t.Placement),
                main.Statement.Categorized!.Transactions.Select(t => t.Placement));
        }
    }
}
