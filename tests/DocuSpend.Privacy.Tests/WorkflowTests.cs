using System.Text.RegularExpressions;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// The pipeline keeps the promises the checks depend on: every check runs under its stable name, nothing is allowed
/// to fail quietly, and third-party actions are pinned (plan: the pipeline).
/// </summary>
public partial class WorkflowTests
{
    /// <summary>
    /// The check names from the spec. They are an interface: a check marked as required for merging is found by its
    /// name. secret-scanning is a repository setting, not a workflow, so it has no job.
    /// </summary>
    public static readonly IReadOnlyList<string> CheckNames =
    [
        "build-and-test", "privacy-no-networking-code", "privacy-dependencies", "privacy-no-network-run",
        "privacy-no-real-data", "code-scanning", "dependency-vulnerabilities", "secret-scanning", "release-provenance",
    ];

    public static readonly IReadOnlyList<string> RepositorySettings = ["secret-scanning"];

    private static readonly string WorkflowDir = Repo.PathOf(".github/workflows");

    private static IEnumerable<(string File, string Text)> Workflows() =>
        Directory.EnumerateFiles(WorkflowDir, "*.yml").Order(StringComparer.Ordinal)
            .Select(f => (Path.GetFileName(f), File.ReadAllText(f).ReplaceLineEndings("\n")));

    /// <summary>Job names: "name:" at the indentation of a job's own keys.</summary>
    private static IEnumerable<string> JobNames(string workflow) =>
        JobName().Matches(workflow).Select(m => m.Groups["name"].Value.Trim().Trim('"'));

    public static TheoryData<string> WorkflowChecks => new(CheckNames.Except(RepositorySettings));

    [Theory]
    [MemberData(nameof(WorkflowChecks))]
    public void Every_check_runs_under_its_own_name(string check)
    {
        // A matrix job's name carries its platform, as in "build-and-test (windows)".
        var jobs = Workflows().SelectMany(w => JobNames(w.Text)).ToList();

        Assert.Contains(jobs, job => job == check || job.StartsWith(check + " (", StringComparison.Ordinal));
    }

    [Fact]
    public void The_check_names_are_found_in_the_workflows()
    {
        // Guards the job-name reader: if it found nothing, a renamed check could slip through.
        var jobs = Workflows().SelectMany(w => JobNames(w.Text)).ToList();

        Assert.Contains("build-and-test (${{ matrix.platform }})", jobs);
        Assert.Contains("privacy-no-network-run", jobs);
        Assert.Contains("release-provenance", jobs);
    }

    [Fact]
    public void No_step_is_allowed_to_fail_quietly()
    {
        Assert.All(Workflows(), w => Assert.DoesNotMatch(ContinueOnError(), w.Text));
    }

    [Fact]
    public void Every_action_is_pinned_to_a_full_commit()
    {
        var uses = Workflows().SelectMany(w => Uses().Matches(w.Text).Select(m => (w.File, Line: m.Value.Trim()))).ToList();

        Assert.NotEmpty(uses);
        Assert.All(uses, u => Assert.Matches(PinnedUse(), u.Line));
    }

    [Fact]
    public void Every_workflow_sets_its_permissions()
    {
        Assert.All(Workflows(), w => Assert.Matches(TopLevelPermissions(), w.Text));
    }

    [Fact]
    public void No_check_in_the_main_workflow_depends_on_another_job()
    {
        // When a needed job fails, the job that needs it is skipped, and a skipped check counts as passed.
        var ci = File.ReadAllText(Path.Combine(WorkflowDir, "ci.yml")).ReplaceLineEndings("\n");

        Assert.DoesNotMatch(Needs(), ci);
    }

    [GeneratedRegex(@"^ {4}name: (?<name>.+)$", RegexOptions.Multiline)]
    private static partial Regex JobName();

    [GeneratedRegex(@"^\s*(- )?uses: .+$", RegexOptions.Multiline)]
    private static partial Regex Uses();

    [GeneratedRegex(@"uses: [\w.-]+/[\w./-]+@[0-9a-f]{40} # v\d+(\.\d+)*$")]
    private static partial Regex PinnedUse();

    [GeneratedRegex(@"^permissions:", RegexOptions.Multiline)]
    private static partial Regex TopLevelPermissions();

    [GeneratedRegex(@"^\s*(- )?continue-on-error:", RegexOptions.Multiline)]
    private static partial Regex ContinueOnError();

    [GeneratedRegex(@"^\s+needs:", RegexOptions.Multiline)]
    private static partial Regex Needs();
}
