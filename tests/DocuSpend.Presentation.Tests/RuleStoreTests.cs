using System.Diagnostics;
using System.Text;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The rules file (spec: saving; starting; when the saved rules can't be used). Every test uses its own temporary
/// folder, never the real application-data folder.
/// </summary>
public sealed class RuleStoreTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("docuspend-rules-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Folder => Path.Combine(_root, "DocuSpend");

    private string FilePath => Path.Combine(Folder, "rules.json");

    private static readonly CategorizationConfig Defaults = CategorizationConfig.Default;

    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 10, 25, 14, 30, 12, TimeSpan.Zero));

    private RuleStore Store() => new(FilePath, Clock);

    internal static CategorizationConfig WithRules(int count = 3)
    {
        var config = Defaults.AddCategory("Deti", CategoryRole.Spending).Value!;
        config = config.AddUserRule(new TextCondition("Café „Ž“ 😀", TextMode.Contains), new SubcategoryId("food.groceries"), 2).Value!;
        config = config.AddUserRule(new AmountCondition(null, 12.50m), config.Catalog.Categories[^1].Subcategories[0].Id, 2).Value!;
        for (var i = 2; i < count; i++)
            config = config.AddUserRule(new TypeCondition([TransactionType.Transfer]), new SubcategoryId("own-transfers.other"), 1 + i % 5).Value!;
        return config;
    }

    private string[] FilesInFolder() =>
        Directory.EnumerateFileSystemEntries(Folder).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray()!;

    [Fact]
    public void Without_a_file_or_folder_the_defaults_are_used_with_no_problem_and_saving_works()
    {
        var store = Store();
        var load = store.Load(Defaults);

        Assert.Same(Defaults, load.Config);
        Assert.Equal(RulesProblem.None, load.Problem);
        Assert.True(store.IsSaving);
        Assert.False(Directory.Exists(Folder));
    }

    [Fact]
    public void With_the_folder_but_no_file_the_defaults_are_used_with_no_problem()
    {
        Directory.CreateDirectory(Folder);
        Assert.Equal(RulesProblem.None, Store().Load(Defaults).Problem);
    }

    [Fact]
    public void Saved_rules_come_back_in_a_new_store_exactly()
    {
        var config = WithRules();
        Assert.True(Store().Save(config));

        var load = Store().Load(Defaults);

        Assert.Equal(RulesProblem.None, load.Problem);
        Assert.Equal(config.UserRules, load.Config.UserRules);
        Assert.Equal(config.Catalog.Categories[^1] with { Subcategories = [] }, load.Config.Catalog.Categories[^1] with { Subcategories = [] });
    }

    [Fact]
    public void Saving_creates_the_folder_and_leaves_only_the_file()
    {
        Assert.True(Store().Save(WithRules()));
        Assert.Equal(["rules.json"], FilesInFolder());
    }

    [Fact]
    public void The_file_is_UTF8_without_a_byte_order_mark_and_readable()
    {
        Store().Save(WithRules());
        var bytes = File.ReadAllBytes(FilePath);

        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Contains("Café „Ž“", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void A_file_with_a_byte_order_mark_from_an_editor_still_loads()
    {
        Store().Save(WithRules());
        File.WriteAllBytes(FilePath, [.. Encoding.UTF8.Preamble, .. File.ReadAllBytes(FilePath)]);

        Assert.Equal(RulesProblem.None, Store().Load(Defaults).Problem);
    }

    [Fact]
    public void A_later_save_replaces_the_earlier_one()
    {
        Store().Save(WithRules(3));
        Store().Save(WithRules(5));

        Assert.Equal(5, Store().Load(Defaults).Config.UserRules.Count);
    }

    // An interrupted save: the rename never happened, so a temporary file is left next to the old rules.

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void After_an_interrupted_save_the_previous_rules_load_without_a_message(bool temporaryComplete)
    {
        var store = Store();
        store.Save(WithRules(3));
        var newer = Encoding.UTF8.GetBytes(Categorization.Saving.SavedRules.Write(WithRules(6)));
        File.WriteAllBytes(Path.Combine(Folder, $"rules.json.{Guid.NewGuid():N}.tmp"), temporaryComplete ? newer : newer[..(newer.Length / 3)]);

        var load = Store().Load(Defaults);

        Assert.Equal(RulesProblem.None, load.Problem);
        Assert.Equal(3, load.Config.UserRules.Count);
    }

    [Fact]
    public void A_leftover_temporary_file_does_not_stop_the_next_save()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, $"rules.json.{Guid.NewGuid():N}.tmp"), "{ half");

        var store = Store();
        Assert.Equal(RulesProblem.None, store.Load(Defaults).Problem);
        Assert.True(store.Save(WithRules()));
        Assert.Equal(3, Store().Load(Defaults).Config.UserRules.Count);
    }

    // Damaged files.

    public static TheoryData<string, byte[]> DamagedFiles() => new()
    {
        { "garbled", Encoding.UTF8.GetBytes("{ \"version\": 1, \"rul") },
        { "empty", [] },
        { "not UTF-8", [0x7B, 0xFF, 0xFE, 0x7D] },
        { "UTF-16", Encoding.Unicode.GetBytes("""{ "version": 1, "categories": [], "subcategories": [], "rules": [] }""") },
        { "a target that isn't there", Encoding.UTF8.GetBytes("""
            { "version": 1, "categories": [], "subcategories": [], "rules": [ { "id": "user-1", "priority": 1, "order": 1,
              "target": "user-9", "condition": { "kind": "variableSymbol", "symbol": "1" } } ] }
            """) },
    };

    [Theory]
    [MemberData(nameof(DamagedFiles))]
    public void A_damaged_file_is_moved_aside_byte_for_byte_and_saving_goes_on(string what, byte[] content)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllBytes(FilePath, content);
        File.WriteAllText(Path.Combine(Folder, "settings.json"), "{}");

        var store = Store();
        var load = store.Load(Defaults);

        Assert.True(load.Problem == RulesProblem.Damaged, what);
        Assert.Same(Defaults, load.Config);
        Assert.Equal(Path.Combine(Folder, "rules.damaged-20261025-143012.json"), load.MovedTo);
        Assert.Equal(content, File.ReadAllBytes(load.MovedTo!));
        Assert.Equal(["rules.damaged-20261025-143012.json", "settings.json"], FilesInFolder());
        Assert.Equal("{}", File.ReadAllText(Path.Combine(Folder, "settings.json")));

        Assert.True(store.IsSaving);
        Assert.True(store.Save(WithRules()));
        Assert.Equal(3, Store().Load(Defaults).Config.UserRules.Count);
    }

    [Fact]
    public void Two_damaged_files_found_on_two_starts_are_both_kept()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, "first");
        var first = Store().Load(Defaults).MovedTo;
        File.WriteAllText(FilePath, "second");
        var second = Store().Load(Defaults).MovedTo;

        Assert.NotEqual(first, second);
        Assert.Equal("first", File.ReadAllText(first!));
        Assert.Equal("second", File.ReadAllText(second!));
        Assert.EndsWith("rules.damaged-20261025-143012-2.json", second);
    }

    [Fact]
    public void A_damaged_file_that_cannot_be_moved_aside_is_left_untouched_and_saving_is_off()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, "damaged");
        // Read-only sharing lets the store read the file but not move it.
        using var holder = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var store = Store();
        var load = store.Load(Defaults);

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(RulesProblem.CannotOpen, load.Problem);
            Assert.False(store.IsSaving);
            Assert.Equal(["rules.json"], FilesInFolder());
        }
        else
        {
            // Linux lets a file that is open be renamed, so the move works there; the case can't be made this way.
            Assert.Equal(RulesProblem.Damaged, load.Problem);
        }
    }

    // Files that can't be opened right now, and files from a newer DocuSpend.

    [Fact]
    public void A_file_held_open_by_another_program_is_left_untouched_and_saving_is_off()
    {
        var config = WithRules();
        Store().Save(config);
        var before = File.ReadAllBytes(FilePath);

        var store = Store();
        RuleStoreLoad load;
        using (new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            load = store.Load(Defaults);

        Assert.Equal(RulesProblem.CannotOpen, load.Problem);
        Assert.Same(Defaults, load.Config);
        Assert.False(store.IsSaving);
        Assert.False(store.Save(WithRules(5)));
        Assert.Equal(before, File.ReadAllBytes(FilePath));
        Assert.Equal(["rules.json"], FilesInFolder());
    }

    [Fact]
    public void A_folder_in_the_files_place_is_a_file_that_cannot_be_opened()
    {
        Directory.CreateDirectory(FilePath);
        var store = Store();

        Assert.Equal(RulesProblem.CannotOpen, store.Load(Defaults).Problem);
        Assert.False(store.IsSaving);
        Assert.True(Directory.Exists(FilePath));
    }

    [Fact]
    public void A_file_from_a_newer_DocuSpend_is_left_untouched_and_saving_is_off()
    {
        Directory.CreateDirectory(Folder);
        const string Newer = """{ "version": 2, "rulePacks": [ "x" ] }""";
        File.WriteAllText(FilePath, Newer);

        var store = Store();
        var load = store.Load(Defaults);

        Assert.Equal(RulesProblem.Newer, load.Problem);
        Assert.False(store.IsSaving);
        Assert.False(store.Save(WithRules()));
        Assert.Equal(Newer, File.ReadAllText(FilePath));
        Assert.Equal(["rules.json"], FilesInFolder());
    }

    // Saves that fail.

    [Fact]
    public void A_save_fails_without_an_exception_when_a_file_is_in_the_folders_place()
    {
        File.WriteAllText(Folder, "");
        var store = Store();
        store.Load(Defaults);

        Assert.False(store.Save(WithRules()));
        Assert.True(store.IsSaving);

        File.Delete(Folder);
        Assert.True(store.Save(WithRules()));
    }

    [Fact]
    public void A_failed_save_leaves_the_previous_file_and_no_temporary_file()
    {
        var store = Store();
        store.Save(WithRules(3));
        var before = File.ReadAllBytes(FilePath);

        bool saved;
        using (new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            saved = store.Save(WithRules(5));

        if (OperatingSystem.IsWindows())
        {
            Assert.False(saved);
            Assert.Equal(before, File.ReadAllBytes(FilePath));
            Assert.Equal(["rules.json"], FilesInFolder());
        }
        else
        {
            // Linux replaces a file that is open by renaming over it, so the save succeeds there.
            Assert.True(saved);
        }
    }

    [Fact]
    public void A_thousand_rules_save_and_load_in_under_a_second_each()
    {
        var config = WithRules(1000);
        var store = Store();
        store.Save(config);

        var save = Stopwatch.StartNew();
        Assert.True(store.Save(config));
        save.Stop();
        var load = Stopwatch.StartNew();
        var loaded = Store().Load(Defaults);
        load.Stop();

        Assert.Equal(1000, loaded.Config.UserRules.Count);
        Assert.True(save.Elapsed < TimeSpan.FromSeconds(1), $"Saving took {save.Elapsed}.");
        Assert.True(load.Elapsed < TimeSpan.FromSeconds(1), $"Loading took {load.Elapsed}.");
    }
}

/// <summary>A clock that always says the same time, in UTC, so moved-aside names are predictable.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
