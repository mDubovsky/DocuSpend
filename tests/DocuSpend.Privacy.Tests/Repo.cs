namespace DocuSpend.Privacy.Tests;

/// <summary>The repository the tests run in, and the files the privacy checks look at.</summary>
public static class Repo
{
    public static readonly string Root = FindRoot();

    public static readonly string PrivacyDir = Path.Combine(Root, "privacy");

    public static string PathOf(string relative) => Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Every file in the working tree, as repository-relative paths with forward slashes, skipping build output,
    /// git's own data, and samples/private (real statements live there and are never looked at).
    /// </summary>
    public static IEnumerable<string> Files()
    {
        var pending = new Stack<DirectoryInfo>([new DirectoryInfo(Root)]);
        while (pending.TryPop(out var dir))
        {
            foreach (var file in dir.EnumerateFiles())
                yield return Relative(file.FullName);
            foreach (var sub in dir.EnumerateDirectories())
            {
                if (!SkippedDirs.Contains(sub.Name) && Relative(sub.FullName) != "samples/private")
                    pending.Push(sub);
            }
        }
    }

    private static readonly HashSet<string> SkippedDirs = new(StringComparer.OrdinalIgnoreCase)
        { ".git", ".vs", "bin", "obj", "TestResults", "node_modules" };

    private static string Relative(string fullPath) => Path.GetRelativePath(Root, fullPath).Replace('\\', '/');

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return dir.FullName;
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
