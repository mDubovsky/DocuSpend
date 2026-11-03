using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuSpend.Presentation.Look;

/// <summary>
/// The user's settings, in DocuSpend's folder in their application-data folder: the choice of look (spec: the choice
/// is remembered), and whether the box explaining fingerprints was switched off with "Túto správu už nezobrazovať"
/// (work/022-categorize-from-list/spec.md: the privacy box). It holds nothing about statements, rules or the user. A
/// file that can't be read means "follow Windows" and "show the box", and the next change replaces it; a file that
/// can't be written is no error. Unlike the rules, a lost preference costs one click, so it isn't protected the way the
/// rules file is. Saving one setting keeps the other as it is in the file.
/// </summary>
public sealed class SettingsStore(string path)
{
    private const int Version = 1;

    public static string DefaultPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocuSpend", "settings.json");

    public string FilePath { get; } = path;

    public LookChoice LoadLook() => Parse(Read()?.Look) ?? LookChoice.System;

    /// <summary>True when the user ticked "Túto správu už nezobrazovať" in the box about fingerprints.</summary>
    public bool LoadHideFingerprintNote() => Read() is { HideFingerprintNote: true };

    /// <summary>False when the file couldn't be written; the choice still applies for this session.</summary>
    public bool SaveLook(LookChoice choice) =>
        Write(new SettingsFile(Version, Word(choice), Read()?.HideFingerprintNote));

    /// <summary>False when the file couldn't be written; the box then shows again next time DocuSpend starts.</summary>
    public bool SaveHideFingerprintNote(bool hide)
    {
        var look = Parse(Read()?.Look);
        return Write(new SettingsFile(Version, look is { } l ? Word(l) : null, hide ? true : null));
    }

    private static string Word(LookChoice choice) => choice.ToString().ToLowerInvariant();

    private static LookChoice? Parse(string? word) =>
        Enum.TryParse<LookChoice>(word, ignoreCase: true, out var choice) && Enum.IsDefined(choice) ? choice : null;

    /// <summary>The file, or null when there is none or it can't be read as this version's settings.</summary>
    private SettingsFile? Read()
    {
        try
        {
            if (!File.Exists(FilePath))
                return null;
            var file = JsonSerializer.Deserialize(File.ReadAllText(FilePath), SettingsJson.Default.SettingsFile);
            return file is { Version: Version } ? file : null;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private bool Write(SettingsFile file)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(file, SettingsJson.Default.SettingsFile);
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
}

/// <param name="HideFingerprintNote">Written only when true, so a file without it reads as "show the box".</param>
internal sealed record SettingsFile(int Version, string? Look, bool? HideFingerprintNote = null);

/// <summary>Source-generated, so reading and writing the file needs no reflection.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SettingsFile))]
internal sealed partial class SettingsJson : JsonSerializerContext;
