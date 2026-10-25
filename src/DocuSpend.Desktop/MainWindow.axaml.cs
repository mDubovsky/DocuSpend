using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Styling;
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
    public MainWindow() : this(new MainViewModel(new SpikeRuleStore(Path.Combine(Path.GetTempPath(), "docuspend-designer-rules.json")))) { }

    public MainWindow(MainViewModel model)
    {
        InitializeComponent();
        DataContext = _model = model;

        SpendingPie.DataPointerDown += OnSliceClicked;
        model.Statement.PropertyChanged += OnStatementChanged;
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Look))
                ApplyLook(model.Look);
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

    private void OnStatementChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StatementViewModel.Slices) or nameof(StatementViewModel.BalancePoints))
            DrawCharts();
    }

    private void DrawCharts()
    {
        var palette = Palette;
        var (ring, categories) = Charts.Ring(_model.Statement.Slices, palette);
        _sliceCategories = categories;
        SpendingPie.Series = ring;

        BalanceLine.XAxes = Charts.DateAxis(palette);
        BalanceLine.YAxes = Charts.AmountAxis(palette);
        BalanceLine.Series = Charts.Balance(_model.Statement.BalancePoints, palette);

        var (background, text) = Charts.Tooltip(palette);
        SpendingPie.TooltipBackgroundPaint = BalanceLine.TooltipBackgroundPaint = background;
        SpendingPie.TooltipTextPaint = BalanceLine.TooltipTextPaint = text;
    }

    private void OnSliceClicked(IChartView chart, IEnumerable<ChartPoint>? points)
    {
        if (points?.FirstOrDefault()?.Context.Series is { } series && _sliceCategories.TryGetValue(series, out var category))
            _model.Statement.SelectedCategory = category;
    }
}
