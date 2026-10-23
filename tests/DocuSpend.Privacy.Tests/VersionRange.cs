namespace DocuSpend.Privacy.Tests;

/// <summary>
/// A range of assembly versions in interval notation: "[6.2.4, 6.3)" includes 6.2.4 and everything up to, but not
/// including, 6.3; "(a, b]", "[a,)" and so on likewise. A bare version, "6.2.4", means exactly that version.
/// Missing version parts count as zero, so 6.2.4 equals 6.2.4.0.
/// </summary>
public sealed record VersionRange(Version? Min, bool MinInclusive, Version? Max, bool MaxInclusive)
{
    public static VersionRange Parse(string text)
    {
        var s = text.Trim();
        if (s.Length > 0 && s[0] is not ('[' or '('))
        {
            var exact = ParseVersion(s, text);
            return new VersionRange(exact, true, exact, true);
        }
        if (s.Length < 3 || s[^1] is not (']' or ')') || s.Count(c => c == ',') != 1)
            throw new FormatException($"Not a version range: '{text}'. Use for example \"[6.2.4, 6.3)\".");

        var parts = s[1..^1].Split(',', StringSplitOptions.TrimEntries);
        var min = parts[0].Length == 0 ? null : ParseVersion(parts[0], text);
        var max = parts[1].Length == 0 ? null : ParseVersion(parts[1], text);
        if (min is null && max is null)
            throw new FormatException($"A version range must have at least one bound: '{text}'.");
        if (min is not null && max is not null && min > max)
            throw new FormatException($"A version range's lower bound is above its upper bound: '{text}'.");
        return new VersionRange(min, s[0] == '[', max, s[^1] == ']');
    }

    public bool Contains(Version version)
    {
        var v = Normalize(version);
        if (Min is not null && (MinInclusive ? v < Min : v <= Min))
            return false;
        if (Max is not null && (MaxInclusive ? v > Max : v >= Max))
            return false;
        return true;
    }

    private static Version ParseVersion(string part, string text) =>
        Version.TryParse(part, out var v) ? Normalize(v)
            : Version.TryParse(part + ".0", out v) ? Normalize(v)
            : throw new FormatException($"Not a version: '{part}' in '{text}'.");

    private static Version Normalize(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
}
