# Findings: Try Avalonia for the MVP screens

Intent: [intent.md](intent.md) · Spec: [spec.md](spec.md) · Plan: [plan.md](plan.md)

## Verdict: go

Avalonia 11.3 with LiveCharts2 2.0.5 does everything the MVP screens need, with little friction, and its privacy cost is small, known and enforced. [Decision 0005](../../docs/decisions/0005-avalonia-for-the-user-interface.md) is confirmed, and LiveCharts2 is confirmed as the chart library.

The spike built the statement view (header, spending pie, balance line, category and transaction lists, click-through from the pie), a rule form with three kinds of condition, rules saved across restarts, a Windows build made by CI, and window tests that run without a screen. Every acceptance criterion of the spec is met; the hand checks are below.

## What worked, and what was awkward

| Part | Worked | Awkward |
|---|---|---|
| Charts | Pie with legend and tooltips, step line for the balance, click on a slice to select a category: all on the first build. Slovak axis labels and tooltips through formatter functions. | LiveCharts switches data labels on whenever a label paint is set, ignoring its own "show labels" flag, so small slices get no paint instead. The typeface setting from older examples is obsolete in 2.0.5; the compiler named its replacement. |
| Lists and selection | Plain list boxes with item templates; the selected row and the selected category stay in step both ways through one view-model property. | None worth noting. A data grid would need an extra package; lists were enough. |
| Forms | Choice boxes, text boxes, a button bound to a command, a field shown only for its kind of condition: all through bindings, no code in the window. | None. |
| Slovak | All texts and formatting from one class each. Diacritics render everywhere, including inside the charts once the charts are given Segoe UI. Amounts and dates stay Slovak on English Windows. | Chart texts are drawn by SkiaSharp, not by Avalonia, so they need their own typeface setting; without it diacritics depend on SkiaSharp's fallback. |
| Saving rules | Writing to the user's application-data folder works in the self-contained build and in a clean Windows. | Nothing toolkit-specific. |
| Windows build | One `dotnet publish` command; self-contained; runs on a clean Windows with nothing installed. | A self-contained publish wrote the .NET runtime into the folder the dependency check reads; fixed by giving it its own subfolder. |
| Bindings | Compiled bindings for the whole project: a typo in a binding fails the build. Negation (`!`) and null checks work in bindings without converters. | None. |
| Window tests | The real window, from its compiled XAML, driven without a screen through Avalonia's headless platform; runs on Windows and Linux in about three seconds. | Avalonia 11.3's xunit integration needs xunit v2 (the repository uses v3); a ten-line helper drives the headless session instead. Avalonia 12 supports xunit v3. |

## Where the toolkit fought back

Mostly it didn't. The whole spike, from the first package to the CI build, took one working session, and the screen worked on the first build apart from one obsolete method. The real effort went into privacy, not into the interface:

- **Build telemetry.** The `Avalonia` package always brings `Avalonia.BuildServices`, which sends anonymous usage data to the Avalonia team on every build. It doesn't ship, but it ran on every build machine; it is now off in three layers with a guard test ([privacy/README.md](../../privacy/README.md#avalonias-build-telemetry-is-off)).
- **The previewer.** Avalonia ships its design-time previewer support inside the application, and it uses sockets. It is now left out of what ships, so the IDE's live XAML preview doesn't work. Nobody missed it in the spike; the compiled bindings and a quick run cover most of what it would show.
- **Avalonia 12.** The current Avalonia is 12.1; LiveCharts2's stable release supports 11 only, and its Avalonia 12 support exists only in development builds (last one June 2026). Avalonia 11.3 is still maintained (11.3.22, September 2026).

## Privacy picture

| What | Where it comes from | Networking | Outcome |
|---|---|---|---|
| `Avalonia.BuildServices` | `Avalonia`, at build time only | Sends build telemetry | Switched off in every project; guard test; CI also opts out |
| `Avalonia.DesignerSupport.dll` | `Avalonia` | Sockets (the previewer) | Not shipped (`PrivacyRemovedFromOutput`); the check requires it absent |
| `Avalonia.Remote.Protocol` 11.3.22 | `Avalonia.Controls` | Sockets and DNS, only in its two transport classes, which only the previewer creates | Reviewed in `allowed-dependencies.json`; its namespace is banned in our code |
| `libSkiaSharp.dll` (SkiaSharp 2.88.9) | `Avalonia.Skia`, LiveCharts2 | Imports no networking library | Reviewed in `native-libraries.json` |
| `libHarfBuzzSharp.dll` (HarfBuzzSharp 8.3.1.1) | `Avalonia.Skia` | Imports no networking library | Reviewed in `native-libraries.json` |
| `av_libglesv2.dll` (ANGLE) | `Avalonia.Win32` | Imports no networking library | Reviewed in `native-libraries.json` |
| LiveCharts2, CommunityToolkit.Mvvm, the rest of Avalonia | | None | Nothing to review |

Using only Avalonia's Windows packages, not `Avalonia.Desktop`, kept the macOS and Linux native files out of review. With the network off, the application starts and does everything; while it runs it opens no connection (hand checks A and B1).

## The Windows build

- **Size:** 106.3 MB in 236 files unpacked, 48.9 MB as the CI artifact (zipped). Most of it is the .NET runtime and Skia. No trimming, single-file or ahead-of-time compilation was tried; trimming is the likely first step if size matters, and needs care with Avalonia.
- **Start:** about 4 seconds to the window on a cold start and about 1.8 seconds warm, measured on the maintainer's machine. Ahead-of-time compilation or ready-to-run images would shorten it.
- **On the target machine:** nothing needs to be installed. The build is unsigned, so Windows may warn before the first start (decision 0005, consequences).

## Hand checks

Done by the maintainer on 24 October 2026, with the CI build of commit `146bd80`. All passed.

| Check | Result |
|---|---|
| A1–A10, in Windows Sandbox (a clean Windows, English, networking disabled): starts with nothing installed; all views; Slovak formatting on English Windows; diacritics in the window and the charts; selection; all five samples; adding each kind of rule; invalid input; rules after a restart; the rules file's location | Passed |
| B1, no network connections while the application runs (`Get-NetTCPConnection`, `Get-NetUDPEndpoint`, Resource Monitor) | Passed |
| B2, display scaling at 125 % and 150 % | Passed |
| B3, a small window | Passed |
| B4, a damaged rules file | Passed |

## A dark theme, probed

The maintainer asked for a more modern look, possibly dark. Visual design is out of scope here, so only the toolkit question was answered: how hard is it? A probe, not kept, switched Avalonia to its dark variant and LiveCharts to its dark theme (two lines), and replaced the window's two hard-coded colours with the theme's own (two more). The result was a consistent dark window with readable charts. Following Windows' light or dark setting while the application runs wasn't tried: Avalonia follows it by itself, but LiveCharts' theme is set once at start, so the charts would need to be redrawn with the other theme.

## Project structure for the real screens

What the spike used, and recommends keeping:

- **`src/DocuSpend.Presentation`**: everything the screens decide, with no dependency on Avalonia: view models (with CommunityToolkit.Mvvm's generated properties and commands, which earned their place), `SlovakFormat` for all formatting and reading of numbers and dates, and `Texts` for every visible string. It reaches the reader and categorizer directly and is tested like the rest of the code.
- **`src/DocuSpend.Desktop`**: the Avalonia application, as thin as possible: XAML with compiled bindings, the start-up code, and the mapping from plain data to LiveCharts series (`Charts.cs`), which is the only code that knows the chart library.
- **Tests:** behavior in `tests/DocuSpend.Presentation.Tests` against the real samples; a few window tests in `tests/DocuSpend.Desktop.Tests` to catch what only the real window shows (a broken binding, a control that doesn't respond).
- **Texts in a class, not resource files:** it held up. One language, one class, checked by tests that every enum value has a Slovak name.

## Keep or throw away

Keep `DocuSpend.Presentation`, `DocuSpend.Desktop` and both test projects as the start of the real interface. Two parts are throwaway and must be replaced, not grown:

- `SpikeRuleStore` and its file format, by the issue for saving rules;
- the sample picker, by opening a statement the user chooses.

The window's layout and look are a sketch, to be replaced by the visual design.

## For the issue on saving rules

- `Environment.SpecialFolder.ApplicationData` (`%APPDATA%\DocuSpend\`) works in the self-contained build and in a clean Windows.
- Writing to a temporary file and moving it over the old one keeps a crash from leaving half a file; source-generated JSON needs no reflection.
- Refusing to overwrite a file that can't be read, and saying so, worked well and should stay.
- Re-adding rules in their original order with `AddUserRuleOnTop` reproduces the configuration only because the spike never changes priorities. The real format must store each rule's identifier, priority number and creation order (decision 0004).

## Suggested next issues

- **The visual design of the MVP screens**, light and dark, building on the probe above.
- **Moving to Avalonia 12**, once LiveCharts2 has a stable release for it; it also brings xunit v3 support for window tests.
- **Opening a statement the user chooses**, with the clear error messages the MVP requires.
- **Saving rules**, with the notes above.
