using System.Text.Json.Nodes;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Categorization.Saving;

namespace DocuSpend.Categorization.Tests.Saving;

/// <summary>Hand choices as text and back (work/022-categorize-from-list/spec.md: what is kept).</summary>
public class SavedHandChoicesTests
{
    private static readonly CategorizationConfig Defaults = CategorizationConfig.Default;

    /// <summary>Everything a user can make, with two hand choices, one into a sub-category of their own.</summary>
    private static CategorizationConfig WithHandChoices()
    {
        var config = SavedRulesTests.Everything().WithFingerprintKey(FingerprintKey.Create());
        var own = config.Catalog.Categories.Single(c => c.Name == "Deti").Subcategories.Single(s => s.Name == "Krúžky").Id;
        config = config.SetHandChoice(new Fingerprint(new string('a', 64)), new SubcategoryId("transport.taxi")).Value!;
        return config.SetHandChoice(new Fingerprint(new string('b', 64)), own).Value!;
    }

    private static string Mutate(Action<JsonNode> change)
    {
        var json = JsonNode.Parse(SavedRules.Write(WithHandChoices()))!;
        change(json);
        return json.ToJsonString();
    }

    private static JsonNode Choice(JsonNode json, int i) => json["handChoices"]![i]!;

    [Fact]
    public void Hand_choices_and_the_key_come_back_exactly()
    {
        var config = WithHandChoices();

        var loaded = SavedRules.Read(SavedRules.Write(config), Defaults).Config!;

        Assert.Equal(config.HandChoices, loaded.HandChoices);
        Assert.Equal(config.FingerprintKey!.ToText(), loaded.FingerprintKey!.ToText());
        Assert.Equal(config.UserRules, loaded.UserRules);
    }

    [Fact]
    public void Without_hand_choices_the_file_has_an_empty_list_and_no_key()
    {
        var json = JsonNode.Parse(SavedRules.Write(Defaults))!;

        Assert.Empty(json["handChoices"]!.AsArray());
        Assert.Null(json["fingerprintKey"]);
        Assert.Equal(SavedRulesStatus.Loaded, SavedRules.Read(json.ToJsonString(), Defaults).Status);
    }

    [Fact]
    public void A_key_without_hand_choices_is_kept()
    {
        var config = Defaults.WithFingerprintKey(FingerprintKey.Create());

        var loaded = SavedRules.Read(SavedRules.Write(config), Defaults).Config!;

        Assert.Equal(config.FingerprintKey!.ToText(), loaded.FingerprintKey!.ToText());
    }

    [Fact]
    public void The_file_holds_the_fingerprints_and_targets_and_nothing_else_about_the_transactions()
    {
        var choices = JsonNode.Parse(SavedRules.Write(WithHandChoices()))!["handChoices"]!.AsArray();

        Assert.All(choices, c => Assert.Equal(["fingerprint", "target"], c!.AsObject().Select(p => p.Key)));
    }

    public static TheoryData<string, string> DamagedTexts() => new()
    {
        { "version 2 with hand choices", Mutate(j => j["version"] = 2) },
        { "version 2 with only a key", Mutate(j => { j["version"] = 2; j.AsObject().Remove("handChoices"); }) },
        { "version 3 without the list", Mutate(j => j.AsObject().Remove("handChoices")) },
        { "hand choices without a key", Mutate(j => j.AsObject().Remove("fingerprintKey")) },
        { "a key that is too short", Mutate(j => j["fingerprintKey"] = "AAECAw==") },
        { "a key that isn't base64", Mutate(j => j["fingerprintKey"] = "not a key") },
        { "an upper-case fingerprint", Mutate(j => Choice(j, 0)["fingerprint"] = new string('A', 64)) },
        { "a short fingerprint", Mutate(j => Choice(j, 0)["fingerprint"] = "abc") },
        { "a repeated fingerprint", Mutate(j => Choice(j, 1)["fingerprint"] = new string('a', 64)) },
        { "an extra field", Mutate(j => Choice(j, 0)["description"] = "LIDL") },
        { "a missing target", Mutate(j => Choice(j, 0).AsObject().Remove("target")) },
        { "a target user sub-category that isn't there", Mutate(j => Choice(j, 0)["target"] = "user-99999999999999999999999999999999") },
    };

    [Theory]
    [MemberData(nameof(DamagedTexts))]
    public void A_damaged_hand_choice_makes_the_whole_text_damaged(string what, string text)
    {
        var reading = SavedRules.Read(text, Defaults);

        Assert.True(reading.Status == SavedRulesStatus.Damaged, $"{what}: {reading.Status} {reading.Reason}");
        Assert.Null(reading.Config);
        Assert.DoesNotContain("user-", reading.Reason!);
    }

    [Fact]
    public void A_hand_choice_into_a_built_in_sub_category_this_version_does_not_have_means_a_newer_DocuSpend()
    {
        var reading = SavedRules.Read(Mutate(j => Choice(j, 0)["target"] = "housing.garden"), Defaults);

        Assert.Equal(SavedRulesStatus.Newer, reading.Status);
    }
}
