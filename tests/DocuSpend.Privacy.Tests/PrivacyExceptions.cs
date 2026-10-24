using System.Text.Json;
using System.Xml.Linq;

namespace DocuSpend.Privacy.Tests;

/// <summary>One entry in privacy/exceptions.json: a project allowed to use networking, and why.</summary>
public sealed record PrivacyException(string Project, string Reason, string Decision);

/// <summary>
/// The rules for exceptions to the networking ban (decision 0006): the projects that set PrivacyException and
/// the entries in privacy/exceptions.json agree exactly, every entry gives a reason and an existing decision,
/// and no excepted project can reach the code that sees statements, directly or through other projects.
/// </summary>
public static class PrivacyExceptions
{
    /// <summary>The projects that define statements, transactions and their categories.</summary>
    public static readonly IReadOnlyList<string> StatementProjects =
    [
        "src/DocuSpend.Statements/DocuSpend.Statements.csproj",
        "src/DocuSpend.Categorization/DocuSpend.Categorization.csproj",
    ];

    public static IReadOnlyList<PrivacyException> Parse(string json)
    {
        var file = JsonSerializer.Deserialize<ExceptionsFile>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        });
        var entries = file?.Exceptions ?? throw new JsonException("privacy/exceptions.json must have an \"exceptions\" list.");
        if (entries.Any(e => string.IsNullOrWhiteSpace(e.Project)))
            throw new JsonException("Every entry in privacy/exceptions.json must name its project.");
        return entries;
    }

    /// <summary>Whether a project file sets PrivacyException to true anywhere, even under a condition.</summary>
    public static bool DeclaresException(string projectXml) =>
        XDocument.Parse(projectXml).Descendants().Any(e => e.Name.LocalName == "PrivacyException"
            && string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));

    /// <summary>The projects a project file references, as repository-relative paths with forward slashes.</summary>
    public static IEnumerable<string> References(string projectPath, string projectXml)
    {
        var dir = projectPath.Contains('/') ? projectPath[..projectPath.LastIndexOf('/')] : "";
        return XDocument.Parse(projectXml).Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .OfType<string>()
            .Select(include => Normalize(dir, include));
    }

    /// <summary>
    /// Problems with the exceptions, given every project file (repository-relative path → XML), the parsed entries,
    /// and a way to tell whether a repository file exists.
    /// </summary>
    public static IReadOnlyList<string> Check(IReadOnlyDictionary<string, string> projects,
        IReadOnlyList<PrivacyException> entries, Func<string, bool> fileExists)
    {
        var problems = new List<string>();
        var declared = projects.Where(p => DeclaresException(p.Value)).Select(p => p.Key).ToHashSet();
        var listed = entries.Select(e => e.Project).ToList();

        foreach (var project in declared.Where(p => !listed.Contains(p)).Order(StringComparer.Ordinal))
            problems.Add($"{project} sets PrivacyException but has no entry in privacy/exceptions.json.");
        foreach (var duplicate in listed.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key))
            problems.Add($"privacy/exceptions.json lists {duplicate} more than once.");

        foreach (var entry in entries)
        {
            if (!projects.ContainsKey(entry.Project))
                problems.Add($"privacy/exceptions.json lists {entry.Project}, which doesn't exist.");
            else if (!declared.Contains(entry.Project))
                problems.Add($"privacy/exceptions.json lists {entry.Project}, which doesn't set PrivacyException.");
            if (string.IsNullOrWhiteSpace(entry.Reason))
                problems.Add($"The exception for {entry.Project} gives no reason.");
            if (string.IsNullOrWhiteSpace(entry.Decision)
                || !entry.Decision.StartsWith("docs/decisions/", StringComparison.Ordinal) || !fileExists(entry.Decision))
                problems.Add($"The exception for {entry.Project} must name an existing decision in docs/decisions/.");
        }

        foreach (var project in declared.Order(StringComparer.Ordinal))
        {
            foreach (var path in PathsToStatementProjects(project, projects))
                problems.Add($"{project} has a privacy exception but can reach the statement code: {string.Join(" → ", path)}. "
                    + "Decision 0006: the networking module must never be able to see a statement.");
        }
        return problems;
    }

    /// <summary>Every shortest reference path from a project to a statement project, through any projects in between.</summary>
    private static IEnumerable<IReadOnlyList<string>> PathsToStatementProjects(string start,
        IReadOnlyDictionary<string, string> projects)
    {
        var cameFrom = new Dictionary<string, string?> { [start] = null };
        var queue = new Queue<string>([start]);
        while (queue.TryDequeue(out var current))
        {
            if (!projects.TryGetValue(current, out var xml))
                continue;
            foreach (var next in References(current, xml).Where(n => cameFrom.TryAdd(n, current)))
                queue.Enqueue(next);
        }

        foreach (var target in StatementProjects.Where(cameFrom.ContainsKey))
        {
            var path = new List<string>();
            for (string? at = target; at is not null; at = cameFrom[at])
                path.Insert(0, at);
            yield return path;
        }
    }

    private static string Normalize(string dir, string include)
    {
        var parts = new List<string>(dir.Length == 0 ? [] : dir.Split('/'));
        foreach (var part in include.Replace('\\', '/').Split('/'))
        {
            if (part == "..")
                parts.RemoveAt(parts.Count - 1);
            else if (part is not ("." or ""))
                parts.Add(part);
        }
        return string.Join('/', parts);
    }

    private sealed record ExceptionsFile(List<PrivacyException>? Exceptions);
}
