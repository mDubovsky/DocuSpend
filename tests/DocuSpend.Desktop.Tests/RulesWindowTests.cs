using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// Keeping the rules, in the real window (spec: when the saved rules can't be used; acceptance criteria). What the
/// store and the view model do is tested in DocuSpend.Presentation.Tests; these catch what only the window shows.
/// </summary>
public sealed class RulesWindowTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("docuspend-rules-window-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string RulesFile => Path.Combine(_folder, "rules.json");

    private (MainWindow Window, MainViewModel Model) Start()
    {
        var model = new MainViewModel(new RuleStore(RulesFile), example: MainWindowTests.Sample("01-typical-month"));
        var window = new MainWindow(model);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, model);
    }

    private static Border Named(Window window, string name) => window.FindControl<Border>(name)!;

    private static string? TextIn(Control control) =>
        control.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).FirstOrDefault(t => !string.IsNullOrEmpty(t));

    private static bool Lists(Window window, string text) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == text && t.IsEffectivelyVisible);

    [Fact]
    public void A_damaged_file_is_reported_on_the_start_screen_and_the_message_can_be_closed() => Headless.Run(() =>
    {
        File.WriteAllText(RulesFile, "{ damaged");
        var (window, model) = Start();

        var message = Named(window, "RulesMessage");
        Assert.False(model.HasStatement);
        Assert.True(message.IsEffectivelyVisible);
        Assert.StartsWith("Uložené pravidlá boli poškodené", TextIn(message));
        Assert.Contains(_folder, TextIn(message));

        message.GetLogicalDescendants().OfType<Button>().Single().Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(message.IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void The_rules_message_stays_over_a_statement_beside_the_message_about_a_file() => Headless.Run(() =>
    {
        File.WriteAllText(RulesFile, "{ damaged");
        var (window, model) = Start();

        Headless.Wait(model.OpenExampleCommand.ExecuteAsync(null));
        Headless.Wait(model.OpenFileAsync(MainWindowTests.Sample("07-not-a-statement")));

        Assert.True(model.HasStatement);
        Assert.True(Named(window, "RulesMessage").IsEffectivelyVisible);
        Assert.True(Named(window, "OpenMessage").IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void While_saving_is_off_my_rules_say_so() => Headless.Run(() =>
    {
        File.WriteAllText(RulesFile, """{ "version": 2 }""");
        var (window, model) = Start();
        Headless.Wait(model.OpenExampleCommand.ExecuteAsync(null));

        var line = Named(window, "RulesNotSaving");
        Assert.True(line.IsEffectivelyVisible);
        Assert.Equal(Texts.RulesNotSaving, TextIn(line));
        window.Close();
    });

    [Fact]
    public void Without_a_problem_neither_the_message_nor_the_line_shows() => Headless.Run(() =>
    {
        var (window, model) = Start();
        Headless.Wait(model.OpenExampleCommand.ExecuteAsync(null));

        Assert.False(Named(window, "RulesMessage").IsEffectivelyVisible);
        Assert.False(Named(window, "RulesNotSaving").IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public void A_rule_added_in_one_window_is_listed_in_a_new_window_started_on_the_same_folder() => Headless.Run(() =>
    {
        var (first, model) = Start();
        Headless.Wait(model.OpenExampleCommand.ExecuteAsync(null));
        model.RuleForm.SelectedKind = ConditionKind.Text;
        model.RuleForm.Text = "Pekáreň";
        model.RuleForm.SelectedCategory = model.RuleForm.Categories.Single(c => c.Id == new CategoryId("food"));
        model.RuleForm.SelectedSubcategory = model.RuleForm.Subcategories[0];
        model.AddRuleCommand.Execute(null);
        var placements = model.Statement.Categorized!.Transactions.Select(t => t.Placement).ToList();
        first.Close();

        var (second, restarted) = Start();
        Headless.Wait(restarted.OpenExampleCommand.ExecuteAsync(null));
        Dispatcher.UIThread.RunJobs();

        Assert.True(Lists(second, "Text obsahuje „Pekáreň“"));
        Assert.Equal(placements, restarted.Statement.Categorized!.Transactions.Select(t => t.Placement));
        second.Close();
    });
}
