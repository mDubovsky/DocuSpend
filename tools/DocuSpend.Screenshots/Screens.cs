using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using DocuSpend.Categorization.Model;
using DocuSpend.Desktop;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

/// <summary>The states of the screens the specs ask to see, each set up through the view models, as a user would.</summary>
internal static class Screens
{
    /// <summary>
    /// A name for the file, and how to get the window into that state. <paramref name="Prepare"/> sets up the rules
    /// file before the window starts, since the window reads it at start; what it returns is disposed once it has.
    /// <paramref name="InWindow"/> does what only the window can, such as opening a tooltip.
    /// </summary>
    public sealed record Screen(
        string Name, Action<MainViewModel> Arrange, Func<string, IDisposable?>? Prepare = null, Action<MainWindow>? InWindow = null);

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
        new("06-rules-damaged", _ => { }, rules =>
        {
            File.WriteAllText(rules, "{ damaged");
            return null;
        }),
        new("07-rules-not-saving", AddRuleOnTheExample, rules =>
        {
            File.WriteAllText(rules, """{ "version": 1, "categories": [], "subcategories": [], "rules": [] }""");
            // Held open with no sharing, as another program might, while the window starts.
            return new FileStream(rules, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }),
        new("08-rule-not-saved", AddRuleOnTheExample, rules =>
        {
            // A file where the folder should be: the folder can't be created, so the save fails.
            Directory.Delete(Path.GetDirectoryName(rules)!);
            File.WriteAllText(Path.GetDirectoryName(rules)!, "");
            return null;
        }),
        new("09-rules-list", AddRulesOfEveryKind),
        new("10-rule-delete-question", main =>
        {
            AddRulesOfEveryKind(main);
            main.AskToDeleteCommand.Execute(main.Rules[1]);
        }),
        new("11-rule-editing", main =>
        {
            AddRulesOfEveryKind(main);
            main.EditRuleCommand.Execute(main.Rules[2]);
        }, InWindow: window => ToolTip.SetIsOpen(window.FindControl<Control>("PriorityHelp")!, true)),
        new("12-built-in-rules", main =>
        {
            AddRulesOfEveryKind(main);
            main.SelectedRuleView = main.RuleViews[1];
        }),
    ];

    /// <summary>Opens the example and adds a rule of each kind through the form, then renumbers one, as a user would.</summary>
    private static void AddRulesOfEveryKind(MainViewModel main)
    {
        Open(main, "01-typical-month");
        var form = main.RuleForm;
        void Add(string target, Action fill)
        {
            fill();
            form.SelectedCategory = form.Categories.Single(c => c.Subcategories.Any(s => s.Id == new SubcategoryId(target)));
            form.SelectedSubcategory = form.Subcategories.Single(s => s.Id == new SubcategoryId(target));
            main.AddRuleCommand.Execute(null);
        }

        Add("food.groceries", () => { form.SelectedKind = ConditionKind.Text; form.Text = "Kaufland"; });
        Add("own-transfers.other", () =>
        {
            form.SelectedKind = ConditionKind.CounterpartyName;
            form.SelectedTextMode = form.TextModes[1];
            form.Text = "Ján Vzorový";
        });
        Add("housing.rent", () => { form.SelectedKind = ConditionKind.Amount; form.AmountFrom = "500"; });
        Add("leisure.games", () =>
        {
            form.SelectedKind = ConditionKind.Type;
            form.Types[0].IsChecked = form.Types[1].IsChecked = true;
        });
        Add("housing.equipment", () => { form.SelectedKind = ConditionKind.VariableSymbol; form.Symbol = "0326"; });
        Add("investments.other", () => { form.SelectedKind = ConditionKind.CounterpartyAccount; form.Account = "SK31 1200 0000 1987 4263 7541"; });

        main.EditRuleCommand.Execute(main.Rules.Single(r => r.Condition.StartsWith("Suma", StringComparison.Ordinal)));
        form.Priority = "3";
        main.SaveRuleCommand.Execute(null);
        form.Message = null;
    }

    private static void AddRuleOnTheExample(MainViewModel main)
    {
        Open(main, "01-typical-month");
        main.Statement.SelectedCategory = new CategoryId("uncategorized-out");
        main.RuleForm.SelectedKind = ConditionKind.Text;
        main.RuleForm.Text = main.Statement.Transactions[0].Description;
        main.RuleForm.SelectedCategory = main.RuleForm.Categories.Single(c => c.Id == new CategoryId("leisure"));
        main.RuleForm.SelectedSubcategory = main.RuleForm.Subcategories[0];
        main.AddRuleCommand.Execute(null);
    }

    /// <summary>Opens a fresh window in the state, renders it, and saves "&lt;name&gt;-&lt;look&gt;.png".</summary>
    public static string Capture(Screen screen, LookChoice choice, string look, string output)
    {
        var folder = Directory.CreateTempSubdirectory("docuspend-screenshots-").FullName;
        var rules = Path.Combine(folder, "DocuSpend", "rules.json");
        Directory.CreateDirectory(Path.GetDirectoryName(rules)!);
        try
        {
            MainViewModel model;
            using (screen.Prepare?.Invoke(rules))
                model = new MainViewModel(new RuleStore(rules), example: SamplePath("01-typical-month"));
            model.SelectedLook = model.Looks.Single(l => l.Choice == choice);
            // Tall enough that nothing needs scrolling, so the whole window is in one picture.
            var window = new MainWindow(model) { Width = 1280, Height = 1660 };
            window.Show();
            screen.Arrange(model);
            Settle();
            if (screen.InWindow is { } inWindow)
            {
                inWindow(window);
                Settle();
            }

            var path = Path.Combine(output, $"{screen.Name}-{look}.png");
            window.CaptureRenderedFrame()!.Save(path);
            window.Close();
            return path;
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
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
