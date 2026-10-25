using Avalonia.Headless;
using Avalonia.Threading;
using DocuSpend.Categorization.Model;
using DocuSpend.Desktop;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

/// <summary>The states of the screens the specs ask to see, each set up through the view models, as a user would.</summary>
internal static class Screens
{
    /// <summary>A name for the file, and how to get the window into that state.</summary>
    public sealed record Screen(string Name, Action<MainViewModel> Arrange);

    public static IReadOnlyList<Screen> All { get; } =
    [
        new("00-start", _ => { }),
        new("00-start-message", main => Open(main, "06-other-bank-statement")),
        new("00-start-dragging", main => main.IsDraggingOver = true),
        new("01-statement", main =>
        {
            Wait(main.OpenExampleCommand.ExecuteAsync(null));
            main.Statement.SelectedCategory = main.Statement.Categories.FirstOrDefault()?.Category;
        }),
        new("02-busy-month", main =>
        {
            Open(main, "02-busy-month");
            main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        }),
        new("02-busy-month-message", main =>
        {
            Open(main, "02-busy-month");
            Open(main, "07-not-a-statement");
        }),
        new("03-empty-month", main => Open(main, "04-empty-month")),
        new("04-rule-error", main =>
        {
            Open(main, "01-typical-month");
            main.RuleForm.SelectedKind = ConditionKind.Amount;
            main.RuleForm.AmountFrom = "dvanásť";
            main.RuleForm.SelectedCategory = main.RuleForm.Categories.Single(c => c.Id == new CategoryId("food"));
            main.RuleForm.SelectedSubcategory = main.RuleForm.Subcategories[0];
            main.AddRuleCommand.Execute(null);
        }),
        new("05-rule-added", main =>
        {
            Open(main, "01-typical-month");
            main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
            main.RuleForm.SelectedKind = ConditionKind.Text;
            main.RuleForm.Text = main.Statement.Transactions[0].Description;
            main.RuleForm.SelectedCategory = main.RuleForm.Categories.Single(c => c.Id == new CategoryId("leisure"));
            main.RuleForm.SelectedSubcategory = main.RuleForm.Subcategories[0];
            main.AddRuleCommand.Execute(null);
        }),
    ];

    /// <summary>Opens a fresh window in the state, renders it, and saves "&lt;name&gt;-&lt;look&gt;.png".</summary>
    public static string Capture(Screen screen, LookChoice choice, string look, string output)
    {
        var rules = Path.Combine(Path.GetTempPath(), "docuspend-screenshots-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var model = new MainViewModel(new SpikeRuleStore(rules), example: SamplePath("01-typical-month"));
            model.SelectedLook = model.Looks.Single(l => l.Choice == choice);
            // Tall enough that nothing needs scrolling, so the whole window is in one picture.
            var window = new MainWindow(model) { Width = 1280, Height = 1660 };
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

    /// <summary>Opens a sample from the repository, as the open window would.</summary>
    private static void Open(MainViewModel main, string sample) => Wait(main.OpenFileAsync(SamplePath(sample)));

    /// <summary>Waits for an opening to finish while letting the UI thread run what it queues.</summary>
    private static void Wait(Task task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
        task.GetAwaiter().GetResult();
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

    /// <summary>A sample in the repository's samples/public; only the example travels with the program.</summary>
    private static string SamplePath(string name)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return Path.Combine(dir.FullName, "samples", "public", name + ".pdf");
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
