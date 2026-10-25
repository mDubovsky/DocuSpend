using System.Text.Json.Nodes;
using DocuSpend.Categorization.BuiltIn;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Categorization.Saving;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Tests.Saving;

/// <summary>The user's part of the categorization as text and back (spec: what is kept; starting; acceptance criteria).</summary>
public class SavedRulesTests
{
    private static readonly CategorizationConfig Defaults = CategorizationConfig.Default;

    private static CategorizationConfig Ok(Outcome<CategorizationConfig> outcome)
    {
        Assert.True(outcome.IsSuccess, outcome.Refusal?.Message);
        return outcome.Value!;
    }

    private static CategoryId CategoryNamed(CategorizationConfig config, string name) => config.Catalog.Categories.Single(c => c.Name == name).Id;

    private static SubcategoryId SubNamed(CategorizationConfig config, string category, string name) =>
        config.Catalog.Categories.Single(c => c.Name == category).Subcategories.Single(s => s.Name == name).Id;

    /// <summary>Everything a user can make: categories of each role, sub-categories, and every kind of condition.</summary>
    internal static CategorizationConfig Everything()
    {
        var c = Defaults;
        c = Ok(c.AddCategory("Deti", CategoryRole.Spending));
        c = Ok(c.AddSubcategory(CategoryNamed(c, "Deti"), "Krúžky"));
        c = Ok(c.AddCategory("Vedľajší príjem", CategoryRole.Income));
        c = Ok(c.AddCategory("Požičané rodine", CategoryRole.Excluded));
        c = Ok(c.AddSubcategory(new CategoryId("housing"), "Garáž"));
        c = Ok(c.AddSubcategory(new CategoryId("income"), "Prenájom"));

        var krúžky = SubNamed(c, "Deti", "Krúžky");
        var garáž = SubNamed(c, "Bývanie", "Garáž");
        var prenájom = SubNamed(c, "Príjmy", "Prenájom");
        var deti = SubNamed(c, "Deti", "Ostatné");
        var rodina = SubNamed(c, "Požičané rodine", "Ostatné");
        var príjem = SubNamed(c, "Vedľajší príjem", "Ostatné");

        c = Ok(c.AddUserRule(new TypeCondition([TransactionType.FeeReversal]), new SubcategoryId("fees.fees"), 1));
        c = Ok(c.AddUserRule(new TypeCondition([TransactionType.InstantTransfer, TransactionType.Transfer]), new SubcategoryId("own-transfers.other"), 6));
        c = Ok(c.AddUserRule(new TextCondition("minimarket", TextMode.Contains), new SubcategoryId("food.groceries"), 2));
        c = Ok(c.AddUserRule(new TextCondition("PEKAREN KLAS TRENCIN", TextMode.EqualTo), krúžky, 2));
        c = Ok(c.AddUserRule(new CounterpartyNameCondition("vzor", TextMode.Contains), rodina, 1));
        c = Ok(c.AddUserRule(new CounterpartyNameCondition("Bytové družstvo Lipa", TextMode.EqualTo), garáž, 3));
        c = Ok(c.AddUserRule(new CounterpartyAccountCondition("SK57 1100 0000 0029 4587 1630"), príjem, 1));
        c = Ok(c.AddUserRule(new AmountCondition(50m, 100.00m), deti, 2));
        c = Ok(c.AddUserRule(new AmountCondition(null, 12.50m), new SubcategoryId("leisure.games"), 5));
        c = Ok(c.AddUserRule(new AmountCondition(1000m, null), new SubcategoryId("investments.other"), 4));
        c = Ok(c.AddUserRule(new VariableSymbolCondition("0326"), new SubcategoryId("housing.equipment"), 2));
        c = Ok(c.AddUserRule(new TextCondition("Café „Ž“  😀 Lekáreň", TextMode.Contains), prenájom, 2));
        // A rule whose number changed after it was created keeps its place among equals.
        c = Ok(c.SetUserRulePriority(c.UserRules[2].Id, 7));
        return c;
    }

    private static CategorizationConfig RoundTrip(CategorizationConfig config)
    {
        var reading = SavedRules.Read(SavedRules.Write(config), Defaults);
        Assert.Equal(SavedRulesStatus.Loaded, reading.Status);
        Assert.Null(reading.Reason);
        return reading.Config!;
    }

    private static void AssertSameUserPart(CategorizationConfig expected, CategorizationConfig actual)
    {
        Assert.Equal(expected.UserRules, actual.UserRules);
        Assert.Equal(expected.DefaultRules, actual.DefaultRules);
        Assert.Equal(expected.Catalog.Categories.Count, actual.Catalog.Categories.Count);
        foreach (var (e, a) in expected.Catalog.Categories.Zip(actual.Catalog.Categories))
        {
            Assert.Equal(e with { Subcategories = [] }, a with { Subcategories = [] });
            Assert.Equal(e.Subcategories, a.Subcategories);
        }
    }

    private static void AssertSameCategorization(CategorizationConfig expected, CategorizationConfig actual)
    {
        foreach (var name in Samples.Valid)
        {
            var statement = Samples.Read(name);
            var before = Categorizer.Categorize(statement, expected).Transactions.Select(t => (t.Placement, t.Rule)).ToList();
            var after = Categorizer.Categorize(statement, actual).Transactions.Select(t => (t.Placement, t.Rule)).ToList();
            Assert.Equal(before, after);
        }
    }

    [Fact]
    public void Everything_the_user_made_comes_back_with_the_same_identifiers_conditions_priorities_and_orders()
    {
        var everything = Everything();
        AssertSameUserPart(everything, RoundTrip(everything));
    }

    [Fact]
    public void Every_valid_sample_is_categorized_the_same_after_a_round_trip_rule_for_rule()
    {
        var everything = Everything();
        AssertSameCategorization(everything, RoundTrip(everything));
    }

    [Fact]
    public void The_round_trip_test_is_not_vacuous_user_rules_decide_on_the_samples()
    {
        var decidedByUser = Samples.Valid
            .SelectMany(n => Categorizer.Categorize(Samples.Read(n), Everything()).Transactions)
            .Select(t => t.Rule).Where(r => r?.Source == RuleSource.User).Select(r => r!.Id).Distinct().Count();
        Assert.True(decidedByUser >= 8, $"Only {decidedByUser} user rules decided anything.");
    }

    [Fact]
    public void Texts_and_amounts_come_back_exactly_as_entered()
    {
        var loaded = RoundTrip(Everything());

        Assert.Contains(loaded.UserRules, r => r.Condition is TextCondition { Text: "Café „Ž“  😀 Lekáreň" });
        var amount = Assert.Single(loaded.UserRules, r => r.Condition is AmountCondition { Min: null }).Condition;
        Assert.Equal("12.50", ((AmountCondition)amount).Max!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Slovak_letters_are_written_as_they_are()
    {
        var text = SavedRules.Write(Everything());
        Assert.Contains("Krúžky", text);
        Assert.Contains("Bytové družstvo Lipa", text);
    }

    [Fact]
    public void A_rule_added_after_loading_is_created_after_every_loaded_rule_and_wins_their_tie()
    {
        var loaded = RoundTrip(Everything());
        var added = Ok(loaded.AddUserRuleOnTop(new TextCondition("x", TextMode.Contains), new SubcategoryId("food.other")));

        var rule = added.UserRules[^1];
        Assert.True(loaded.UserRules.All(r => r.Order < rule.Order));
        Assert.Equal(rule.Id, added.RulesInOrder().First().Id);
    }

    [Fact]
    public void Nothing_built_in_is_written()
    {
        var empty = JsonNode.Parse(SavedRules.Write(Defaults))!;
        Assert.Empty(empty["categories"]!.AsArray());
        Assert.Empty(empty["subcategories"]!.AsArray());
        Assert.Empty(empty["rules"]!.AsArray());

        var text = SavedRules.Write(Everything());
        foreach (var category in BuiltInCatalog.Create().Categories)
        {
            // A built-in category's identifier may appear only where a user sub-category says where it is, and a
            // built-in sub-category's only as a rule's target; their names never.
            Assert.DoesNotContain($"\"{category.Name}\"", text);
            Assert.All(category.Subcategories, s =>
            {
                Assert.DoesNotContain($"\"{s.Name}\"", text.Replace("\"Ostatné\"", ""));
                Assert.Equal(Count(text, $"\"target\": \"{s.Id}\""), Count(text, $"\"{s.Id}\""));
            });
        }
        Assert.All(Defaults.DefaultRules, r => Assert.DoesNotContain(r.Id.Value, text));
    }

    private static int Count(string text, string part) => (text.Length - text.Replace(part, "").Length) / part.Length;

    [Fact]
    public void A_loaded_file_uses_the_built_in_names_and_default_rules_of_the_DocuSpend_that_reads_it()
    {
        var text = SavedRules.Write(Everything());
        var renamed = new CategoryCatalog(BuiltInCatalog.Create().Categories.Select(c => c.Id.Value == "housing" ? c with { Name = "Bývanie a domácnosť" } : c));
        var newDefaults = CategorizationConfig.Create(renamed, Defaults.DefaultRules.Take(3));

        var loaded = SavedRules.Read(text, newDefaults).Config!;

        Assert.Equal("Bývanie a domácnosť", loaded.Catalog.FindCategory(new CategoryId("housing"))!.Name);
        Assert.Contains(loaded.Catalog.FindCategory(new CategoryId("housing"))!.Subcategories, s => s.Name == "Garáž");
        Assert.Equal(newDefaults.DefaultRules, loaded.DefaultRules);
    }

    [Fact]
    public void Every_value_has_a_fixed_word()
    {
        Assert.All(Enum.GetValues<TransactionType>(), t => Assert.Equal(t, Words.ParseType(Words.Type(t))));
        Assert.All(Enum.GetValues<CategoryRole>(), r => Assert.Equal(r, Words.ParseRole(Words.Role(r))));
        Assert.All(Enum.GetValues<TextMode>(), m => Assert.Equal(m, Words.ParseMode(Words.Mode(m))));
        Assert.Equal("equals", Words.Mode(TextMode.EqualTo));
    }

    [Fact]
    public void A_user_sub_category_named_like_a_built_in_sibling_is_not_damage()
    {
        var text = SavedRules.Write(Ok(Defaults.AddSubcategory(new CategoryId("housing"), "Garáž")));
        var json = JsonNode.Parse(text)!;
        json["subcategories"]![0]!["name"] = "Nájom";

        var reading = SavedRules.Read(json.ToJsonString(), Defaults);

        Assert.Equal(SavedRulesStatus.Loaded, reading.Status);
        Assert.Equal(2, reading.Config!.Catalog.FindCategory(new CategoryId("housing"))!.Subcategories.Count(s => s.Name == "Nájom"));
    }

    [Fact]
    public void An_empty_file_of_the_current_shape_loads_as_no_user_rules()
    {
        var reading = SavedRules.Read("""{ "version": 1, "categories": [], "subcategories": [], "rules": [] }""", Defaults);
        Assert.Equal(SavedRulesStatus.Loaded, reading.Status);
        Assert.Empty(reading.Config!.UserRules);
        AssertSameUserPart(Defaults, reading.Config);
    }

    // Damaged and newer texts, made from a good one so each case breaks exactly one thing.

    private static string Mutate(Action<JsonNode> change)
    {
        var json = JsonNode.Parse(SavedRules.Write(Everything()))!;
        change(json);
        return json.ToJsonString();
    }

    private static JsonNode Rule(JsonNode json, int i) => json["rules"]![i]!;

    public static TheoryData<string, string> DamagedTexts()
    {
        var good = SavedRules.Write(Everything());
        return new()
        {
            { "garbled", "{ \"version\": 1, \"categ" + "\u0000ories\": [ }" },
            { "cut short", good[..(good.Length / 2)] },
            { "empty", "" },
            { "only spaces", "   \n" },
            { "not an object", "[1, 2]" },
            { "no version", Mutate(j => j.AsObject().Remove("version")) },
            { "version as text", Mutate(j => j["version"] = "1") },
            { "version zero", Mutate(j => j["version"] = 0) },
            { "trailing text", good + " x" },
            { "a comment", good.Replace("\"rules\"", "/* c */ \"rules\"") },
            { "an unknown field", Mutate(j => j["extra"] = 1) },
            { "a missing list", Mutate(j => j.AsObject().Remove("rules")) },
            { "a null name", Mutate(j => j["categories"]![0]!["name"] = null) },
            { "an unknown role", Mutate(j => j["categories"]![0]!["role"] = "savings") },
            { "a blank category name", Mutate(j => j["categories"]![0]!["name"] = "  ") },
            { "two user categories with one name", Mutate(j => j["categories"]![1]!["name"] = "deti") },
            { "a user category without user- in its identifier", Mutate(j => j["categories"]![0]!["id"] = "kids") },
            { "a user category without its Ostatné", Mutate(j => j["categories"]![1]!["subcategories"]!.AsArray().RemoveAt(0)) },
            { "a repeated identifier", Mutate(j => j["categories"]![2]!["id"] = j["categories"]![1]!["id"]!.GetValue<string>()) },
            { "a sub-category in an Uncategorized bucket", Mutate(j => j["subcategories"]![0]!["category"] = "uncategorized-out") },
            { "a sub-category listed for a user category", Mutate(j => j["subcategories"]![0]!["category"] = j["categories"]![0]!["id"]!.GetValue<string>()) },
            { "a priority of 0", Mutate(j => Rule(j, 0)["priority"] = 0) },
            { "a repeated creation order", Mutate(j => Rule(j, 1)["order"] = 1) },
            { "a rule without user- in its identifier", Mutate(j => Rule(j, 0)["id"] = "default/fees.fees/x") },
            { "an unknown kind of condition", Mutate(j => Rule(j, 0)["condition"]!["kind"] = "card") },
            { "an incomplete condition", Mutate(j => Rule(j, 2)["condition"]!["text"] = " ") },
            { "an amount condition with no bound", Mutate(j => Rule(j, 9)["condition"] = JsonNode.Parse("""{ "kind": "amount" }""")) },
            { "a condition with another kind's field", Mutate(j => Rule(j, 2)["condition"]!["symbol"] = "1") },
            { "an unknown transaction type", Mutate(j => Rule(j, 0)["condition"]!["types"] = new JsonArray("cash")) },
            { "an unknown mode", Mutate(j => Rule(j, 2)["condition"]!["mode"] = "starts") },
            { "a target user sub-category that isn't there", Mutate(j => Rule(j, 0)["target"] = "user-99999999999999999999999999999999") },
        };
    }

    [Theory]
    [MemberData(nameof(DamagedTexts))]
    public void A_damaged_text_is_reported_as_damaged_and_nothing_from_it_is_used(string what, string text)
    {
        var reading = SavedRules.Read(text, Defaults);

        Assert.True(reading.Status == SavedRulesStatus.Damaged, $"{what}: {reading.Status}");
        Assert.Null(reading.Config);
        Assert.False(string.IsNullOrEmpty(reading.Reason));
    }

    public static TheoryData<string, string> NewerTexts() => new()
    {
        { "a later version", Mutate(j => j["version"] = SavedRules.CurrentVersion + 1) },
        { "a later version of another shape", """{ "version": 2, "rulePacks": [] }""" },
        { "a target built-in sub-category this version doesn't have", Mutate(j => Rule(j, 0)["target"] = "housing.garden") },
        { "a sub-category in a built-in category this version doesn't have", Mutate(j => j["subcategories"]![0]!["category"] = "pets") },
    };

    [Theory]
    [MemberData(nameof(NewerTexts))]
    public void A_text_from_a_newer_DocuSpend_is_reported_as_newer(string what, string text)
    {
        var reading = SavedRules.Read(text, Defaults);

        Assert.True(reading.Status == SavedRulesStatus.Newer, $"{what}: {reading.Status}");
        Assert.Null(reading.Config);
    }

    [Fact]
    public void No_reason_contains_anything_from_the_text()
    {
        var everything = SavedRules.Write(Everything());
        foreach (var text in DamagedTexts().Select(row => row.Data.Item2).Concat(NewerTexts().Select(row => row.Data.Item2)))
        {
            var reason = SavedRules.Read(text, Defaults).Reason!;
            Assert.DoesNotContain("Krúžky", reason);
            Assert.DoesNotContain("user-", reason);
            Assert.DoesNotContain("minimarket", reason, StringComparison.OrdinalIgnoreCase);
        }
        Assert.NotEmpty(everything);
    }
}
