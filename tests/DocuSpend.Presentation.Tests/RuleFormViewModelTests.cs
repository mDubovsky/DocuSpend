using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>The rule form: three kinds of condition and a target, validated with Slovak messages (spec: adding a rule).</summary>
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
    public void A_type_rule_is_built_from_the_chosen_type()
    {
        var form = Form();
        form.SelectedKind = ConditionKind.Type;
        form.SelectedType = form.Types.Single(t => t.Type == TransactionType.InstantTransfer);
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

        Assert.Equal([ConditionKind.Text, ConditionKind.Amount, ConditionKind.Type], form.Kinds.Select(k => k.Kind));
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
}
