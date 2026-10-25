using System.Text;
using DocuSpend.Categorization;
using DocuSpend.Categorization.Saving;

namespace DocuSpend.Presentation;

/// <summary>Why the saved rules aren't used at start, if they aren't (spec: when the saved rules can't be used).</summary>
public enum RulesProblem
{
    /// <summary>The saved rules were used, or there were none.</summary>
    None,

    /// <summary>The file couldn't be understood; it was moved aside and saving goes on.</summary>
    Damaged,

    /// <summary>The file couldn't be opened right now; it is left untouched and nothing is saved this session.</summary>
    CannotOpen,

    /// <summary>A newer DocuSpend wrote the file; it is left untouched and nothing is saved this session.</summary>
    Newer,
}

/// <param name="Config">The categorization to start with: the saved one, or the built-in one when there is a problem.</param>
/// <param name="MovedTo">Where a damaged file was moved, for the message; otherwise null.</param>
public sealed record RuleStoreLoad(CategorizationConfig Config, RulesProblem Problem, string? MovedTo = null);

/// <summary>
/// The user's rules and categories in a file in their application-data folder (spec: what is kept; saving). A save
/// writes a complete temporary file, flushes it to disk and renames it over the old one, so the file is always either
/// the old rules or the new ones. A file that can't be read is never overwritten: a damaged one is moved aside first,
/// and one that is locked or from a newer DocuSpend turns saving off for the session.
/// </summary>
public sealed class RuleStore(string path, TimeProvider? clock = null)
{
    public static string DefaultPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocuSpend", "rules.json");

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public string FilePath { get; } = Path.GetFullPath(path);

    private string Folder => Path.GetDirectoryName(FilePath)!;

    /// <summary>False once the file turned out locked or newer: saving is then refused until DocuSpend closes.</summary>
    public bool IsSaving { get; private set; } = true;

    /// <summary>The saved rules on top of <paramref name="defaults"/>, or the defaults and the reason they aren't.</summary>
    public RuleStoreLoad Load(CategorizationConfig defaults)
    {
        if (Directory.Exists(FilePath))
            return Off(defaults, RulesProblem.CannotOpen);

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(FilePath);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return new RuleStoreLoad(defaults, RulesProblem.None);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Off(defaults, RulesProblem.CannotOpen);
        }

        var reading = Decode(bytes) is { } text ? SavedRules.Read(text, defaults) : null;
        switch (reading?.Status)
        {
            case SavedRulesStatus.Loaded:
                return new RuleStoreLoad(reading.Config!, RulesProblem.None);
            case SavedRulesStatus.Newer:
                return Off(defaults, RulesProblem.Newer);
            default:
                return MoveAside() is { } movedTo
                    ? new RuleStoreLoad(defaults, RulesProblem.Damaged, movedTo)
                    : Off(defaults, RulesProblem.CannotOpen);
        }
    }

    /// <summary>
    /// Replaces the saved rules with the user's part of <paramref name="config"/>. False when saving is off or the
    /// file couldn't be written; the previous file is then as it was.
    /// </summary>
    public bool Save(CategorizationConfig config)
    {
        if (!IsSaving)
            return false;
        var temporary = Path.Combine(Folder, $"{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(Folder);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(Utf8.GetBytes(SavedRules.Write(config)));
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, FilePath, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            try
            {
                File.Delete(temporary);
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
                // Left behind; it is never read, because only the file itself is.
            }
            return false;
        }
    }

    private RuleStoreLoad Off(CategorizationConfig defaults, RulesProblem problem)
    {
        IsSaving = false;
        return new RuleStoreLoad(defaults, problem);
    }

    /// <summary>The text, or null when the bytes aren't UTF-8. A byte-order mark, which some editors add, is allowed.</summary>
    private static string? Decode(byte[] bytes)
    {
        ReadOnlySpan<byte> mark = [0xEF, 0xBB, 0xBF];
        var start = bytes.AsSpan().StartsWith(mark) ? mark.Length : 0;
        try
        {
            return Utf8.GetString(bytes, start, bytes.Length - start);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    /// <summary>
    /// Moves the damaged file to rules.damaged-yyyyMMdd-HHmmss.json in the same folder, with -2, -3, … when that name
    /// is taken, so an earlier damaged file is never replaced. Null when it can't be moved.
    /// </summary>
    private string? MoveAside()
    {
        var name = Path.GetFileNameWithoutExtension(FilePath);
        var stamp = _clock.GetLocalNow().ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        for (var n = 1; n < 1000; n++)
        {
            var target = Path.Combine(Folder, n == 1 ? $"{name}.damaged-{stamp}.json" : $"{name}.damaged-{stamp}-{n}.json");
            if (File.Exists(target) || Directory.Exists(target))
                continue;
            try
            {
                File.Move(FilePath, target, overwrite: false);
                return target;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                if (File.Exists(target) || Directory.Exists(target))
                    continue;
                return null;
            }
        }
        return null;
    }
}
