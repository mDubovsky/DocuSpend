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

/// <summary>"Túto správu už nezobrazovať" in the box about fingerprints (work/022-categorize-from-list/spec.md).</summary>
public sealed class FingerprintNoteSettingTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "docuspend-tests-" + Guid.NewGuid().ToString("N"));

    private string File_ => Path.Combine(_folder, "DocuSpend", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private void WriteFile(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(File_)!);
        File.WriteAllText(File_, content);
    }

    [Fact]
    public void Without_a_file_the_box_shows() => Assert.False(new SettingsStore(File_).LoadHideFingerprintNote());

    [Fact]
    public void Switching_the_box_off_is_kept_and_can_be_undone()
    {
        Assert.True(new SettingsStore(File_).SaveHideFingerprintNote(true));
        Assert.True(new SettingsStore(File_).LoadHideFingerprintNote());

        Assert.True(new SettingsStore(File_).SaveHideFingerprintNote(false));
        Assert.False(new SettingsStore(File_).LoadHideFingerprintNote());
    }

    [Fact]
    public void Saving_the_look_keeps_the_box_switched_off_and_the_other_way_round()
    {
        var store = new SettingsStore(File_);
        store.SaveLook(LookChoice.Dark);
        store.SaveHideFingerprintNote(true);
        store.SaveLook(LookChoice.Light);

        var again = new SettingsStore(File_);
        Assert.Equal(LookChoice.Light, again.LoadLook());
        Assert.True(again.LoadHideFingerprintNote());
    }

    [Fact]
    public void A_settings_file_from_before_this_item_reads_as_show_the_box_and_keeps_its_look()
    {
        WriteFile("""{ "version": 1, "look": "dark" }""");

        var store = new SettingsStore(File_);
        Assert.False(store.LoadHideFingerprintNote());
        store.SaveHideFingerprintNote(true);
        Assert.Equal(LookChoice.Dark, new SettingsStore(File_).LoadLook());
    }

    [Fact]
    public void The_note_is_written_only_when_the_box_is_switched_off()
    {
        var store = new SettingsStore(File_);
        store.SaveLook(LookChoice.Dark);
        Assert.DoesNotContain("hideFingerprintNote", File.ReadAllText(File_));

        store.SaveHideFingerprintNote(true);
        Assert.Contains("\"hideFingerprintNote\": true", File.ReadAllText(File_));
    }

    [Theory]
    [InlineData("{ damaged")]
    [InlineData("""{ "version": 99, "hideFingerprintNote": true }""")]
    [InlineData("""{ "version": 1, "hideFingerprintNote": "yes" }""")]
    public void An_unreadable_file_means_show_the_box(string content)
    {
        WriteFile(content);

        Assert.False(new SettingsStore(File_).LoadHideFingerprintNote());
    }

    [Fact]
    public void A_file_that_cant_be_written_is_no_error()
    {
        Directory.CreateDirectory(File_);

        Assert.False(new SettingsStore(File_).SaveHideFingerprintNote(true));
    }
}
