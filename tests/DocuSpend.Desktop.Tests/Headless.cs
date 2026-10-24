using Avalonia;
using Avalonia.Headless;

namespace DocuSpend.Desktop.Tests;

/// <summary>The application on Avalonia's headless platform: no screen, no window system, nothing drawn.</summary>
public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}

/// <summary>
/// One headless session for the whole test run, with its own UI thread. Avalonia 11.3's xunit integration needs
/// xunit v2, so tests hand their work to the session themselves.
/// </summary>
internal static class Headless
{
    private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.StartNew(typeof(TestApp));

    public static void Run(Action test) => Session.Dispatch(test, CancellationToken.None).GetAwaiter().GetResult();
}
