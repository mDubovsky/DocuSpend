using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuSpend.Categorization.Saving;

// The saved text, version 1 (plan: the saved shape). These records are the shape itself: changing one changes what
// DocuSpend writes, so the change needs a new version and a reader for this one (tests/…/Saving/rules-v1.json).

internal sealed record SavedFile(int Version, List<SavedCategory> Categories, List<SavedSubcategoryIn> Subcategories, List<SavedRule> Rules);

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
