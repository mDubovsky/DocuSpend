using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuSpend.Presentation.Look;

/// <summary>
/// The user's settings: today only the choice of look, in DocuSpend's folder in their application-data folder
/// (spec: the choice is remembered). It holds nothing about statements, rules or the user. A file that can't be read
/// means "follow Windows", and the next choice replaces it; a file that can't be written is no error. Unlike the
/// rules, a lost preference costs one click, so it isn't protected the way the rules file is.
/// </summary>
public sealed class SettingsStore(string path)
{
    private const int Version = 1;

    public static string DefaultPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocuSpend", "settings.json");

    public string FilePath { get; } = path;

    public LookChoice LoadLook()
    {
        try
        {
            if (!File.Exists(FilePath))
                return LookChoice.System;
            var file = JsonSerializer.Deserialize(File.ReadAllText(FilePath), SettingsJson.Default.SettingsFile);
            return file is { Version: Version } && Enum.TryParse<LookChoice>(file.Look, ignoreCase: true, out var choice)
                ? choice
                : LookChoice.System;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return LookChoice.System;
        }
    }

    /// <summary>False when the file couldn't be written; the choice still applies for this session.</summary>
    public bool SaveLook(LookChoice choice)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(new SettingsFile(Version, choice.ToString().ToLowerInvariant()), SettingsJson.Default.SettingsFile);
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

internal sealed record SettingsFile(int Version, string? Look);

/// <summary>Source-generated, so reading and writing the file needs no reflection.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(SettingsFile))]
internal sealed partial class SettingsJson : JsonSerializerContext;
