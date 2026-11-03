using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>What the condition starts with in the "Zaradiť" box (work/022-categorize-from-list/spec.md).</summary>
public class PrefillTests
{
    private static readonly DateOnly Day = new(2026, 9, 12);

    private static Transaction Make(TransactionType type, decimal amount = -23.45m, string? description = null,
        string? name = null, string? account = null, string? symbol = null) => new()
    {
        BookingDate = Day,
        ValueDate = Day,
        Type = type,
        Amount = amount,
        Currency = "EUR",
        Description = description,
        CounterpartyName = name,
        CounterpartyAccount = account,
        VariableSymbol = symbol,
        PrintedType = type == TransactionType.Unknown ? "PLATBA HODINKAMI" : null,
    };

    private static RuleFormViewModel Filled(Transaction transaction, SubcategoryId? target = null)
    {
        var form = new RuleFormViewModel(CategorizationConfig.Default.Catalog);
        form.Prefill(transaction, target);
        return form;
    }

    public static TheoryData<Transaction, ConditionKind> FirstKinds => new()
    {
        { Make(TransactionType.CardPayment, description: "LIDL BRATISLAVA 1234"), ConditionKind.Text },
        { Make(TransactionType.WalletCardPayment, description: "LIDL", name: "LIDL SK"), ConditionKind.Text },
        { Make(TransactionType.Transfer, description: "Úhrada", name: "Ján Vzorový", account: "SK00 1"), ConditionKind.CounterpartyName },
        { Make(TransactionType.InstantTransfer, description: "SEPA Okamžitá platba", account: "SK00 1"), ConditionKind.CounterpartyAccount },
        { Make(TransactionType.AccountFee, -6.90m, description: "Vedenie konta"), ConditionKind.Text },
        { Make(TransactionType.Unknown, description: null), ConditionKind.Text },
    };

    [Theory]
    [MemberData(nameof(FirstKinds))]
    public void The_kind_chosen_at_first_depends_on_the_transaction(Transaction transaction, ConditionKind kind) =>
        Assert.Equal(kind, Filled(transaction).SelectedKind);

    [Fact]
    public void Every_kind_starts_filled_in_from_the_transaction()
    {
        var form = Filled(Make(TransactionType.Transfer, -1234.5m, "Nájom marec", "Ján Vzorový", "SK00 1111", "0326"));

        Assert.Equal((ConditionKind.CounterpartyName, "Ján Vzorový", TextMode.Contains), (form.SelectedKind, form.Text, form.SelectedTextMode.Mode));
        Assert.Equal("SK00 1111", form.Account);
        Assert.Equal(("1234,5", "1234,5"), (form.AmountFrom, form.AmountTo));
        Assert.Equal([TransactionType.Transfer], form.Types.Where(t => t.IsChecked).Select(t => t.Type));
        Assert.Equal("0326", form.Symbol);

        form.SelectedKind = ConditionKind.Text;
        Assert.Equal("Nájom marec", form.Text);
        form.SelectedKind = ConditionKind.CounterpartyName;
        Assert.Equal("Ján Vzorový", form.Text);
    }

    [Fact]
    public void The_text_falls_back_to_the_counterparty_and_then_to_nothing()
    {
        var noDescription = Filled(Make(TransactionType.Transfer, name: "Ján"));
        noDescription.SelectedKind = ConditionKind.Text;

        Assert.Equal("Ján", noDescription.Text);
        Assert.Equal("", Filled(Make(TransactionType.AccountFee)).Text);
    }

    [Fact]
    public void A_missing_detail_leaves_its_field_empty_and_the_form_gives_the_usual_reason()
    {
        var form = Filled(Make(TransactionType.CardPayment, description: "LIDL"), new SubcategoryId("food.groceries"));

        Assert.Equal(("", ""), (form.Account, form.Symbol));
        form.SelectedKind = ConditionKind.CounterpartyName;
        Assert.Equal("", form.Text);
        Assert.Null(form.TryBuild());
        Assert.Equal("Zadajte text, ktorý obsahuje aspoň jedno písmeno alebo číslicu.", form.Error);
        form.SelectedKind = ConditionKind.CounterpartyAccount;
        Assert.Null(form.TryBuild());
        Assert.Equal("Zadajte číslo účtu.", form.Error);
        form.SelectedKind = ConditionKind.VariableSymbol;
        Assert.Null(form.TryBuild());
        Assert.Equal("Zadajte variabilný symbol.", form.Error);
    }

    [Fact]
    public void An_unknown_type_ticks_no_type_and_a_type_rule_is_refused()
    {
        var form = Filled(Make(TransactionType.Unknown, description: "x"), new SubcategoryId("food.groceries"));

        Assert.DoesNotContain(form.Types, t => t.IsChecked);
        form.SelectedKind = ConditionKind.Type;
        Assert.Null(form.TryBuild());
        Assert.Equal("Vyberte typ transakcie.", form.Error);
    }

    [Fact]
    public void The_amount_is_the_size_of_the_amount_for_money_coming_in_too()
    {
        var form = Filled(Make(TransactionType.Transfer, 42.50m, name: "Eva"));

        Assert.Equal(("42,50", "42,50"), (form.AmountFrom, form.AmountTo));
    }

    [Fact]
    public void The_prefilled_condition_catches_the_transaction_for_every_kind_it_can_be_made_of()
    {
        var transaction = Make(TransactionType.Transfer, -12.5m, "Nájom", "Ján", "SK00 1", "77");
        foreach (var kind in Enum.GetValues<ConditionKind>())
        {
            var form = Filled(transaction, new SubcategoryId("housing.rent"));
            form.SelectedKind = kind;

            var draft = form.TryBuild();

            Assert.True(draft is not null, $"{kind}: {form.Error}");
            Assert.True(draft.Condition.Matches(transaction), kind.ToString());
        }
    }

    [Fact]
    public void The_given_sub_category_is_chosen_and_none_otherwise()
    {
        var chosen = Filled(Make(TransactionType.CardPayment, description: "LIDL"), new SubcategoryId("food.groceries"));
        var none = Filled(Make(TransactionType.CardPayment, description: "LIDL"));

        Assert.Equal(new SubcategoryId("food.groceries"), chosen.SelectedSubcategory?.Id);
        Assert.Equal(new CategoryId("food"), chosen.SelectedCategory?.Id);
        Assert.Null(none.SelectedSubcategory);
    }

    [Fact]
    public void The_box_offers_no_Uncategorized_bucket() =>
        Assert.DoesNotContain(Filled(Make(TransactionType.CardPayment)).Categories, c => c.IsUncategorized);
}
