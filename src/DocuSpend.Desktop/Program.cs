using System.Globalization;
using Avalonia;
using DocuSpend.Presentation;

namespace DocuSpend.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // The interface is always Slovak (decision 0003). DocuSpend formats with SlovakFormat explicitly; this covers
        // anything in the toolkit that formats on its own.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = SlovakFormat.Culture;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = SlovakFormat.Culture;
        Charts.Configure();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>The Windows platform and Skia only, with no platform detection.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseWin32()
            .UseSkia()
            .LogToTrace();
}
