using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The spike's throwaway rule file (spec: rules survive a restart). Every test uses its own temporary folder, never
/// the real application-data folder.
/// </summary>
public sealed class SpikeRuleStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string File_ => Path.Combine(_folder, "DocuSpend", "spike-rules.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static readonly IReadOnlyList<RuleDraft> Three =
    [
        new(new TextCondition("Netflix", TextMode.Contains), new SubcategoryId("online-shopping.web-services")),
        new(new AmountCondition(12.50m, null), new SubcategoryId("leisure.games")),
        new(new TypeCondition([TransactionType.InstantTransfer]), new SubcategoryId("food.groceries")),
    ];

    [Fact]
    public void Without_a_file_there_are_no_rules_and_saving_works()
    {
        var store = new SpikeRuleStore(File_);

        Assert.Empty(store.Load());
        Assert.True(store.IsAvailable);
    }

    [Fact]
    public void Rules_come_back_as_they_were_saved_in_order()
    {
        Assert.True(new SpikeRuleStore(File_).Save(Three));

        var loaded = new SpikeRuleStore(File_).Load();

        Assert.Equal(Three, loaded);
    }

    [Fact]
    public void Saving_creates_the_folder()
    {
        new SpikeRuleStore(File_).Save(Three);

        Assert.True(File.Exists(File_));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "version": 99, "rules": [] }""")]
    [InlineData("""{ "version": 1, "rules": [ { "kind": "colour", "target": "leisure.games" } ] }""")]
    [InlineData("""{ "version": 1, "rules": [ { "kind": "text", "target": "leisure.games" } ] }""")]
    public void A_damaged_file_is_not_available_and_is_never_overwritten(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(File_)!);
        File.WriteAllText(File_, content);
        var store = new SpikeRuleStore(File_);

        Assert.Empty(store.Load());
        Assert.False(store.IsAvailable);
        Assert.False(store.Save(Three));
        Assert.Equal(content, File.ReadAllText(File_));
    }

    [Fact]
    public void The_real_file_is_in_the_users_application_data_folder()
    {
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocuSpend", "spike-rules.json");

        Assert.Equal(expected, SpikeRuleStore.DefaultPath);
    }
}
