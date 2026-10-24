using System.Text.Json;
using System.Text.Json.Serialization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation;

/// <summary>
/// THROWAWAY. The spike's rule file: the user rules in the order they were added, in the user's own
/// application-data folder. Its format is not the real one; that is designed in the issue for saving rules
/// (intent: decisions). A file it can't read is never overwritten: the store becomes unavailable and refuses to save.
/// </summary>
public sealed class SpikeRuleStore(string path)
{
    private const int Version = 1;

    public static string DefaultPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocuSpend", "spike-rules.json");

    public string FilePath { get; } = path;

    /// <summary>False once the file turned out unreadable; saving is then refused.</summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>The saved rules, oldest first. Empty, and unavailable, when the file can't be read.</summary>
    public IReadOnlyList<RuleDraft> Load()
    {
        if (!File.Exists(FilePath))
            return [];
        try
        {
            var file = JsonSerializer.Deserialize(File.ReadAllText(FilePath), StoreJson.Default.StoredFile);
            if (file is not { Version: Version, Rules: { } rules })
                return Unavailable();
            var drafts = rules.Select(ToDraft).ToList();
            return drafts.Any(d => d is null) ? Unavailable() : drafts!;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return Unavailable();
        }
    }

    /// <summary>Marks the file unreadable, for problems found after loading (a target that doesn't exist).</summary>
    public void MarkUnavailable() => IsAvailable = false;

    /// <summary>Replaces the file with these rules. False when the store is unavailable or the file can't be written.</summary>
    public bool Save(IReadOnlyList<RuleDraft> rules)
    {
        if (!IsAvailable)
            return false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(new StoredFile(Version, rules.Select(FromDraft).ToList()), StoreJson.Default.StoredFile);
            var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, json);
            File.Move(temporary, FilePath, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private List<RuleDraft> Unavailable()
    {
        IsAvailable = false;
        return [];
    }

    private static RuleDraft? ToDraft(StoredRule rule)
    {
        Condition? condition = rule.Kind switch
        {
            "text" when rule.Text is not null && Enum.TryParse<TextMode>(rule.Mode, out var mode) => new TextCondition(rule.Text, mode),
            "amount" => new AmountCondition(rule.Min, rule.Max),
            "type" when Enum.TryParse<TransactionType>(rule.Type, out var type) => new TypeCondition([type]),
            _ => null,
        };
        if (condition is null || condition.Validate() is not null || string.IsNullOrWhiteSpace(rule.Target))
            return null;
        return new RuleDraft(condition, new SubcategoryId(rule.Target));
    }

    private static StoredRule FromDraft(RuleDraft draft) => draft.Condition switch
    {
        TextCondition text => new StoredRule("text", text.Text, text.Mode.ToString(), null, null, null, draft.Target.Value),
        AmountCondition amount => new StoredRule("amount", null, null, amount.Min, amount.Max, null, draft.Target.Value),
        TypeCondition type => new StoredRule("type", null, null, null, null, type.Types.Single().ToString(), draft.Target.Value),
        _ => throw new NotSupportedException($"The spike saves only text, amount and type rules, not {draft.Condition.GetType().Name}."),
    };
}

internal sealed record StoredFile(int Version, List<StoredRule>? Rules);

internal sealed record StoredRule(string Kind, string? Text, string? Mode, decimal? Min, decimal? Max, string? Type, string Target);

/// <summary>Source-generated, so reading and writing the file needs no reflection.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(StoredFile))]
internal sealed partial class StoreJson : JsonSerializerContext;
