using System.ComponentModel;
using Avalonia.Controls;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;

namespace DocuSpend.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _model;
    private IReadOnlyDictionary<ISeries, CategoryId> _sliceCategories = new Dictionary<ISeries, CategoryId>();

    /// <summary>For the XAML loader only; the application uses the constructor with a model.</summary>
    public MainWindow() : this(new MainViewModel([], Categorization.CategorizationConfig.Default)) { }

    public MainWindow(MainViewModel model)
    {
        InitializeComponent();
        DataContext = _model = model;

        BalanceLine.XAxes = Charts.DateAxis();
        BalanceLine.YAxes = Charts.AmountAxis();
        SpendingPie.DataPointerDown += OnSliceClicked;
        model.Statement.PropertyChanged += OnStatementChanged;
        DrawCharts();
    }

    private void OnStatementChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StatementViewModel.Slices) or nameof(StatementViewModel.BalancePoints))
            DrawCharts();
    }

    private void DrawCharts()
    {
        var (series, categories) = Charts.Pie(_model.Statement.Slices);
        _sliceCategories = categories;
        SpendingPie.Series = series;
        BalanceLine.Series = Charts.Balance(_model.Statement.BalancePoints);
    }

    private void OnSliceClicked(IChartView chart, IEnumerable<ChartPoint>? points)
    {
        if (points?.FirstOrDefault()?.Context.Series is { } series && _sliceCategories.TryGetValue(series, out var category))
            _model.Statement.SelectedCategory = category;
    }
}
