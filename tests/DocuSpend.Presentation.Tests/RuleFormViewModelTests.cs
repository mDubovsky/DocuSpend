using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>The rule form: six kinds of condition and a target, validated with Slovak messages (spec: adding a rule).</summary>
public class RuleFormViewModelTests
{
    private static readonly SubcategoryId Games = new("leisure.games");

    private static RuleFormViewModel Form() => new(CategorizationConfig.Default.Catalog);

    private static void Target(RuleFormViewModel form, SubcategoryId target)
    {
        form.SelectedCategory = form.Categories.Single(c => c.Subcategories.Any(s => s.Id == target));
        form.SelectedSubcategory = form.Subcategories.Single(s => s.Id == target);
    }

    [Fact]
    public void A_text_rule_is_built_with_its_mode()
    {
        var form = Form();
        form.SelectedKind = ConditionKind.Text;
        form.Text = "netflix";
        form.SelectedTextMode = form.TextModes.Single(m => m.Mode == TextMode.EqualTo);
        Target(form, Games);

        var draft = form.TryBuild();

        Assert.NotNull(draft);
        Assert.Equal(new TextCondition("netflix", TextMode.EqualTo), draft.Condition);
        Assert.Equal(Games, draft.Target);
        Assert.Null(form.Error);
    }

    [Theory]
    [InlineData("12,50", "", "12.50", null)]
    [InlineData("", "1 000", null, "1000")]
    [InlineData("10", "20,99", "10", "20.99")]
    public void An_amount_rule_reads_slovak_amounts(string from, string to, string? min, string? max)
    {
        var form = Form();
        form.SelectedKind = ConditionKind.Amount;
        form.AmountFrom = from;
        form.AmountTo = to;
        Target(form, Games);

        var draft = form.TryBuild();

        Assert.True(draft is not null, form.Error);
        static decimal? Parse(string? s) => s is null ? null : decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(new AmountCondition(Parse(min), Parse(max)), draft.Condition);
    }

    [Fact]
    public void A_type_rule_is_built_from_the_ticked_type()
    {
        var form = Form();
        form.SelectedKind = ConditionKind.Type;
        form.Types.Single(t => t.Type == TransactionType.InstantTransfer).IsChecked = true;
        Target(form, Games);

        var draft = form.TryBuild();

        Assert.NotNull(draft);
        Assert.Equal(new TypeCondition([TransactionType.InstantTransfer]), draft.Condition);
    }

    public static TheoryData<ConditionKind, string, string, string, string> Invalid => new()
    {
        { ConditionKind.Text, "", "", "", Texts.Refusal(RefusalKind.EmptyText) },
        { ConditionKind.Text, " - ", "", "", Texts.Refusal(RefusalKind.EmptyText) },
        { ConditionKind.Amount, "", "dvadsať", "", Texts.AmountNotANumber },
        { ConditionKind.Amount, "12.50", "", "", Texts.AmountNotANumber },
        { ConditionKind.Amount, "", "", "", Texts.Refusal(RefusalKind.InvalidAmountRange) },
        { ConditionKind.Amount, "30", "20", "", Texts.Refusal(RefusalKind.InvalidAmountRange) },
        { ConditionKind.Amount, "-5", "", "", Texts.Refusal(RefusalKind.InvalidAmountRange) },
        { ConditionKind.Type, "", "", "", Texts.Refusal(RefusalKind.NoTypeChosen) },
    };

    [Theory]
    [MemberData(nameof(Invalid))]
    public void Invalid_input_gives_a_slovak_message_and_no_rule(ConditionKind kind, string from, string to, string text, string message)
    {
        var form = Form();
        form.SelectedKind = kind;
        form.AmountFrom = from;
        form.AmountTo = to;
        form.Text = kind == ConditionKind.Text ? from : text;
        Target(form, Games);

        Assert.Null(form.TryBuild());
        Assert.Equal(message, form.Error);
    }

    [Fact]
    public void A_rule_without_a_subcategory_is_refused()
    {
        var form = Form();
        form.SelectedKind = ConditionKind.Text;
        form.Text = "netflix";
        form.SelectedCategory = form.Categories[0];

        Assert.Null(form.TryBuild());
        Assert.Equal(Texts.Refusal(RefusalKind.UnknownTarget), form.Error);
    }

    [Fact]
    public void Choosing_a_category_offers_its_subcategories_and_clears_the_previous_choice()
    {
        var form = Form();
        Target(form, Games);

        form.SelectedCategory = form.Categories.Single(c => c.Id == new CategoryId("food"));

        Assert.Equal(CategorizationConfig.Default.Catalog.FindCategory(new CategoryId("food"))!.Subcategories, form.Subcategories);
        Assert.Null(form.SelectedSubcategory);
    }

    [Fact]
    public void The_uncategorized_buckets_are_not_targets()
    {
        // A rule's target is always a sub-category, and the two Uncategorized buckets have none (item 003).
        Assert.DoesNotContain(Form().Categories, c => c.IsUncategorized);
    }

    [Fact]
    public void Every_choice_has_a_slovak_label()
    {
        var form = Form();

        Assert.Equal(Enum.GetValues<ConditionKind>(), form.Kinds.Select(k => k.Kind));
        Assert.Equal(["Text v popise", "Protistrana", "Účet protistrany", "Suma", "Typ transakcie", "Variabilný symbol"], form.Kinds.Select(k => k.Label));
        Assert.All(form.Kinds, k => Assert.False(string.IsNullOrWhiteSpace(k.Label)));
        Assert.All(form.TextModes, m => Assert.False(string.IsNullOrWhiteSpace(m.Label)));
        Assert.Equal(Enum.GetValues<TransactionType>().Length, form.Types.Count);
    }

    [Fact]
    public void A_successful_rule_clears_the_message_and_the_values()
    {
        var form = Form();
        form.SelectedKind = ConditionKind.Text;
        Target(form, Games);
        Assert.Null(form.TryBuild());

        form.Text = "netflix";
        Assert.NotNull(form.TryBuild());
        form.Clear();

        Assert.Null(form.Error);
        Assert.Equal("", form.Text);
    }

    // The kinds the form didn't offer before #24

    [Theory]
    [InlineData(TextMode.Contains)]
    [InlineData(TextMode.EqualTo)]
    public void A_counterparty_name_rule_is_built_with_its_mode(TextMode mode)
    {
        var form = Form();
        form.SelectedKind = ConditionKind.CounterpartyName;
        form.SelectedTextMode = form.TextModes.Single(m => m.Mode == mode);
        form.Text = "  Ján Vzorový ";
        Target(form, Games);

        Assert.Equal(new CounterpartyNameCondition("Ján Vzorový", mode), form.TryBuild()?.Condition);
    }

    [Fact]
    public void A_counterparty_account_rule_is_built_from_the_account_without_surrounding_spaces()
    {
        var form = Form();
        form.SelectedKind = ConditionKind.CounterpartyAccount;
        form.Account = " SK31 1200 0000 1987 4263 7541 ";
        Target(form, Games);

        Assert.Equal(new CounterpartyAccountCondition("SK31 1200 0000 1987 4263 7541"), form.TryBuild()?.Condition);
    }

    [Fact]
    public void A_variable_symbol_rule_is_built_from_the_symbol_without_surrounding_spaces()
    {
        var form = Form();
        form.SelectedKind = ConditionKind.VariableSymbol;
        form.Symbol = " 0326 ";
        Target(form, Games);

        Assert.Equal(new VariableSymbolCondition("0326"), form.TryBuild()?.Condition);
    }

    [Fact]
    public void A_type_rule_takes_every_ticked_type()
    {
        var form = Form();
        form.SelectedKind = ConditionKind.Type;
        form.Types.Single(t => t.Type == TransactionType.Transfer).IsChecked = true;
        form.Types.Single(t => t.Type == TransactionType.CardPayment).IsChecked = true;
        Target(form, Games);

        Assert.Equal(new TypeCondition([TransactionType.CardPayment, TransactionType.Transfer]), form.TryBuild()?.Condition);
    }

    public static TheoryData<ConditionKind, string, RefusalKind> Missing => new()
    {
        { ConditionKind.CounterpartyName, "", RefusalKind.EmptyText },
        { ConditionKind.CounterpartyName, "   ", RefusalKind.EmptyText },
        { ConditionKind.CounterpartyAccount, "", RefusalKind.EmptyAccount },
        { ConditionKind.CounterpartyAccount, "   ", RefusalKind.EmptyAccount },
        { ConditionKind.VariableSymbol, "", RefusalKind.EmptySymbol },
        { ConditionKind.VariableSymbol, "   ", RefusalKind.EmptySymbol },
        { ConditionKind.Type, "", RefusalKind.NoTypeChosen },
    };

    [Theory]
    [MemberData(nameof(Missing))]
    public void A_missing_value_gives_its_slovak_reason(ConditionKind kind, string value, RefusalKind refusal)
    {
        var form = Form();
        form.SelectedKind = kind;
        form.Text = form.Account = form.Symbol = value;
        Target(form, Games);

        Assert.Null(form.TryBuild());
        Assert.Equal(Texts.Refusal(refusal), form.Error);
    }

    [Fact]
    public void Only_the_fields_of_the_chosen_kind_show()
    {
        var form = Form();
        var shown = new Dictionary<ConditionKind, (bool Text, bool Account, bool Amount, bool Type, bool Symbol)>();
        foreach (var kind in Enum.GetValues<ConditionKind>())
        {
            form.SelectedKind = kind;
            shown[kind] = (form.IsText, form.IsAccount, form.IsAmount, form.IsType, form.IsSymbol);
        }

        Assert.Equal((true, false, false, false, false), shown[ConditionKind.Text]);
        Assert.Equal((true, false, false, false, false), shown[ConditionKind.CounterpartyName]);
        Assert.Equal((false, true, false, false, false), shown[ConditionKind.CounterpartyAccount]);
        Assert.Equal((false, false, true, false, false), shown[ConditionKind.Amount]);
        Assert.Equal((false, false, false, true, false), shown[ConditionKind.Type]);
        Assert.Equal((false, false, false, false, true), shown[ConditionKind.VariableSymbol]);
    }

    [Fact]
    public void Clearing_empties_every_kind_of_value()
    {
        var form = Form();
        form.Text = form.Account = form.Symbol = form.AmountFrom = form.AmountTo = "x";
        form.Types[0].IsChecked = true;

        form.Clear();

        Assert.All([form.Text, form.Account, form.Symbol, form.AmountFrom, form.AmountTo], v => Assert.Equal("", v));
        Assert.All(form.Types, t => Assert.False(t.IsChecked));
    }
}
