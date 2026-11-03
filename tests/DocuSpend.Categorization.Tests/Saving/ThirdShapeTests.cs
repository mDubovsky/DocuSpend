using System.Text.Json.Nodes;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Categorization.Saving;

namespace DocuSpend.Categorization.Tests.Saving;

/// <summary>
/// rules-v3.json is a file of the third saved shape, written by hand: the second, with the hand choices and the key
/// their fingerprints were made with (work/022-categorize-from-list/plan.md). The two fingerprints are those of two
/// transactions of sample 01 under that key. Like rules-v1.json and rules-v2.json, never change it to follow a later
/// shape: add a reader for this one instead.
/// </summary>
public class ThirdShapeTests
{
    private static readonly string Text = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "Saving", "rules-v3.json"));

    private const string Rodina = "user-22222222222222222222222222222222";

    private static CategorizationConfig Loaded()
    {
        var reading = SavedRules.Read(Text, CategorizationConfig.Default);
        Assert.True(reading.Status == SavedRulesStatus.Loaded, reading.Reason);
        return reading.Config!;
    }

    [Fact]
    public void The_third_shape_loads_with_its_rules_hand_choices_and_key()
    {
        var config = Loaded();

        Assert.Equal(6, config.UserRules.Count);
        Assert.Equal(
            [
                new HandChoice(new Fingerprint("e78ee27493da43ca954d70533dc00197279aa5c771162e64587ec65149f302fd"), new SubcategoryId("food.groceries")),
                new HandChoice(new Fingerprint("15ef16b3cd9697e323124fab92a8de9fc16ae5b8534373dc082c1b2aff544462"), new SubcategoryId(Rodina)),
            ],
            config.HandChoices);
        Assert.Equal("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=", config.FingerprintKey!.ToText());
    }

    [Fact]
    public void On_sample_one_the_hand_choices_decide_their_transactions_and_the_rules_the_rest()
    {
        var result = Categorizer.Categorize(Samples.Read("01-typical-month"), Loaded());
        var minimarkets = result.Transactions.Where(t => t.Transaction.Description == "MINIMARKET U LIPY TRENCIN").ToList();
        var netflix = result.Transactions.Single(t => t.Transaction.Description == "NETFLIX.COM");

        // The rule sends the minimarkets to leisure; the hand choice keeps the second in groceries.
        Assert.Equal(["leisure.other", "food.groceries", "leisure.other"], minimarkets.Select(t => t.Placement.Subcategory!.Value.Value));
        Assert.Equal([false, true, false], minimarkets.Select(t => t.ByHand));
        Assert.Equal(Rodina, netflix.Placement.Subcategory!.Value.Value);
        Assert.True(netflix.ByHand);
        Assert.Equal(2, result.Transactions.Count(t => t.ByHand));
    }

    [Fact]
    public void This_DocuSpend_writes_the_third_shape_and_reads_back_what_it_wrote()
    {
        var loaded = Loaded();
        var written = SavedRules.Write(loaded);

        Assert.Equal(3, SavedRules.CurrentVersion);
        Assert.Equal(3, JsonNode.Parse(written)!["version"]!.GetValue<int>());
        var again = SavedRules.Read(written, CategorizationConfig.Default).Config!;
        Assert.Equal(loaded.HandChoices, again.HandChoices);
        Assert.Equal(loaded.UserRules, again.UserRules);
        Assert.Equal(loaded.FingerprintKey!.ToText(), again.FingerprintKey!.ToText());
    }

    [Theory]
    [InlineData("rules-v1.json")]
    [InlineData("rules-v2.json")]
    public void The_earlier_shapes_still_load_beside_the_third_with_no_hand_choices(string file)
    {
        var text = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "Saving", file));

        var reading = SavedRules.Read(text, CategorizationConfig.Default);

        Assert.Equal(SavedRulesStatus.Loaded, reading.Status);
        Assert.Empty(reading.Config!.HandChoices);
        Assert.Null(reading.Config.FingerprintKey);
    }
}
