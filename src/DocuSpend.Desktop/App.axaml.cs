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
            // The example statement travels with the program, in a folder next to it.
            desktop.MainWindow = new MainWindow(new MainViewModel(
                new SpikeRuleStore(SpikeRuleStore.DefaultPath), new SettingsStore(SettingsStore.DefaultPath), Example.DefaultPath));
        }
        base.OnFrameworkInitializationCompleted();
    }
}
