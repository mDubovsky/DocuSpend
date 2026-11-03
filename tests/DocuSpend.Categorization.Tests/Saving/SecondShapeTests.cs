using System.Text.Json.Nodes;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Categorization.Saving;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Tests.Saving;

/// <summary>
/// rules-v2.json is a file of the second saved shape, written by hand: the first shape, raised so that an earlier
/// DocuSpend takes a file that may hold a cash-withdrawal rule for a newer one, not for a damaged one
/// (work/041-unknown-transaction-kinds/plan.md). Like rules-v1.json, never change it to follow a later shape.
/// </summary>
public class SecondShapeTests
{
    private static readonly string Text = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "Saving", "rules-v2.json"));

    private const string Hotovosť = "user-11111111111111111111111111111111";

    private static CategorizationConfig Loaded()
    {
        var reading = SavedRules.Read(Text, CategorizationConfig.Default);
        Assert.True(reading.Status == SavedRulesStatus.Loaded, reading.Reason);
        return reading.Config!;
    }

    [Fact]
    public void The_second_shape_loads_with_its_cash_withdrawal_rule()
    {
        var rules = Loaded().UserRules;

        Assert.Equal(3, rules.Count);
        Assert.Equal(new TypeCondition([TransactionType.CashWithdrawal]), rules[0].Condition);
        Assert.Equal(new SubcategoryId($"{Hotovosť}.other"), rules[0].Target);
        Assert.Equal(new TextCondition("hodinkami", TextMode.Contains), rules[1].Condition);
    }

    [Fact]
    public void On_sample_ten_the_saved_rules_place_cash_unknown_types_and_wallet_payments()
    {
        var result = Categorizer.Categorize(Samples.Read("10-new-types-month"), Loaded());
        string Where(Func<Transaction, bool> which) => result.Transactions.First(t => which(t.Transaction)).Placement.Subcategory!.Value.Value;

        Assert.Equal($"{Hotovosť}.other", Where(t => t.Type == TransactionType.CashWithdrawal));
        Assert.Equal("leisure.other", Where(t => t.PrintedType == "PLATBA HODINKAMI"));
        Assert.Equal("food.groceries", Where(t => t.Type == TransactionType.WalletCardPayment));
    }

    [Fact]
    public void This_DocuSpend_writes_the_second_shape_or_a_later_one()
    {
        // Version 3 followed (work/022-categorize-from-list/plan.md); ThirdShapeTests pins what is written now.
        var written = JsonNode.Parse(SavedRules.Write(Loaded()))!;

        Assert.True(written["version"]!.GetValue<int>() >= 2);
    }

    [Fact]
    public void A_file_with_a_cash_withdrawal_rule_is_one_an_earlier_DocuSpend_takes_for_newer()
    {
        // An earlier DocuSpend knew shape 1 only and reports any later version as newer, leaving the file untouched;
        // that check is pinned by SavedRulesTests' "a later version". A file this version writes is never shape 1.
        var config = Loaded();

        Assert.True(JsonNode.Parse(SavedRules.Write(config))!["version"]!.GetValue<int>() > 1);
    }

    [Fact]
    public void The_first_shape_still_loads_beside_the_second()
    {
        var v1 = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "Saving", "rules-v1.json"));

        Assert.Equal(SavedRulesStatus.Loaded, SavedRules.Read(v1, CategorizationConfig.Default).Status);
    }
}
