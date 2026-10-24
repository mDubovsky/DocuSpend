using Avalonia.Headless;
using Avalonia.Threading;
using DocuSpend.Categorization.Model;
using DocuSpend.Desktop;
using DocuSpend.Presentation;

/// <summary>The states of the screens the spec asks to see, each set up through the view models, as a user would.</summary>
internal static class Screens
{
    /// <summary>A name for the file, and how to get the window into that state.</summary>
    public sealed record Screen(string Name, Action<MainViewModel> Arrange);

    public static IReadOnlyList<Screen> All { get; } =
    [
        new("01-statement", main => main.Statement.SelectedCategory = main.Statement.Categories.FirstOrDefault()?.Category),
        new("02-busy-month", main =>
        {
            main.SelectedSample = main.Samples.Single(s => s.Name == "02-busy-month");
            main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        }),
        new("03-empty-month", main => main.SelectedSample = main.Samples.Single(s => s.Name == "04-empty-month")),
        new("04-rule-error", main =>
        {
            main.RuleForm.SelectedKind = ConditionKind.Amount;
            main.RuleForm.AmountFrom = "dvanásť";
            main.RuleForm.SelectedCategory = main.RuleForm.Categories.Single(c => c.Id == new CategoryId("food"));
            main.RuleForm.SelectedSubcategory = main.RuleForm.Subcategories[0];
            main.AddRuleCommand.Execute(null);
        }),
        new("05-rule-added", main =>
        {
            main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
            main.RuleForm.SelectedKind = ConditionKind.Text;
            main.RuleForm.Text = main.Statement.Transactions[0].Description;
            main.RuleForm.SelectedCategory = main.RuleForm.Categories.Single(c => c.Id == new CategoryId("leisure"));
            main.RuleForm.SelectedSubcategory = main.RuleForm.Subcategories[0];
            main.AddRuleCommand.Execute(null);
        }),
    ];

    /// <summary>Opens a fresh window in the state, renders it, and saves "&lt;name&gt;-&lt;look&gt;.png".</summary>
    public static string Capture(Screen screen, string look, string output)
    {
        var rules = Path.Combine(Path.GetTempPath(), "docuspend-screenshots-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var model = new MainViewModel(SampleCatalog.Available(SamplesFolder()), new SpikeRuleStore(rules));
            // Tall enough that nothing needs scrolling, so the whole window is in one picture.
            var window = new MainWindow(model) { Width = 1280, Height = 1340 };
            window.Show();
            screen.Arrange(model);
            Settle();

            var path = Path.Combine(output, $"{screen.Name}-{look}.png");
            window.CaptureRenderedFrame()!.Save(path);
            window.Close();
            return path;
        }
        finally
        {
            File.Delete(rules);
        }
    }

    /// <summary>Lets layout, bindings and the charts' own updates finish before the picture is taken.</summary>
    private static void Settle()
    {
        for (var i = 0; i < 10; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(30);
        }
    }

    /// <summary>The samples copied next to the program, or the repository's when run from a build folder without them.</summary>
    private static string SamplesFolder()
    {
        var beside = Path.Combine(AppContext.BaseDirectory, "samples");
        if (Directory.Exists(beside))
            return beside;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return Path.Combine(dir.FullName, "samples", "public");
        }
        throw new InvalidOperationException("No samples found.");
    }
}
