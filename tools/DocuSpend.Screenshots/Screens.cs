using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DocuSpend.Categorization.Model;
using DocuSpend.Desktop;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;
using LiveChartsCore.Drawing;

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
            ShowRules(main);
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
            ShowRules(main);
        }),
        new("05-rule-added-statement", main =>
        {
            AddRuleOnTheExample(main);
        }),
        new("06-rules-damaged", _ => { }, rules =>
        {
            File.WriteAllText(rules, "{ damaged");
            return null;
        }),
        new("07-rules-not-saving", main => { AddRuleOnTheExample(main); ShowRules(main); }, rules =>
        {
            File.WriteAllText(rules, """{ "version": 1, "categories": [], "subcategories": [], "rules": [] }""");
            // Held open with no sharing, as another program might, while the window starts.
            return new FileStream(rules, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }),
        new("08-rule-not-saved", main => { AddRuleOnTheExample(main); ShowRules(main); }, rules =>
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
        // Hovers through the real charts: the pointer is moved onto a slice or a day, and the window finds what is there.
        new("13-ring-card-top", main => Open(main, "02-busy-month"), InWindow: window => HoverRing(window, 280)),
        new("13-ring-card-right", main => Open(main, "02-busy-month"), InWindow: window => HoverRing(window, 10)),
        new("13-ring-card-bottom", main => Open(main, "02-busy-month"), InWindow: window => HoverRing(window, 100)),
        new("13-ring-card-left", main => Open(main, "02-busy-month"), InWindow: window => HoverRing(window, 190)),
        new("13-ring-card-long-name", main => Open(main, "02-busy-month"), InWindow: window => HoverRing(window, MiddleOf(window, "Elektronika"))),
        new("14-balance-card-gap", main => Open(main, "01-typical-month"), InWindow: window => HoverDay(window, DayWithoutBookings(window))),
        new("14-balance-card-busy", main => Open(main, "02-busy-month"), InWindow: window => HoverDay(window, BusiestDay(window))),
        // The switches and the scroll bars (#19): "Podľa systému" is never the chosen look in these pictures.
        new("15-look-switch-hover", main => Open(main, "01-typical-month"), InWindow: window => PointAt(window, Choice(window.LookSwitch, 0))),
        new("16-rules-switch-hover", main => { Open(main, "01-typical-month"); ShowRules(main); }, InWindow: window => PointAt(window, Choice(window.RuleViewSwitch, 1))),
        // The two screens (#21): each at the default window size, and the switch under the pointer.
        new("18-rules-screen", main =>
        {
            AddRulesOfEveryKind(main);
            main.RuleForm.Message = null;
        }),
        new("19-screen-switch-hover", main => Open(main, "01-typical-month"), InWindow: window => PointAt(window, Choice(window.ScreenSwitch, 1))),
        new("17-scroll-bar", main =>
        {
            Open(main, "02-busy-month");
            SelectTheLongestCategory(main);
        }, InWindow: ScrollTransactionsAndPointAtTheHandle),
    ];

    private static void ShowRules(MainViewModel main) => main.SelectedScreen = main.Screens.Single(s => s.Screen == AppScreen.Rules);

    private static ListBoxItem Choice(ListBox @switch, int index) => @switch.GetVisualDescendants().OfType<ListBoxItem>().ElementAt(index);

    private static void PointAt(MainWindow window, Control control) =>
        window.MouseMove(control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value);

    /// <summary>Selects the category with the most transactions, so its list has more than fits.</summary>
    private static void SelectTheLongestCategory(MainViewModel main)
    {
        var statement = main.Statement;
        statement.SelectedRow = statement.Categories.MaxBy(row =>
        {
            statement.SelectedRow = row;
            return statement.Transactions.Count;
        })!;
    }

    /// <summary>Scrolls the transaction list part way, then puts the pointer on its scroll bar's handle.</summary>
    private static void ScrollTransactionsAndPointAtTheHandle(MainWindow window)
    {
        var model = (MainViewModel)window.DataContext!;
        var list = window.GetVisualDescendants().OfType<ListBox>().Single(l => ReferenceEquals(l.ItemsSource, model.Statement.Transactions));
        var scroller = list.GetVisualDescendants().OfType<ScrollViewer>().First();
        scroller.Offset = new Vector(0, (scroller.Extent.Height - scroller.Viewport.Height) / 3);
        Settle();
        var bar = scroller.GetVisualDescendants().OfType<ScrollBar>().First(b => b.Orientation == Avalonia.Layout.Orientation.Vertical);
        PointAt(window, bar.GetVisualDescendants().OfType<Thumb>().Single());
    }

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
        ShowRules(main);
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

    /// <summary>The ring's centre in the window.</summary>
    private static Point RingCentre(MainWindow window) =>
        window.SpendingPie.TranslatePoint(new Point(window.SpendingPie.Bounds.Width / 2, window.SpendingPie.Bounds.Height / 2), window)!.Value;

    /// <summary>Midway between the ring's edges: the ring runs from 0.6 to 0.94 of the way out from the centre.</summary>
    private static double RingRadius(MainWindow window) => 0.77 * window.SpendingPie.Bounds.Width / 2;

    /// <summary>Moves the pointer onto the ring at an angle in degrees, clockwise from 3 o'clock, where the ring starts, midway between its edges.</summary>
    private static void HoverRing(MainWindow window, double degrees)
    {
        var centre = RingCentre(window);
        var radians = degrees * Math.PI / 180;
        window.MouseMove(centre);
        window.MouseMove(new Point(centre.X + RingRadius(window) * Math.Cos(radians), centre.Y + RingRadius(window) * Math.Sin(radians)));
    }

    /// <summary>The angle of the middle of the slice whose category name starts with <paramref name="name"/>.</summary>
    private static double MiddleOf(MainWindow window, string name)
    {
        var slices = ((MainViewModel)window.DataContext!).Statement.Slices;
        var before = slices.TakeWhile(s => !s.Name.StartsWith(name, StringComparison.Ordinal)).Sum(s => s.Share);
        return (double)(before + slices.First(s => s.Name.StartsWith(name, StringComparison.Ordinal)).Share / 2) * 360;
    }

    /// <summary>Moves the pointer to the middle of a day's stretch of the balance line, halfway up the plotting area.</summary>
    private static void HoverDay(MainWindow window, DateOnly day)
    {
        var chart = window.BalanceLine;
        var area = chart.CoreChart;
        var noon = day.ToDateTime(new TimeOnly(12, 0)).Ticks;
        var x = chart.ScaleDataToPixels(new LvcPointD(noon, 0)).X;
        var middle = new Point(x, area.DrawMarginLocation.Y + area.DrawMarginSize.Height / 2);
        window.MouseMove(chart.TranslatePoint(new Point(area.DrawMarginLocation.X + 1, middle.Y), window)!.Value);
        window.MouseMove(chart.TranslatePoint(middle, window)!.Value);
    }

    private static DateOnly DayWithoutBookings(MainWindow window)
    {
        var statement = ((MainViewModel)window.DataContext!).Statement;
        var booked = statement.Categorized!.Transactions.Select(t => t.Transaction.BookingDate).ToHashSet();
        return statement.BalancePoints.Skip(1).First(p => !booked.Contains(p.Date)).Date;
    }

    private static DateOnly BusiestDay(MainWindow window)
    {
        var statement = ((MainViewModel)window.DataContext!).Statement;
        return statement.Categorized!.Transactions.GroupBy(t => t.Transaction.BookingDate).MaxBy(g => g.Count())!.Key;
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
            // The size DocuSpend opens at, so the picture shows what the user sees without scrolling.
            var window = new MainWindow(model) { Width = 1280, Height = 940 };
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
