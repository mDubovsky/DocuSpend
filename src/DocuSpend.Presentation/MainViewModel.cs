using CommunityToolkit.Mvvm.ComponentModel;
using DocuSpend.Categorization;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Presentation;

/// <summary>The window: the samples to choose from, the statement shown, and (later) the rule form.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IStatementReader _reader = new VubStatementReader();

    public MainViewModel(IReadOnlyList<SampleItem> samples, CategorizationConfig config)
    {
        Samples = samples;
        Statement = new StatementViewModel(config);
        SelectedSample = samples.FirstOrDefault();
    }

    public IReadOnlyList<SampleItem> Samples { get; }

    public StatementViewModel Statement { get; }

    [ObservableProperty]
    public partial SampleItem? SelectedSample { get; set; }

    /// <summary>Why the chosen sample isn't shown, or null.</summary>
    [ObservableProperty]
    public partial string? Error { get; private set; }

    partial void OnSelectedSampleChanged(SampleItem? value)
    {
        if (value is null)
            return;
        var result = _reader.Read(File.ReadAllBytes(value.Path));
        Error = result.IsSuccess ? null : Texts.StatementUnreadable;
        if (result.Statement is { } statement)
            Statement.Show(statement);
    }
}
