using LibGit2Sharp;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// The files git tracks, read from the repository's index with LibGit2Sharp, so no git process is started (the ban
/// forbids starting programs, tests included). The index also holds files staged but not yet committed, so the check
/// catches them before the commit.
/// </summary>
public static class TrackedFiles
{
    public static IReadOnlyList<string> Read(string repositoryRoot)
    {
        var gitDir = Repository.Discover(repositoryRoot)
            ?? throw new InvalidOperationException($"{repositoryRoot} is not inside a git repository, so the tracked files can't be checked.");

        // Only the index is read. On CI the checkout may belong to another user, which git would otherwise refuse.
        GlobalSettings.SetOwnerValidation(false);
        using var repository = new Repository(gitDir);
        return repository.Index.Select(entry => entry.Path.Replace('\\', '/')).ToList();
    }
}
