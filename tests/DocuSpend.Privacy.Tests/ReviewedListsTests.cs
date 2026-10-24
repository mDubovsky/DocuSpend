using System.Text.Json;

namespace DocuSpend.Privacy.Tests;

/// <summary>Version ranges and the reviewed lists are read strictly: a malformed entry fails rather than being skipped.</summary>
[Trait("Check", "privacy-dependencies")]
public class ReviewedListsTests
{
    [Theory]
    [InlineData("[6.2.4, 6.3)", "6.2.4.0", true)]
    [InlineData("[6.2.4, 6.3)", "6.2.9.1", true)]
    [InlineData("[6.2.4, 6.3)", "6.3.0.0", false)]
    [InlineData("[6.2.4, 6.3)", "6.2.3.9", false)]
    [InlineData("(6.2.4, 6.3]", "6.2.4.0", false)]
    [InlineData("(6.2.4, 6.3]", "6.3.0.0", true)]
    [InlineData("[1.0,)", "99.0.0.0", true)]
    [InlineData("(,2.0)", "1.9.0.0", true)]
    [InlineData("6.2.4", "6.2.4.0", true)]
    [InlineData("6.2.4", "6.2.5.0", false)]
    [InlineData("6", "6.0.0.0", true)]
    public void A_version_range_contains_the_versions_it_names(string range, string version, bool expected)
    {
        Assert.Equal(expected, VersionRange.Parse(range).Contains(Version.Parse(version)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("[,]")]
    [InlineData("[1.0, 2.0")]
    [InlineData("[2.0, 1.0)")]
    [InlineData("[1.0, 2.0, 3.0)")]
    [InlineData("latest")]
    [InlineData("*")]
    public void A_malformed_version_range_fails(string range)
    {
        Assert.Throws<FormatException>(() => VersionRange.Parse(range));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "allowed": [ { "assembly": "A", "versions": "[1.0, 2.0)", "reason": "", "reviewer": "R" } ] }""")]
    [InlineData("""{ "allowed": [ { "assembly": "A", "versions": "[1.0, 2.0)", "reason": "Why.", "reviewer": " " } ] }""")]
    [InlineData("""{ "allowed": [ { "assembly": "A", "reason": "Why.", "reviewer": "R" } ] }""")]
    [InlineData("""{ "allowed": [ { "assembly": "A", "versions": "any", "reason": "Why.", "reviewer": "R" } ] }""")]
    [InlineData("""{ "allowed": [ { "versions": "[1.0, 2.0)", "reason": "Why.", "reviewer": "R" } ] }""")]
    [InlineData("""{ "allowed": [], "allowEverything": true }""")]
    public void A_malformed_allowlist_fails(string json)
    {
        Assert.ThrowsAny<JsonException>(() => ReviewedLists.ParseAllowedDependencies(json));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "native": [ { "file": "x.dll", "reason": "Why." } ] }""")]
    [InlineData("""{ "native": [ { "reason": "Why.", "reviewer": "R" } ] }""")]
    public void A_malformed_native_library_list_fails(string json)
    {
        Assert.ThrowsAny<JsonException>(() => ReviewedLists.ParseNativeLibraries(json));
    }

    [Fact]
    public void A_well_formed_allowlist_is_read()
    {
        var entry = Assert.Single(ReviewedLists.ParseAllowedDependencies(
            """{ "allowed": [ { "assembly": "A", "versions": "[1.0, 2.0)", "reason": "Why.", "reviewer": "R" } ] }"""));

        Assert.Equal(new AllowedDependency("A", "[1.0, 2.0)", "Why.", "R"), entry);
    }
}
