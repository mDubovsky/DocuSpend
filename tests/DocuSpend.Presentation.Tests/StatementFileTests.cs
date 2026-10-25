namespace DocuSpend.Presentation.Tests;

/// <summary>Reading the file the user chose into memory (spec: opening a statement; when a file can't be opened).</summary>
public sealed class StatementFileTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-file-tests-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Temp(string name) => Path.Combine(_folder, name);

    [Fact]
    public async Task A_sample_is_read_whole()
    {
        var path = Path.Combine(Samples.Dir, "01-typical-month.pdf");

        var content = await StatementFile.ReadAsync(path);

        Assert.Null(content.Problem);
        Assert.Equal(File.ReadAllBytes(path), content.Bytes);
    }

    [Fact]
    public async Task An_empty_file_is_read_as_no_bytes_and_left_to_the_reader()
    {
        var path = Temp("empty.pdf");
        File.WriteAllBytes(path, []);

        var content = await StatementFile.ReadAsync(path);

        Assert.Null(content.Problem);
        Assert.Empty(content.Bytes!);
    }

    [Fact]
    public async Task A_missing_file_cannot_be_opened()
    {
        var content = await StatementFile.ReadAsync(Temp("gone.pdf"));

        Assert.Equal(OpenProblem.CannotOpen, content.Problem);
        Assert.Null(content.Bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("bad\0name.pdf")]
    public async Task A_path_that_is_not_a_path_cannot_be_opened(string path) =>
        Assert.Equal(OpenProblem.CannotOpen, (await StatementFile.ReadAsync(path)).Problem);

    [Fact]
    public async Task A_folder_cannot_be_opened() =>
        Assert.Equal(OpenProblem.CannotOpen, (await StatementFile.ReadAsync(_folder)).Problem);

    [Fact]
    public async Task A_file_another_program_holds_locked_cannot_be_opened()
    {
        var path = Temp("locked.pdf");
        File.WriteAllBytes(path, [1, 2, 3]);
        using var holder = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Assert.Equal(OpenProblem.CannotOpen, (await StatementFile.ReadAsync(path)).Problem);
    }

    [Fact]
    public async Task A_file_another_program_has_open_for_reading_still_opens()
    {
        var path = Temp("viewer.pdf");
        File.WriteAllBytes(path, [1, 2, 3]);
        // A PDF viewer typically keeps the file open, allowing others to read it.
        using var viewer = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var content = await StatementFile.ReadAsync(path);

        Assert.Null(content.Problem);
        Assert.Equal([1, 2, 3], content.Bytes);
    }

    [Fact]
    public async Task A_file_over_the_limit_is_too_large()
    {
        var path = Temp("huge.pdf");
        using (var file = new FileStream(path, FileMode.CreateNew))
            file.SetLength(StatementFile.MaxBytes + 1);

        var content = await StatementFile.ReadAsync(path);

        Assert.Equal(OpenProblem.TooLarge, content.Problem);
        Assert.Null(content.Bytes);
    }

    [Fact]
    public async Task A_file_exactly_at_the_limit_is_read()
    {
        var path = Temp("limit.pdf");
        using (var file = new FileStream(path, FileMode.CreateNew))
            file.SetLength(StatementFile.MaxBytes);

        var content = await StatementFile.ReadAsync(path);

        Assert.Null(content.Problem);
        Assert.Equal(StatementFile.MaxBytes, content.Bytes!.Length);
    }

    [Fact]
    public void The_limit_is_50_megabytes() => Assert.Equal(50L * 1024 * 1024, StatementFile.MaxBytes);
}
