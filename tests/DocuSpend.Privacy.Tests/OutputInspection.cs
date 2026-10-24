namespace DocuSpend.Privacy.Tests;

/// <summary>A shipped assembly that uses networking, and how our own assembly leads to it.</summary>
public sealed record NetworkingFinding(string File, string Assembly, Version Version, IReadOnlyList<string> Uses,
    IReadOnlyList<string>? Path)
{
    /// <summary>"DocuSpend.X → Lib → Net", or a note that nothing in the output references the assembly.</summary>
    public string PathText => Path is null
        ? "in the output, but not referenced by anything in it (if it is left over from an earlier build, run dotnet clean)"
        : string.Join(" → ", Path);

    public override string ToString() =>
        $"{Assembly} {Version} ({File}) uses networking: {string.Join(", ", Uses)}. Path: {PathText}.";
}

/// <summary>Everything in one project's build output, as the dependency check sees it.</summary>
public sealed record OutputReport(string Root, IReadOnlyList<string> ManagedFiles, IReadOnlyList<string> NativeFiles,
    IReadOnlyList<NetworkingFinding> Findings);

/// <summary>
/// Inspects every file in a build output directory. Managed assemblies are read for networking; anything else that
/// could be executable code (native libraries, or a file that can't be read as an assembly) is listed as native,
/// because its behavior can't be inspected. The .NET launcher for the project itself is not listed.
/// </summary>
public static class OutputInspection
{
    /// <param name="ownPrograms">
    /// The repository's own project names. A program that references another of ours (the screenshot tool references
    /// the desktop application) gets that program's launcher copied along; it is skipped like the root's own launcher,
    /// but only when that project's assembly is in the output too.
    /// </param>
    public static OutputReport Inspect(string outputDir, string rootAssembly, NetworkingList networking,
        IReadOnlyCollection<string>? ownPrograms = null)
    {
        if (!Directory.Exists(outputDir))
            throw new DirectoryNotFoundException($"No build output at {outputDir}. Build the solution first (dotnet build).");

        var files = Directory.EnumerateFiles(outputDir, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .Select(path => (Path: path, File: System.IO.Path.GetRelativePath(outputDir, path).Replace('\\', '/')))
            .ToList();
        var present = files.Select(f => f.File).ToHashSet(StringComparer.Ordinal);
        var launchers = (ownPrograms ?? []).Where(p => present.Contains(p + ".dll")).Append(rootAssembly).ToList();

        var managed = new List<(string File, InspectedAssembly Assembly)>();
        var native = new List<string>();
        foreach (var (path, file) in files)
        {
            if (!MayBeCode(file) || launchers.Any(l => IsLauncher(file, l)))
                continue;
            if (AssemblyInspector.Inspect(File.ReadAllBytes(path), networking) is { } assembly)
                managed.Add((file, assembly));
            else
                native.Add(file);
        }

        if (!managed.Any(m => m.Assembly.Name == rootAssembly))
            throw new InvalidOperationException($"{rootAssembly} is not in its own build output at {outputDir}.");

        var paths = ShortestPaths(rootAssembly, managed.Select(m => m.Assembly).ToList());
        var findings = managed
            .Where(m => m.Assembly.NetworkingUses.Count > 0)
            .Select(m => new NetworkingFinding(m.File, m.Assembly.Name, m.Assembly.Version, m.Assembly.NetworkingUses,
                paths.GetValueOrDefault(m.Assembly.Name)))
            .ToList();
        return new OutputReport(rootAssembly, managed.Select(m => m.File).ToList(), native, findings);
    }

    /// <summary>Files that hold code: assemblies and native libraries, including versioned .so files.</summary>
    private static bool MayBeCode(string file)
    {
        var name = file[(file.LastIndexOf('/') + 1)..].ToLowerInvariant();
        return name.EndsWith(".dll", StringComparison.Ordinal) || name.EndsWith(".exe", StringComparison.Ordinal)
            || name.EndsWith(".dylib", StringComparison.Ordinal) || name.EndsWith(".so", StringComparison.Ordinal)
            || name.Contains(".so.", StringComparison.Ordinal) || file.Contains("/native/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The SDK's launcher (apphost) for an executable project: DocuSpend.X.exe, or DocuSpend.X on Linux and macOS, also
    /// in a platform folder such as win-x64/ that a platform-specific build leaves in the output.
    /// </summary>
    private static bool IsLauncher(string file, string rootAssembly)
    {
        var name = file[(file.LastIndexOf('/') + 1)..];
        return name == rootAssembly + ".exe" || name == rootAssembly;
    }

    /// <summary>For each assembly reachable from the root, the shortest chain of references that leads to it.</summary>
    private static Dictionary<string, IReadOnlyList<string>> ShortestPaths(string root, IReadOnlyList<InspectedAssembly> assemblies)
    {
        var byName = assemblies.GroupBy(a => a.Name).ToDictionary(g => g.Key, g => g.SelectMany(a => a.References).Distinct().ToList());
        var cameFrom = new Dictionary<string, string?> { [root] = null };
        var queue = new Queue<string>([root]);
        while (queue.TryDequeue(out var current))
        {
            foreach (var next in byName[current].Where(byName.ContainsKey).Where(n => cameFrom.TryAdd(n, current)))
                queue.Enqueue(next);
        }

        return cameFrom.Keys.ToDictionary(name => name, name =>
        {
            var path = new List<string>();
            for (string? at = name; at is not null; at = cameFrom[at])
                path.Insert(0, at);
            return (IReadOnlyList<string>)path;
        });
    }
}
