using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuSpend.Privacy.Tests;

/// <summary>An entry in privacy/allowed-dependencies.json: a third-party assembly whose networking was reviewed.</summary>
public sealed record AllowedDependency(string Assembly, string Versions, string Reason, string Reviewer)
{
    public bool Allows(NetworkingFinding finding) =>
        finding.Assembly == Assembly && VersionRange.Parse(Versions).Contains(finding.Version);
}

/// <summary>An entry in privacy/native-libraries.json: a native file in the output that was reviewed.</summary>
public sealed record ReviewedNativeLibrary(string File, string Reason, string Reviewer);

/// <summary>
/// The reviewed lists in privacy/. Each entry must say why it is acceptable and who reviewed it; a file that
/// doesn't parse, or an entry missing either, fails the check rather than being skipped.
/// </summary>
public static class ReviewedLists
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static IReadOnlyList<AllowedDependency> ParseAllowedDependencies(string json)
    {
        var entries = JsonSerializer.Deserialize<AllowedFile>(json, Options)?.Allowed
            ?? throw new JsonException("privacy/allowed-dependencies.json must have an \"allowed\" list.");
        foreach (var entry in entries)
        {
            Require(entry.Assembly, "assembly", "privacy/allowed-dependencies.json");
            Require(entry.Reason, "reason", $"the entry for {entry.Assembly}");
            Require(entry.Reviewer, "reviewer", $"the entry for {entry.Assembly}");
            Require(entry.Versions, "versions", $"the entry for {entry.Assembly}");
            try { VersionRange.Parse(entry.Versions); }
            catch (FormatException e) { throw new JsonException($"The entry for {entry.Assembly}: {e.Message}"); }
        }
        return entries;
    }

    public static IReadOnlyList<ReviewedNativeLibrary> ParseNativeLibraries(string json)
    {
        var entries = JsonSerializer.Deserialize<NativeFile>(json, Options)?.Native
            ?? throw new JsonException("privacy/native-libraries.json must have a \"native\" list.");
        foreach (var entry in entries)
        {
            Require(entry.File, "file", "privacy/native-libraries.json");
            Require(entry.Reason, "reason", $"the entry for {entry.File}");
            Require(entry.Reviewer, "reviewer", $"the entry for {entry.File}");
        }
        return entries;
    }

    private static void Require(string? value, string field, string where)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException($"{where} needs a \"{field}\".");
    }

    private sealed record AllowedFile(List<AllowedDependency>? Allowed);
    private sealed record NativeFile(List<ReviewedNativeLibrary>? Native);
}
