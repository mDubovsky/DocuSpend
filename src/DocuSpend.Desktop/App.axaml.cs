using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DocuSpend.Categorization;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Look.LookResources.Apply(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The synthetic samples travel with the program, in a folder next to it.
            var samples = SampleCatalog.Available(Path.Combine(AppContext.BaseDirectory, "samples"));
            desktop.MainWindow = new MainWindow(new MainViewModel(samples, new SpikeRuleStore(SpikeRuleStore.DefaultPath), new SettingsStore(SettingsStore.DefaultPath)));
        }
        base.OnFrameworkInitializationCompleted();
    }
}
