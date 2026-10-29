using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DocuSpend.Categorization.Model;
using DocuSpend.Desktop.Look;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;

namespace DocuSpend.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _model;
    private IReadOnlyDictionary<ISeries, CategoryId> _sliceCategories = new Dictionary<ISeries, CategoryId>();

    /// <summary>For the XAML loader only; the application uses the constructor with a model.</summary>
    public MainWindow() : this(new MainViewModel(new RuleStore(Path.Combine(Path.GetTempPath(), "docuspend-designer-rules.json")))) { }

    public MainWindow(MainViewModel model)
    {
        InitializeComponent();
        DataContext = _model = model;
        model.Picker = new StatementPicker(this);

        // Drag events only bubble. Seeing handled events too, the window gets every drag even when a control inside
        // it (the rule form's text box) has handled it.
        AddHandler(DragDrop.DragEnterEvent, OnDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(DragDrop.DropEvent, OnDrop, RoutingStrategies.Bubble, handledEventsToo: true);

        // A help mark's tooltip opens on hover by itself; Avalonia 11 has no option to open it on keyboard focus too.
        AddHandler(GotFocusEvent, (_, e) =>
        {
            if (e.Source is Button help && help.Classes.Contains("help") && e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional)
                ToolTip.SetIsOpen(help, true);
        }, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(LostFocusEvent, (_, e) =>
        {
            if (e.Source is Button help && help.Classes.Contains("help"))
                ToolTip.SetIsOpen(help, false);
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        SpendingPie.DataPointerDown += OnSliceClicked;
        WireHoverCards();
        model.Statement.PropertyChanged += OnStatementChanged;
        model.Categories.PropertyChanged += OnCategoriesChanged;
        AddHandler(KeyDownEvent, OnCategoryKeyDown, RoutingStrategies.Bubble);
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Look))
                ApplyLook(model.Look);
            // The deletion question takes the focus on the box itself, with no button chosen and no focus mark, so
            // Tab moves among its buttons rather than through the dimmed window behind it.
            if (e.PropertyName == nameof(MainViewModel.RuleToDelete) && model.RuleToDelete is not null)
                Avalonia.Threading.Dispatcher.UIThread.Post(() => DeleteDialogBox.Focus());
            // The charts are hidden on the start screen. LiveCharts measures a chart only after it has painted a
            // frame since its last measure, and resets that only when the chart scrolls into view, so charts that
            // were hidden stayed empty until scrolled away and back. That is also the case when the user comes back from the
            // rules screen. Once they have been rendered visible, ask both
            // to update.
            if (e.PropertyName == nameof(MainViewModel.ShowsStatement))
                HideHoverCards();
            if (e.PropertyName == nameof(MainViewModel.ShowsStatement) && model.ShowsStatement)
                RequestAnimationFrame(_ => RequestAnimationFrame(_ =>
                {
                    SpendingPie.CoreChart.Update();
                    BalanceLine.CoreChart.Update();
                }));
        };
        ApplyLook(model.Look);
        // The charts draw their own colours, so a change of look (the switch, or Windows) redraws them.
        ActualThemeVariantChanged += (_, _) => DrawCharts();
        DrawCharts();
    }

    /// <summary>
    /// "Podľa systému" hands the choice to Avalonia, which follows Windows, also when it changes while DocuSpend runs;
    /// the other two fix it. The whole application changes at once, and the charts redraw on the change.
    /// </summary>
    internal static void ApplyLook(LookChoice choice)
    {
        if (Avalonia.Application.Current is { } app)
            app.RequestedThemeVariant = choice switch
            {
                LookChoice.Light => ThemeVariant.Light,
                LookChoice.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
    }

    /// <summary>The palette of the look the window shows now.</summary>
    internal Palette Palette => LookResources.For(ActualThemeVariant);

    /// <summary>
    /// Enter in a name field of the "Kategórie" card adds what is typed. Without this, the rule form's default button
    /// would take the key and add a rule.
    /// </summary>
    private void OnCategoryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.Source is not TextBox field)
            return;
        if (field.Name == nameof(CategoryNameField))
            _model.AddCategoryCommand.Execute(null);
        else if (field.Name == "SubcategoryNameField")
            _model.AddSubcategoryCommand.Execute(null);
        else
            return;
        e.Handled = true;
    }

    /// <summary>A line for a new sub-category took the cursor; a new category scrolls into view.</summary>
    private void OnCategoriesChanged(object? sender, PropertyChangedEventArgs e)
    {
        var card = _model.Categories;
        if (e.PropertyName == nameof(CategoriesViewModel.LineOpenings) && card.OpenRow is { } open)
            Avalonia.Threading.Dispatcher.UIThread.Post(() => RowOf(open)?
                .GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "SubcategoryNameField")?.Focus(),
                Avalonia.Threading.DispatcherPriority.Loaded);
        if (e.PropertyName == nameof(CategoriesViewModel.LastAdded) && card.LastAdded is { } added)
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                RowOf(card.Rows.FirstOrDefault(r => r.Id == added))?.BringIntoView(),
                Avalonia.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>The control that shows a row of the "Kategórie" card, once the list has made it.</summary>
    private Control? RowOf(CategoryCardRow? row)
    {
        var index = row is null ? -1 : _model.Categories.Rows.ToList().IndexOf(row);
        return index < 0 ? null : CategoryList.ContainerFromIndex(index);
    }

    private void OnStatementChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StatementViewModel.Slices) or nameof(StatementViewModel.BalancePoints))
            DrawCharts();
    }

    private void DrawCharts()
    {
        HideHoverCards();
        var palette = Palette;
        var (ring, categories) = Charts.Ring(_model.Statement.Slices, palette);
        _sliceCategories = categories;
        SpendingPie.Series = ring;

        BalanceLine.XAxes = Charts.DateAxis(palette);
        BalanceLine.YAxes = Charts.AmountAxis(palette);
        BalanceLine.Series = Charts.Balance(_model.Statement.BalancePoints, palette);
    }

    /// <summary>Anything may be dropped, so a wrong drop gets its message; only files show "Pustite výpis sem".</summary>
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        _model.IsDraggingOver = e.DataTransfer.Contains(DataFormat.File);
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        _model.IsDraggingOver = false;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        await _model.DropAsync(Dropped(e.DataTransfer));
    }

    /// <summary>The local paths of the dropped files, and how many other things (folders, text) came with them.</summary>
    internal static DroppedContent Dropped(IDataTransfer data)
    {
        var items = data.Items;
        var files = items.Select(i => i.TryGetFile()).OfType<IStorageFile>()
            .Select(f => f.TryGetLocalPath()).OfType<string>().ToList();
        // Nothing usable at all still counts as one wrong thing, so it gets the "only a file" message.
        var others = Math.Max(items.Count - files.Count, files.Count == 0 ? 1 : 0);
        return new DroppedContent(files, others);
    }

    private void OnSliceClicked(IChartView chart, IEnumerable<ChartPoint>? points)
    {
        if (points?.FirstOrDefault()?.Context.Series is { } series && _sliceCategories.TryGetValue(series, out var category))
            _model.Statement.SelectedCategory = category;
    }
}
