namespace DocuSpend.Privacy.Tests;

/// <summary>What the dependency check concluded about one project's build output.</summary>
public sealed record DependencyCheckResult(
    IReadOnlyList<string> Failures,
    IReadOnlyList<string> Reported,
    IReadOnlySet<AllowedDependency> UsedAllowances,
    IReadOnlySet<string> UsedNativeEntries);

/// <summary>
/// privacy-dependencies (decision 0006): nothing that ships brings networking, telemetry or unreviewed native code.
/// A networking finding passes only if privacy/allowed-dependencies.json covers that assembly and version, and is
/// still reported; a denied package fails wherever it is in the tree; a native file passes only if
/// privacy/native-libraries.json lists it, and is still reported, because native code can't be inspected.
/// </summary>
public static class DependencyCheck
{
    private const string Rule = "privacy-dependencies";
    private const string Decision = "decision 0006, docs/decisions/0006-privacy-approach-and-network-boundary.md";

    public static DependencyCheckResult Evaluate(
        OutputReport output,
        IReadOnlyList<AllowedDependency> allowed,
        IReadOnlyList<ReviewedNativeLibrary> nativeLibraries,
        IReadOnlyList<ResolvedPackage> packages,
        DeniedPackages denied)
    {
        var failures = new List<string>();
        var reported = new List<string>();
        var usedAllowances = new HashSet<AllowedDependency>();
        var usedNative = new HashSet<string>(StringComparer.Ordinal);

        foreach (var finding in output.Findings)
        {
            if (allowed.FirstOrDefault(a => a.Allows(finding)) is { } allowance)
            {
                usedAllowances.Add(allowance);
                reported.Add($"{output.Root}: allowed by review ({allowance.Reviewer}): {finding} Reason: {allowance.Reason}");
            }
            else
            {
                failures.Add($"{Rule}: {output.Root} ships {finding} No shipped dependency may use the network ({Decision}). "
                    + "Remove the dependency, or, if the use is never reached, ask for a review: a reviewer adds the assembly "
                    + "and version range to privacy/allowed-dependencies.json with the reason. See privacy/README.md.");
            }
        }

        foreach (var package in packages)
        {
            if (denied.Denies(package.Id) is { } pattern)
                failures.Add($"{Rule}: {output.Root} depends on {package.Id} {package.Version}, which privacy/denied-packages.txt "
                    + $"denies ('{pattern}'): telemetry, analytics, crash reporting and auto-update are not allowed ({Decision}). "
                    + $"Path: {package.PathText}. Remove it, or the package that brings it in. See privacy/README.md.");
        }

        foreach (var file in output.NativeFiles)
        {
            if (nativeLibraries.FirstOrDefault(n => n.File == file) is { } entry)
            {
                usedNative.Add(entry.File);
                reported.Add($"{output.Root}: native file {file}, reviewed ({entry.Reviewer}): {entry.Reason}");
            }
            else
            {
                failures.Add($"{Rule}: {output.Root} ships the native file {file}, whose behavior can't be inspected. "
                    + $"A reviewer must list it in privacy/native-libraries.json with the reason it is acceptable ({Decision}). "
                    + "See privacy/README.md.");
            }
        }

        return new DependencyCheckResult(failures, reported, usedAllowances, usedNative);
    }

    /// <summary>
    /// Package files the output should contain but doesn't: without them the inspection would be incomplete. A project
    /// may remove a package file from what it ships, declared with PrivacyRemovedFromOutput in its project file (see
    /// Directory.Build.targets); that file must then really be gone, and some package must still provide it.
    /// </summary>
    public static IReadOnlyList<string> MissingFromOutput(string root, IReadOnlyList<string> expected,
        IReadOnlyCollection<string> present, IReadOnlyCollection<string>? removed = null)
    {
        removed ??= [];
        var missing = expected.Where(f => !present.Contains(f) && !removed.Contains(f))
            .Select(f => $"{Rule}: {root}'s build output lacks {f}, which its packages provide, so the inspection would miss it. "
                + "Every project must copy its packages to its output (CopyLocalLockFileAssemblies, set in Directory.Build.props).");
        var stillThere = removed.Where(present.Contains)
            .Select(f => $"{Rule}: {root} declares {f} as removed from its output (PrivacyRemovedFromOutput), but it is still there. "
                + "See privacy/README.md.");
        var stale = removed.Where(f => !expected.Contains(f))
            .Select(f => $"{Rule}: {root} declares {f} as removed from its output (PrivacyRemovedFromOutput), but no package provides it "
                + "any more. Remove the declaration. See privacy/README.md.");
        return [.. missing, .. stillThere, .. stale];
    }
}
