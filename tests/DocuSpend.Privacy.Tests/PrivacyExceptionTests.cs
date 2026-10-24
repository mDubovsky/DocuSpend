using System.Text.Json;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// Exceptions to the networking ban are explicit and limited (spec: privacy-no-networking-code, exceptions):
/// the project setting and the list agree, and an excepted project can never reach statement code.
/// </summary>
[Trait("Check", "privacy-no-networking-code")]
public class PrivacyExceptionTests
{
    private const string Decision = "docs/decisions/0006-privacy-approach-and-network-boundary.md";
    private const string Online = "src/DocuSpend.RulesOnline/DocuSpend.RulesOnline.csproj";

    private static string Project(bool exception = false, params string[] references) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>{(exception ? "<PrivacyException>true</PrivacyException>" : "")}</PropertyGroup>
          <ItemGroup>{string.Concat(references.Select(r => $"<ProjectReference Include=\"{r}\" />"))}</ItemGroup>
        </Project>
        """;

    private static Dictionary<string, string> Repository(string onlineProject) => new()
    {
        ["src/DocuSpend.Statements/DocuSpend.Statements.csproj"] = Project(),
        ["src/DocuSpend.Categorization/DocuSpend.Categorization.csproj"] = Project(false, @"..\DocuSpend.Statements\DocuSpend.Statements.csproj"),
        ["src/DocuSpend.RulePacks/DocuSpend.RulePacks.csproj"] = Project(),
        [Online] = onlineProject,
    };

    private static readonly PrivacyException Entry = new(Online, "Shares rule packs when the user asks.", Decision);

    private static IReadOnlyList<string> Check(Dictionary<string, string> projects, params PrivacyException[] entries) =>
        PrivacyExceptions.Check(projects, entries, path => path == Decision);

    [Fact]
    public void No_exceptions_pass()
    {
        Assert.Empty(Check(Repository(Project())));
    }

    [Fact]
    public void A_listed_exception_that_reaches_only_its_own_types_passes()
    {
        Assert.Empty(Check(Repository(Project(true, @"..\DocuSpend.RulePacks\DocuSpend.RulePacks.csproj")), Entry));
    }

    [Fact]
    public void A_project_that_sets_the_exception_without_an_entry_fails()
    {
        var problem = Assert.Single(Check(Repository(Project(true))));

        Assert.Equal($"{Online} sets PrivacyException but has no entry in privacy/exceptions.json.", problem);
    }

    [Fact]
    public void A_conditional_exception_still_counts()
    {
        var conditional = Project().Replace("<PropertyGroup>",
            "<PropertyGroup Condition=\"'$(CI)' != 'true'\"><PrivacyException>true</PrivacyException>");

        Assert.Single(Check(Repository(conditional)));
    }

    [Fact]
    public void An_entry_for_a_project_that_does_not_set_the_exception_fails()
    {
        var problem = Assert.Single(Check(Repository(Project()), Entry));

        Assert.Contains("doesn't set PrivacyException", problem);
    }

    [Fact]
    public void An_entry_for_a_missing_project_fails()
    {
        var problem = Assert.Single(Check(Repository(Project()), Entry with { Project = "src/Gone/Gone.csproj" }));

        Assert.Contains("doesn't exist", problem);
    }

    [Fact]
    public void An_entry_listed_twice_fails()
    {
        Assert.Contains(Check(Repository(Project(true)), Entry, Entry), p => p.Contains("more than once"));
    }

    [Theory]
    [InlineData("", Decision)]
    [InlineData("   ", Decision)]
    [InlineData("A reason.", "")]
    [InlineData("A reason.", "docs/decisions/0099-not-written-yet.md")]
    [InlineData("A reason.", "README.md")]
    public void An_entry_needs_a_reason_and_an_existing_decision(string reason, string decision)
    {
        var problem = Assert.Single(Check(Repository(Project(true)), Entry with { Reason = reason, Decision = decision }));

        Assert.StartsWith($"The exception for {Online}", problem);
    }

    [Fact]
    public void An_excepted_project_that_references_the_statement_code_fails()
    {
        var problems = Check(Repository(Project(true, @"..\DocuSpend.Statements\DocuSpend.Statements.csproj")), Entry);

        var problem = Assert.Single(problems);
        Assert.Contains($"{Online} → src/DocuSpend.Statements/DocuSpend.Statements.csproj", problem);
        Assert.Contains("Decision 0006", problem);
    }

    [Fact]
    public void An_excepted_project_that_reaches_the_statement_code_through_another_project_fails()
    {
        var problems = Check(Repository(Project(true, "../DocuSpend.Categorization/DocuSpend.Categorization.csproj")), Entry);

        // Both statement projects are reachable: categorization directly, statements through it.
        Assert.Equal(2, problems.Count);
        Assert.Contains(problems, p => p.Contains(
            $"{Online} → src/DocuSpend.Categorization/DocuSpend.Categorization.csproj → src/DocuSpend.Statements/DocuSpend.Statements.csproj"));
    }

    [Fact]
    public void The_statement_code_itself_can_never_be_excepted()
    {
        var projects = Repository(Project());
        projects["src/DocuSpend.Statements/DocuSpend.Statements.csproj"] = Project(true);
        var entry = Entry with { Project = "src/DocuSpend.Statements/DocuSpend.Statements.csproj" };

        Assert.Contains(Check(projects, entry), p => p.Contains("can reach the statement code"));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "exceptions": [ { "reason": "x", "decision": "y" } ] }""")]
    [InlineData("""{ "exceptions": [], "allowAll": true }""")]
    [InlineData("not json")]
    public void A_malformed_exceptions_file_fails(string json)
    {
        Assert.ThrowsAny<JsonException>(() => PrivacyExceptions.Parse(json));
    }

    [Fact]
    public void The_repository_has_no_exceptions_and_its_projects_agree_with_the_list()
    {
        var entries = PrivacyExceptions.Parse(File.ReadAllText(Path.Combine(Repo.PrivacyDir, "exceptions.json")));
        var projects = Repo.Files()
            .Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(f => f, f => File.ReadAllText(Repo.PathOf(f)));

        Assert.Empty(entries);
        Assert.Empty(PrivacyExceptions.Check(projects, entries, path => File.Exists(Repo.PathOf(path))));
        // The reachability rule names the statement projects by path; a rename must not quietly void it.
        Assert.All(PrivacyExceptions.StatementProjects, p => Assert.Contains(p, projects.Keys));
        Assert.True(projects.Count >= 7, "Expected to find every project in the repository.");
    }
}
