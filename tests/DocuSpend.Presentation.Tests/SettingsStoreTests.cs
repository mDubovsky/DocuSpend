using DocuSpend.Presentation.Look;

namespace DocuSpend.Presentation.Tests;

/// <summary>
/// The remembered choice of look (spec: the choice is remembered). A lost preference costs one click, so anything
/// unreadable simply means "follow Windows". Every test uses its own temporary folder.
/// </summary>
public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string File_ => Path.Combine(_folder, "DocuSpend", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Without_a_file_the_look_follows_windows()
    {
        Assert.Equal(LookChoice.System, new SettingsStore(File_).LoadLook());
    }

    [Theory]
    [InlineData(LookChoice.System)]
    [InlineData(LookChoice.Light)]
    [InlineData(LookChoice.Dark)]
    public void The_choice_comes_back_as_it_was_saved(LookChoice choice)
    {
        Assert.True(new SettingsStore(File_).SaveLook(choice));

        Assert.Equal(choice, new SettingsStore(File_).LoadLook());
    }

    [Fact]
    public void The_file_holds_only_the_choice()
    {
        new SettingsStore(File_).SaveLook(LookChoice.Dark);

        var json = File.ReadAllText(File_);

        Assert.Contains("\"look\"", json);
        Assert.Contains("\"version\"", json);
        Assert.Equal(2, json.Count(c => c == ':'));
    }

    [Theory]
    [InlineData("{ damaged")]
    [InlineData("""{ "version": 99, "look": "dark" }""")]
    [InlineData("""{ "version": 1, "look": "purple" }""")]
    [InlineData("")]
    public void An_unreadable_file_means_follow_windows_and_is_replaced_by_the_next_choice(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(File_)!);
        File.WriteAllText(File_, content);
        var store = new SettingsStore(File_);

        Assert.Equal(LookChoice.System, store.LoadLook());
        Assert.True(store.SaveLook(LookChoice.Light));
        Assert.Equal(LookChoice.Light, new SettingsStore(File_).LoadLook());
    }

    [Fact]
    public void A_file_that_cant_be_written_is_no_error()
    {
        // A folder where the file should be: writing fails, and that's all.
        Directory.CreateDirectory(File_);

        Assert.False(new SettingsStore(File_).SaveLook(LookChoice.Dark));
    }

    [Fact]
    public void The_real_file_is_in_the_users_application_data_folder()
    {
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocuSpend", "settings.json");

        Assert.Equal(expected, SettingsStore.DefaultPath);
    }
}
