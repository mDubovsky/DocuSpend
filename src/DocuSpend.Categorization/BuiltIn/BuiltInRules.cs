using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.BuiltIn;

/// <summary>
/// The default rules from the spec's table, in the order of the table. Each alternative is its own rule, numbered
/// consecutively from 1; the first match wins. A space at either end of a text marks a word edge. The list will
/// grow: adding a rule only changes the result for transactions it matches.
/// Identifiers are derived from the target and the condition, so they stay the same when the list is reordered
/// or extended (decision 0004).
/// </summary>
internal static class BuiltInRules
{
    public static IReadOnlyList<Rule> Create()
    {
        var rules = new List<Rule>();

        void Add(Condition condition, string target, string key) =>
            rules.Add(new Rule(new RuleId($"default/{target}/{key}"), RuleSource.Default, rules.Count + 1, rules.Count + 1, condition, new SubcategoryId(target)));

        void Text(string target, params string[] texts)
        {
            foreach (var text in texts)
                Add(new TextCondition(text, TextMode.Contains), target, Key(text));
        }

        void Name(string target, params string[] texts)
        {
            foreach (var text in texts)
                Add(new CounterpartyNameCondition(text, TextMode.Contains), target, $"name:{Key(text)}");
        }

        Add(new TypeCondition([TransactionType.AccountFee, TransactionType.FeeReversal]), "fees.fees", "type");
        Text("own-transfers.other", "SPORIACI UCET");
        Text("housing.rent", "NAJOM");
        Text("housing.mortgage", "HYPOTEK", " HYPO ");
        Text("housing.equipment", " OBI ", "IKEA", "JYSK", "HORNBACH");
        Text("housing.other", "ENERGETIK", " SPP ", " ZSE ", "VODARENS");
        Text("income.salary", "MZD");
        Name("income.benefits", "URAD PRACE", "SOCIALNA POISTOVNA");
        Text("online-shopping.web-services", "NETFLIX", "SPOTIFY", "DISNEY", "HBO MAX", "YOUTUBE PREMIUM", "YOUTUBEPREMIUM");
        Text("online-shopping.other", "ALIEXPRESS", "ALLEGRO", " TEMU", "GOPAY", "PAYPAL", "PACKETA", " DPD ", "SHOPTET", "POSTA SK", "ZLAVOMAT");
        Text("food.groceries", "TESCO", "LIDL", "KAUFLAND", "BILLA", " COOP", "TERNO", "MINIMARKET", "POTRAVINY", "PEKAREN");
        Text("food.restaurants", "MCDONALD", " KFC ", "STARBUCKS", "BISTRO", "RESTAURACIA", "KAVIAREN", "PIZZ", "WOLT", "FOODORA");
        Text("car.fuel", "SLOVNAFT", "SHELL", " OMV ", "ORLEN", " MOL ", "CERPACIA STANICA");
        Text("car.service", "CARWASH", "AUTOSERVIS", "PNEUSERVIS", "AUTOUMYVAREN");
        Text("electronics.phone", "ORANGE", "SLOVAK TELEKOM", " O2 ", "MOJEO2");
        Text("electronics.electronics", " ALZA", "DATART", "ELEKTRO", "MALL.SK");
        Text("electronics.other", "MAGNETA", "SLOVANET");
        Text("health.drugstore", "DM DROGERIE", " DM ", "DROGERIA", " TETA", "ROSSMANN");
        Text("health.doctors", "SYNLAB", "NEMOCNIC", "POLIKLINIK");
        Text("health.other", "LEKAREN", "DR.MAX", " BENU", "OPTIKA");
        Text("transport.taxi", " BOLT", " UBER", "TAXI");
        Text("transport.public", "ZSSK", "REGIOJET", "FLIXBUS", "SLOVAK LINES", " MHD ");
        Text("other-expenses.insurance", "POISTOVNA", "POISTENIE", "UNIQA", "GENERALI", "ALLIANZ", "KOOPERATIVA");
        Text("other-expenses.other", "OKRESNY URAD", "MESTSKY URAD", "MINISTERSTVO");
        Text("education.school", "SKOLNE", "SKOLK", "MATERSKA SKOLA");
        Text("education.supplies", "KNIHKUPECTVO", "PAPIERNICTVO", "KNIHY");
        Text("education.courses", "DOUCOVANIE", "JAZYKOVA SKOLA");
        Text("leisure.games", "STEAM", "PLAYSTATION", "NINTENDO", " XBOX", "GOG SP", "BLIZZARD");
        Text("leisure.other", " KINO", "CINEMAX", "DECATHLON", "SKIPAS", "SKI RESORT");
        Text("investments.other", " XTB ", "TRADING 212", "FINAX", "INTERACTIVE BROKERS");

        return rules;
    }

    /// <summary>"HBO MAX" becomes "hbo-max": the part of a rule's identifier that comes from its text.</summary>
    private static string Key(string text) => TextMatching.Fold(text).Trim().Replace(' ', '-');
}
