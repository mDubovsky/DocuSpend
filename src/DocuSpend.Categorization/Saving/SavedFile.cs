using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuSpend.Categorization.Saving;

// The saved text, versions 1 to 3 (plan: the saved shape; version 3 added the hand choices, work/022-categorize-from-list/plan.md). These records are the shape itself: changing one changes what
// DocuSpend writes, so the change needs a new version and a reader for this one (tests/…/Saving/rules-v1.json).

/// <summary>
/// <paramref name="FingerprintKey"/> and <paramref name="HandChoices"/> are in version 3 only, and always written there;
/// a version 1 or 2 text that has them is damaged.
/// </summary>
internal sealed record SavedFile(
    int Version,
    List<SavedCategory> Categories,
    List<SavedSubcategoryIn> Subcategories,
    List<SavedRule> Rules,
    string? FingerprintKey = null,
    List<SavedHandChoice>? HandChoices = null);

/// <summary>A sub-category chosen for one transaction, which only its fingerprint identifies.</summary>
internal sealed record SavedHandChoice(string Fingerprint, string Target);

/// <summary>A user category, with all its sub-categories, including its Ostatné.</summary>
internal sealed record SavedCategory(string Id, string Name, string Role, List<SavedSubcategory> Subcategories);

internal sealed record SavedSubcategory(string Id, string Name);

/// <summary>A user sub-category in a built-in category.</summary>
internal sealed record SavedSubcategoryIn(string Category, string Id, string Name);

internal sealed record SavedRule(string Id, int Priority, long Order, string Target, SavedCondition Condition);

/// <summary>One condition: its kind, and only that kind's fields.</summary>
internal sealed record SavedCondition(
    string Kind,
    List<string>? Types = null,
    string? Text = null,
    string? Mode = null,
    string? Account = null,
    decimal? Min = null,
    decimal? Max = null,
    string? Symbol = null);

/// <summary>
/// Source-generated, so reading and writing needs no reflection. Reading is strict: a missing, repeated or unknown
/// field, a null where a value belongs, a comment or trailing text makes the text unreadable, so nothing in a file is
/// ever silently dropped and then lost on the next save. Slovak letters are written as they are, so a person who opens
/// the file can read it. The options are set here, not on the attribute, because a context built from options ignores
/// the attribute's.
/// </summary>
[JsonSerializable(typeof(SavedFile))]
internal sealed partial class SavedRulesJson : JsonSerializerContext
{
    public static SavedRulesJson Strict { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    });
}
