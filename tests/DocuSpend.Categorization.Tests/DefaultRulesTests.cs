using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;
using static DocuSpend.Categorization.Tests.TestTransactions;

namespace DocuSpend.Categorization.Tests;

/// <summary>
/// The default rules against the spec's table (spec: Default rules). The table below is a deliberate second copy of
/// the spec's, so an accidental change to either side fails a test.
/// </summary>
public class DefaultRulesTests
{
    private static readonly CategorizationConfig Config = CategorizationConfig.Default;

    /// <summary>Rows in priority order: the target sub-category and the alternatives, each its own rule.</summary>
    private static readonly (string Target, string[] Texts)[] TextRows =
    [
        ("own-transfers.other", ["SPORIACI UCET"]),
        ("housing.rent", ["NAJOM"]),
        ("housing.mortgage", ["HYPOTEK"]),
        ("housing.equipment", [" OBI ", "IKEA", "JYSK", "HORNBACH"]),
        ("housing.other", ["ENERGETIK", " SPP ", " ZSE ", "VODARENS"]),
        ("income.salary", ["MZD"]),
    ];

    private static readonly (string Target, string[] Names)[] NameRows =
    [
        ("income.benefits", ["URAD PRACE", "SOCIALNA POISTOVNA"]),
    ];

    private static readonly (string Target, string[] Texts)[] MoreTextRows =
    [
        ("online-shopping.web-services", ["NETFLIX", "SPOTIFY", "DISNEY", "HBO MAX", "YOUTUBE PREMIUM", "YOUTUBEPREMIUM"]),
        ("online-shopping.other", ["ALIEXPRESS", "ALLEGRO", " TEMU", "GOPAY", "PAYPAL", "PACKETA", " DPD ", "SHOPTET", "POSTA SK"]),
        ("food.groceries", ["TESCO", "LIDL", "KAUFLAND", "BILLA", " COOP", "TERNO", "MINIMARKET", "POTRAVINY", "PEKAREN"]),
        ("food.restaurants", ["MCDONALD", " KFC ", "STARBUCKS", "BISTRO", "RESTAURACIA", "KAVIAREN", "PIZZ", "WOLT", "FOODORA"]),
        ("car.fuel", ["SLOVNAFT", "SHELL", " OMV ", "ORLEN", " MOL ", "CERPACIA STANICA"]),
        ("car.service", ["CARWASH", "AUTOSERVIS", "PNEUSERVIS", "AUTOUMYVAREN"]),
        ("electronics.phone", ["ORANGE", "SLOVAK TELEKOM", "O2 SLOVAKIA", "MOJEO2"]),
        ("electronics.electronics", [" ALZA", "DATART", "ELEKTRO", "MALL.SK"]),
        ("electronics.other", ["MAGNETA", "SLOVANET"]),
        ("health.drugstore", ["DM DROGERIE", "DROGERIA", " TETA", "ROSSMANN"]),
        ("health.doctors", ["SYNLAB", "NEMOCNIC", "POLIKLINIK"]),
        ("health.other", ["LEKAREN", "DR.MAX", " BENU", "OPTIKA"]),
        ("transport.taxi", [" BOLT", " UBER", "TAXI"]),
        ("transport.public", ["ZSSK", "REGIOJET", "FLIXBUS", "SLOVAK LINES", " MHD "]),
        ("other-expenses.insurance", ["POISTOVNA", "POISTENIE", "UNIQA", "GENERALI", "ALLIANZ", "KOOPERATIVA"]),
        ("other-expenses.other", ["OKRESNY URAD", "MESTSKY URAD", "MINISTERSTVO"]),
        ("education.school", ["SKOLNE", "SKOLK", "MATERSKA SKOLA"]),
        ("education.supplies", ["KNIHKUPECTVO", "PAPIERNICTVO", "KNIHY"]),
        ("education.courses", ["DOUCOVANIE", "JAZYKOVA SKOLA"]),
        ("leisure.games", ["STEAM", "PLAYSTATION", "NINTENDO", " XBOX", "GOG SP"]),
        ("leisure.other", [" KINO", "CINEMAX", "DECATHLON", "SKIPAS", "SKI RESORT"]),
        ("investments.other", [" XTB ", "TRADING 212", "FINAX", "INTERACTIVE BROKERS"]),
    ];

    /// <summary>The spec's table flattened into the conditions in the order of the rules.</summary>
    private static List<(Condition Condition, string Target)> ExpectedRules()
    {
        var expected = new List<(Condition, string)> { (new TypeCondition([TransactionType.AccountFee, TransactionType.FeeReversal]), "fees.fees") };
        expected.AddRange(TextRows.SelectMany(r => r.Texts.Select(t => ((Condition)new TextCondition(t, TextMode.Contains), r.Target))));
        expected.AddRange(NameRows.SelectMany(r => r.Names.Select(t => ((Condition)new CounterpartyNameCondition(t, TextMode.Contains), r.Target))));
        expected.AddRange(MoreTextRows.SelectMany(r => r.Texts.Select(t => ((Condition)new TextCondition(t, TextMode.Contains), r.Target))));
        return expected;
    }

    [Fact]
    public void The_default_rules_are_the_spec_table_in_order()
    {
        var expected = ExpectedRules();

        Assert.Equal(expected.Count, Config.DefaultRules.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Condition, Config.DefaultRules[i].Condition);
            Assert.Equal(expected[i].Target, Config.DefaultRules[i].Target.Value);
        }
    }

    [Fact]
    public void Priority_numbers_are_consecutive_from_one()
    {
        var priorities = Config.DefaultRules.Select(r => r.Priority).ToList();

        Assert.Equal(Enumerable.Range(1, priorities.Count), priorities);
    }

    [Fact]
    public void Identifiers_are_unique_and_derived_from_target_and_condition()
    {
        var ids = Config.DefaultRules.Select(r => r.Id.Value).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Contains("default/housing.rent/najom", ids);
        Assert.Contains("default/fees.fees/type", ids);
        Assert.All(Config.DefaultRules, r => Assert.StartsWith($"default/{r.Target}/", r.Id.Value));
    }

    [Fact]
    public void Identifiers_do_not_change_when_the_list_is_extended_in_the_middle()
    {
        var extended = CategorizationConfig.Create(
            Config.Catalog,
            [
                .. Config.DefaultRules.Take(3),
                new Rule(new RuleId("x"), RuleSource.Default, 4, 4, new TextCondition("NEW", TextMode.Contains), new SubcategoryId("housing.other")),
                .. Config.DefaultRules.Skip(3).Select(r => r with { Priority = r.Priority + 1, Order = r.Order + 1 }),
            ]);

        Assert.Equal(
            Config.DefaultRules.Select(r => r.Id),
            extended.DefaultRules.Where(r => r.Id.Value != "x").OrderBy(r => r.Priority).Select(r => r.Id));
    }

    [Fact]
    public void Every_default_rule_targets_an_existing_sub_category() =>
        Assert.All(Config.DefaultRules, r => Assert.NotNull(Config.Catalog.FindSubcategory(r.Target)));

    [Fact]
    public void Every_default_rule_is_a_valid_default_rule() =>
        Assert.All(Config.DefaultRules, r =>
        {
            Assert.Equal(RuleSource.Default, r.Source);
            Assert.Null(r.Condition.Validate());
        });

    // Behavior: every alternative puts its example into the listed sub-category.

    public static TheoryData<string, string> TextAlternatives()
    {
        var data = new TheoryData<string, string>();
        foreach (var (target, texts) in TextRows.Concat(MoreTextRows))
        {
            foreach (var text in texts)
                data.Add(text, target);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(TextAlternatives))]
    public void A_text_alternative_puts_its_example_in_the_listed_sub_category(string alternative, string target)
    {
        var example = Card($"Platba {alternative.Trim()} Nitra");

        Assert.Equal(target, Decide(example));
    }

    [Theory]
    [MemberData(nameof(TextAlternatives))]
    public void A_text_alternative_also_matches_at_the_ends_of_the_text(string alternative, string target)
    {
        Assert.Equal(target, Decide(Card($"{alternative.Trim()} Nitra")));
        Assert.Equal(target, Decide(Card($"Nitra {alternative.Trim()}")));
    }

    [Theory]
    [InlineData("Úrad práce, sociálnych vecí a rodiny")]
    [InlineData("SOCIALNA POISTOVNA")]
    [InlineData("Sociálna poisťovňa, pobočka Nitra")]
    public void The_benefit_offices_are_found_by_counterparty_name(string name) =>
        Assert.Equal("income.benefits", Decide(Transfer(amount: 150m, name: name)));

    [Fact]
    public void Fees_and_their_reversals_go_to_fees_by_type() =>
        Assert.All([Fee(-6.90m), Fee(6.90m)], t => Assert.Equal("fees.fees", Decide(t)));

    // Punctuation counts as a space, so names followed or preceded by a dot, comma or star are found.

    [Theory]
    [InlineData("Mol.Benzinka Nitra", "car.fuel")]
    [InlineData("Dr.Max 123 Nitra", "health.other")]
    [InlineData("GOOGLE*YouTubePremium", "online-shopping.web-services")]
    [InlineData("Bistro.sk, s. r. o.", "food.restaurants")]
    [InlineData("OBI,Nitra", "housing.equipment")]
    [InlineData("(COOP)", "food.groceries")]
    [InlineData("www.mall.sk", "electronics.electronics")]
    public void Names_next_to_punctuation_are_found(string text, string target) =>
        Assert.Equal(target, Decide(Card(text)));

    [Theory]
    [InlineData("DOMOL.BRATISLAVA")]
    [InlineData("SCOOP.CAFE")]
    [InlineData("KOBI.SKLAD")]
    public void Short_names_still_do_not_match_inside_other_words_next_to_punctuation(string text) =>
        Assert.Null(Decide(Card(text)));

    // Word edges: short names don't match inside other words.

    [Theory]
    [InlineData("DOMOL BRATISLAVA")]
    [InlineData("MOLDAVSKA CESTA")]
    [InlineData("KOBI SKLAD")]
    [InlineData("SCOOP CAFE")]
    [InlineData("GRUBER ONLINE")]
    [InlineData("MOHYLA")]
    public void Short_names_do_not_match_inside_other_words(string text) =>
        Assert.Null(Decide(Card(text)));

    // Order: the first rule in the table wins when several match.

    [Fact]
    public void The_first_matching_rule_wins_when_several_match()
    {
        // "KNIHY" (school supplies) is matched before "STEAM" (games) only if it comes first in the table; here the
        // text has both and the earlier row, supplies, decides.
        Assert.Equal("education.supplies", Decide(Card("KNIHY STEAM")));
        // A transfer that says "najom" and "poistenie" is rent, which comes first.
        Assert.Equal("housing.rent", Decide(Transfer(purpose: "Najom a poistenie")));
    }

    [Fact]
    public void A_transaction_nothing_matches_has_no_default()
    {
        Assert.Null(Decide(Card("MASIARSTVO U SUSEDA")));
        Assert.Null(Decide(Transfer(name: "Ján Príkladný", purpose: "Požičané")));
    }

    private static string? Decide(Transaction transaction) =>
        Config.RulesInOrder().FirstOrDefault(r => r.Condition.Matches(transaction))?.Target.Value;
}
