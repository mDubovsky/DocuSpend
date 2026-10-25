using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop;

/// <summary>
/// Windows' standard open window, for one statement. Avalonia's Windows open window never adds the file to Windows'
/// recent files (plan: verified in its source).
/// </summary>
internal sealed class StatementPicker(TopLevel window) : IStatementPicker
{
    internal static readonly IReadOnlyList<FilePickerFileType> Filters =
    [
        new(Texts.PickerPdfFiles) { Patterns = ["*.pdf"] },
        // Files are judged by their content, so a statement saved without its extension can be opened too.
        new(Texts.PickerAllFiles) { Patterns = ["*.*"] },
    ];

    public async Task<string?> PickAsync()
    {
        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Texts.PickerTitle,
            AllowMultiple = false,
            FileTypeFilter = Filters,
        });
        if (files.Count == 0)
            return null;
        // Every file chosen on Windows has a local path; without one, the empty path reads as "can't be opened".
        return files[0].TryGetLocalPath() ?? "";
    }
}
