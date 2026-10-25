using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Categorization.Saving;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Tests.Saving;

/// <summary>
/// rules-v1.json is a file of the first saved shape, written by hand. Every later DocuSpend must still read it the same
/// way (spec: rules saved by an earlier version). Never change the file or this test to follow a new shape: add a
/// reader for the old shape instead.
/// </summary>
public class FirstShapeTests
{
    private static readonly string Text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Saving", "rules-v1.json"));

    private static SubcategoryId Sub(string id) => new(id);

    private static Rule UserRule(int n, int priority, Condition condition, string target) =>
        new(new RuleId($"user-a{n:D31}"), RuleSource.User, priority, n, condition, Sub(target));

    private const string Deti = "user-11111111111111111111111111111111";
    private const string Krúžky = "user-22222222222222222222222222222222";
    private const string Príjem = "user-33333333333333333333333333333333";
    private const string Rodina = "user-44444444444444444444444444444444";
    private const string Garáž = "user-55555555555555555555555555555555";

    /// <summary>What the file means, written out in code.</summary>
    private static CategorizationConfig Expected()
    {
        var builtIn = CategorizationConfig.Default;
        var catalog = new CategoryCatalog(builtIn.Catalog.Categories
            .Select(c => c.Id.Value == "housing" ? c with { Subcategories = [.. c.Subcategories, new Subcategory(Sub(Garáž), "Garáž")] } : c)
            .Concat(
            [
                new Category(new CategoryId(Deti), "Deti", CategoryRole.Spending,
                    [new Subcategory(Sub($"{Deti}.other"), "Ostatné"), new Subcategory(Sub(Krúžky), "Krúžky")], IsBuiltIn: false),
                new Category(new CategoryId(Príjem), "Vedľajší príjem", CategoryRole.Income, [new Subcategory(Sub($"{Príjem}.other"), "Ostatné")], IsBuiltIn: false),
                new Category(new CategoryId(Rodina), "Požičané rodine", CategoryRole.Excluded, [new Subcategory(Sub($"{Rodina}.other"), "Ostatné")], IsBuiltIn: false),
            ]));
        Rule[] rules =
        [
            UserRule(1, 2, new TextCondition("minimarket", TextMode.Contains), "food.groceries"),
            UserRule(2, 2, new TextCondition("PEKAREN KLAS TRENCIN", TextMode.EqualTo), Krúžky),
            UserRule(3, 1, new CounterpartyNameCondition("vzor", TextMode.Contains), $"{Rodina}.other"),
            UserRule(4, 3, new CounterpartyNameCondition("Bytové družstvo Lipa", TextMode.EqualTo), Garáž),
            UserRule(5, 1, new CounterpartyAccountCondition("SK57 1100 0000 0029 4587 1630"), $"{Príjem}.other"),
            UserRule(6, 2, new AmountCondition(50m, 100.00m), $"{Deti}.other"),
            UserRule(7, 5, new AmountCondition(null, 12.50m), "leisure.games"),
            UserRule(8, 4, new AmountCondition(1000m, null), "investments.other"),
            UserRule(9, 2, new VariableSymbolCondition("0326"), "housing.equipment"),
            UserRule(10, 6, new TypeCondition([TransactionType.Transfer, TransactionType.InstantTransfer]), "own-transfers.other"),
            UserRule(11, 1, new TypeCondition([TransactionType.FeeReversal]), "fees.fees"),
            UserRule(12, 2, new TextCondition("Lekáreň", TextMode.Contains), "health.drugstore"),
        ];
        return CategorizationConfig.Create(catalog, builtIn.DefaultRules, rules);
    }

    private static CategorizationConfig Loaded()
    {
        var reading = SavedRules.Read(Text, CategorizationConfig.Default);
        Assert.True(reading.Status == SavedRulesStatus.Loaded, reading.Reason);
        return reading.Config!;
    }

    [Fact]
    public void The_first_shape_loads_with_the_rules_and_categories_it_describes()
    {
        var expected = Expected();
        var loaded = Loaded();

        Assert.Equal(expected.UserRules, loaded.UserRules);
        Assert.Equal(expected.Catalog.Categories.Count, loaded.Catalog.Categories.Count);
        foreach (var (e, a) in expected.Catalog.Categories.Zip(loaded.Catalog.Categories))
        {
            Assert.Equal(e with { Subcategories = [] }, a with { Subcategories = [] });
            Assert.Equal(e.Subcategories, a.Subcategories);
        }
    }

    [Fact]
    public void The_first_shape_categorizes_every_valid_sample_as_it_describes()
    {
        foreach (var name in Samples.Valid)
        {
            var statement = Samples.Read(name);
            Assert.Equal(
                Categorizer.Categorize(statement, Expected()).Transactions.Select(t => (t.Placement, t.Rule)),
                Categorizer.Categorize(statement, Loaded()).Transactions.Select(t => (t.Placement, t.Rule)));
        }
    }

    [Fact]
    public void On_sample_one_the_saved_priorities_decide_as_they_did()
    {
        var result = Categorizer.Categorize(Samples.Read("01-typical-month"), Loaded());
        string Where(string description) => result.Transactions.First(t => t.Transaction.Description == description).Placement.Subcategory!.Value.Value;

        Assert.Equal("food.groceries", Where("MINIMARKET U LIPY TRENCIN"));
        Assert.Equal(Krúžky, Where("PEKAREN KLAS TRENCIN"));
        // Priority 3 beats the type rule's 6.
        Assert.Equal(Garáž, Where("Najomne marec"));
        // Priority 1 beats the amount rule's 4.
        Assert.Equal($"{Príjem}.other", Where("MZDA 02/2026"));
        Assert.Equal("health.drugstore", Where("LEKAREN ZLATA RYBKA TRENCIN"));
    }
}
