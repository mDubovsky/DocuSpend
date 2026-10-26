using System.Runtime.ExceptionServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;

// Every window test runs on the one headless UI thread, so running test classes in parallel gains nothing. It does
// cost: each waiting class blocks a thread-pool thread, and on a small CI machine the statement reader's background
// work then waited long enough for a test to time out.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

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

    /// <summary>
    /// Runs the test on the session's UI thread, then finishes whatever it left queued while the application still
    /// exists. The session tears its isolated application down right after, and work still queued then (a layout
    /// pass) fails with a font error that would hide the test's own failure, so that failure is kept and rethrown.
    /// </summary>
    /// <summary>
    /// Waits for work started on the UI thread (opening a statement) while letting the UI thread run what it
    /// queues, as the real application's message loop would; blocking on the task would never finish.
    /// </summary>
    public static void Wait(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("The task didn't finish.");
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
        Dispatcher.UIThread.RunJobs();
        task.GetAwaiter().GetResult();
    }

    /// <summary>Chooses "Pravidlá", as a user does with the switch: the rule form and the rules card show only there.</summary>
    public static void ShowRules(DocuSpend.Presentation.MainViewModel model)
    {
        model.SelectedScreen = model.Screens.Single(s => s.Screen == DocuSpend.Presentation.AppScreen.Rules);
        Dispatcher.UIThread.RunJobs();
    }

    public static void Run(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        Session.Dispatch(() =>
        {
            try
            {
                test();
            }
            catch (Exception e)
            {
                failure = ExceptionDispatchInfo.Capture(e);
            }
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }, CancellationToken.None).GetAwaiter().GetResult();
        failure?.Throw();
    }
}
