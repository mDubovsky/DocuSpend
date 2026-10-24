using System.Globalization;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using DocuSpend.Desktop;
using DocuSpend.Presentation;
using DocuSpend.Presentation.Look;

// Renders every screen of the desktop application in both looks to PNG files, from the synthetic samples, without a
// screen (work/011-modern-look/spec.md, "Screenshots for review"). Rules go to a temporary file, never the user's.
//
// Usage: dotnet run --project tools/DocuSpend.Screenshots [-- <output folder>]   (default: artifacts/screenshots)

var output = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine("artifacts", "screenshots"));
Directory.CreateDirectory(output);

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = SlovakFormat.Culture;
Charts.Configure(animate: false);

var session = HeadlessUnitTestSession.StartNew(typeof(ScreenshotApp));
var written = new List<string>();
session.Dispatch(() =>
{
    // Chosen through the header's switch, as a user would, so the screenshots show the real path.
    foreach (var (choice, look) in new[] { (LookChoice.Light, "light"), (LookChoice.Dark, "dark") })
    {
        foreach (var screen in Screens.All)
            written.Add(Screens.Capture(screen, choice, look, output));
    }
}, CancellationToken.None).GetAwaiter().GetResult();
// The session isn't disposed: Avalonia's HeadlessUnitTestSession.Dispose() can throw while shutting down (it failed
// a CI run after every screenshot was written), and the process ends here anyway.

foreach (var file in written)
    Console.WriteLine(Path.GetRelativePath(Directory.GetCurrentDirectory(), file));
Console.WriteLine($"{written.Count} screenshots in {output}");

/// <summary>The application on the headless platform, drawing with Skia so the charts render.</summary>
internal static class ScreenshotApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
