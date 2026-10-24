# Plan: Try Avalonia for the MVP screens

Spec: [spec.md](spec.md)

## Approach

Two new projects, so the toolkit-specific part is as thin as possible and everything else is plain, testable C#:

1. **`src/DocuSpend.Presentation`**: a library with no toolkit dependency. It holds the screen logic (view models), the Slovak texts and formatting, the sample list and the throwaway rule saving. It uses the real reader and categorizer and is tested like the rest of the code.
2. **`src/DocuSpend.Desktop`**: the Avalonia application. Windows, styles, the two charts and the start-up code, and nothing that decides anything.

Both live under `src/` because they ship, so the privacy checks cover them automatically (every project under `src/` and `tools/` is checked). Whether they stay is decided in the findings; if the verdict is to throw the spike away, they are deleted in the change that starts the real interface.

### Versions (checked on NuGet, 24 October 2026)
| Package | Version | Why |
|---|---|---|
| `Avalonia`, `Avalonia.Win32`, `Avalonia.Skia`, `Avalonia.Themes.Fluent` | 11.3.22 | The newest release of the 11.3 line, which is still maintained (September 2026). |
| `LiveChartsCore.SkiaSharpView.Avalonia` | 2.0.5 | LiveCharts2's newest stable release. It supports Avalonia 11 only; Avalonia 12 support exists only in 2.1 development builds. |
| `CommunityToolkit.Mvvm` | 8.4.2 | Generates the property-change boilerplate for view models; a .NET Foundation library with no networking. |

**Why Avalonia 11.3 and not 12.1** (decided with the maintainer): a stable chart library matters more for the verdict than the newest toolkit, and prerelease chart bugs would muddy it. The findings record that charts lag Avalonia 12 and what moving to 12 would take.

**Only the Windows platform packages**, not `Avalonia.Desktop`, which also brings the macOS and Linux backends with their native files. Windows comes first ([decision 0005](../../docs/decisions/0005-avalonia-for-the-user-interface.md)); adding the other backends later is a package reference and a start-up line, which the findings confirm.

### Avalonia's build telemetry is switched off
`Avalonia` always pulls in `Avalonia.BuildServices`, which on every build runs a task that sends anonymous usage data to the Avalonia team (project name hashed, machine identifier, versions, operating system, CI detection). It runs only at build time and doesn't ship in the application, but it is network use in our dependency tree, and it would run on every contributor's machine and in CI. Its own opt-out is an environment variable each person must remember (`AVALONIA_TELEMETRY_OPTOUT=1`). Decided with the maintainer, it is switched off for everyone:

- **In the build:** `Directory.Build.targets` adds a reference to `Avalonia.BuildServices` with all of its assets excluded to every project that references Avalonia, so its build task is never imported. As a second layer, it also defines an empty `AvaloniaStats` target, so the task can't run even if the package's targets got in some other way.
- **In CI:** `AVALONIA_TELEMETRY_OPTOUT=1` is set in every workflow that builds (`ci.yml`, `codeql.yml`, `release.yml`), as a third layer.
- **A guard, under `privacy-dependencies`:** a test reads every project's `obj/project.assets.json` and fails if any project resolves `Avalonia.BuildServices` with its build assets included. It is shown failing on a fixture assets file in which they are included, as every guard is ([item 004](../004-privacy-guard-rails/intent.md)).
- **Documented:** `privacy/README.md` and `CONTRIBUTING.md` say what it is, why it's off, and that it must stay off.

Until the guard is in place, every local build is run with `AVALONIA_TELEMETRY_OPTOUT=1` set, so not even the first build sends anything.

### The rest of the privacy picture
- **Our own code** is under the networking ban as usual. Nothing in the plan needs a banned API: no `Process` (no "open folder" button), no loading by name, no native calls of our own.
- **The dependency check will flag things**, and every finding is reviewed by the maintainer before an entry is added. Expected, to be confirmed by running the check:
  - managed: `Avalonia.Remote.Protocol`, the protocol of Avalonia's design-time previewer, which uses sockets. Whether it can be reached from the running application is the question the review answers;
  - native files: SkiaSharp's `libSkiaSharp.dll`, HarfBuzz's `libHarfBuzzSharp.dll` (text shaping), and ANGLE's `av_libglesv2.dll` (graphics). Native files can't be inspected, only listed and reviewed. `PRIVACY.md` currently says DocuSpend includes no native components, so it is updated with this list.
- **No diagnostics packages:** `Avalonia.Diagnostics` (the developer tools window) and Avalonia's newer diagnostics support, which connects to an external tool, are not referenced, in any build configuration.
- **The application's build output is what ships.** `DocuSpend.Desktop` sets its runtime identifier to `win-x64` and keeps its output in the usual folder (`bin/<configuration>/net10.0/`), so the dependency check, which reads that folder, sees exactly the Windows assemblies and native files that the published build contains, and not the native files of every other platform.

### Screen logic (`DocuSpend.Presentation`)
- **`SlovakFormat`:** one place for the `sk-SK` culture: amounts ("1 349,87 €"), dates ("24. 10. 2026"), periods, and reading typed amounts ("12,50"). Everything uses the culture explicitly, never the current culture of the thread, so English Windows changes nothing. The application also sets `sk-SK` as its default culture at start-up, for anything in the toolkit that formats on its own.
- **`Texts`:** every visible string, in Slovak, in one static class. That includes the names of the transaction types and Slovak messages for the categorizer's refusal kinds (its own refusal messages are English, for developers). A resource file was considered; it adds a build step and tooling for a language list that, per [decision 0003](../../docs/decisions/0003-slovak-interface-english-code.md), has one entry, so a class is simpler. The findings say whether that holds.
- **`SampleCatalog`:** the valid samples `01` to `05`, read from a `samples` folder next to the program.
- **`StatementViewModel`:** reads a sample with the real reader, categorizes it with the current configuration, and exposes plain data the charts draw: pie slices (category, spending, share), balance points (date, end-of-day balance, from the opening balance at the period start to the closing balance at its end), the category list, and the selected category's transactions in booking-date order. It doesn't know about LiveCharts or Avalonia; the desktop project maps the plain data to chart series.
- **`RuleFormViewModel`:** the three kinds of condition, the category and sub-category choice, and validation with Slovak messages. It adds the rule with the categorizer's `AddUserRuleOnTop`, so the newest rule wins, and the statement view categorizes again.
- **`SpikeRuleStore`, throwaway:** a JSON file, `%APPDATA%\DocuSpend\spike-rules.json`, holding the user rules in the order they were added (condition kind, values, target sub-category). Loading adds them again in the same order with `AddUserRuleOnTop`, which gives the same result as when they were added. It uses `System.Text.Json` with a source-generated context (no reflection), and a damaged or unreadable file puts the store in a "not available" state that refuses to save. The name says "spike" on purpose.

### The application (`DocuSpend.Desktop`)
- One window, built in XAML with **compiled bindings switched on for the whole project**, so a binding mistake fails the build instead of failing silently at run time, and no binding needs reflection.
- The Fluent theme; Windows' own font (Segoe UI), which has every Slovak letter.
- The pie and the line chart with LiveCharts2. Chart labels are drawn by SkiaSharp, not by Avalonia, so LiveCharts is given a typeface that has the Slovak letters explicitly; that is a known stumbling block for diacritics in charts.
- Samples `01` to `05` are copied from `samples/public/` into the output's `samples` folder at build time, so the published build carries them. The privacy check for real data looks at tracked files only, and build outputs aren't tracked.
- Start-up uses the Windows platform and Skia only (`UseWin32().UseSkia()`), with no platform detection.

### Tests
- **`tests/DocuSpend.Presentation.Tests`** (xunit v3, like the other test projects): the screen logic on the real samples, test-first. That covers selecting a category, the pie slices and balance points against the samples' expected records, adding each kind of rule, validation messages, Slovak formatting under an English thread culture, the rule store's round trip and its damaged-file behavior (in a temporary folder, never the real application-data folder), and the sample catalog.
- **A headless interface test, as an experiment:** Avalonia's headless test package lets a test create the window without a screen. Its 11.3 release targets xunit v2, while the repository uses xunit v3; the step tries it, and if it doesn't fit, the findings say so and it isn't kept. Test projects don't ship, so this adds no reviewed entries.

### CI
- **A Windows build of the application:** a new job in `ci.yml`, `desktop-build (windows)`, runs on every change: it publishes `DocuSpend.Desktop` for `win-x64`, self-contained (the .NET runtime is included, so the target machine needs nothing installed), as a plain folder, zips it, and keeps it as a workflow artifact. It isn't a check name from the privacy table, so it doesn't affect the "required" list. The upload action is pinned to a commit, as every action is.
- The existing jobs build the new projects too, including on Linux: a `win-x64` application builds on Linux, it just can't run there.
- Not done in the spike: trimming, single-file publishing and ahead-of-time compilation. Each needs extra care with Avalonia; the findings record the size without them and whether they're worth trying.

### Hand checks
- **A clean machine, with the network off:** Windows Sandbox (part of Windows 11 Pro) is a fresh Windows with no .NET and no tools, and it can be started with networking disabled from a small configuration file (`<Networking>Disable</Networking>`). The CI artifact is copied in, unpacked and used through the spec's behavior.
- **No connections:** while the application runs on the maintainer's machine, its connections are listed for its process (`Get-NetTCPConnection -OwningProcess`, and Resource Monitor's network tab); there must be none.
- **English Windows:** the Sandbox's Windows is in English by default, which covers the formatting check by hand as well.
- **Display scaling and a small window:** on the maintainer's machine.

### Alternatives considered
- **One project instead of two:** fewer files, but the screen logic could then only be tested with the toolkit loaded, which is exactly what the real interface should avoid.
- **ReactiveUI**, Avalonia's other common companion: more powerful and much larger. The MVVM toolkit's generated properties and commands are enough here.
- **Hand-written property-change code instead of the MVVM toolkit:** possible, but it is boilerplate the real screens would repeat dozens of times; the findings say whether the toolkit earned its place.
- **`Avalonia.Desktop` with platform detection:** brings the macOS and Linux natives into review for a Windows-first spike.
- **Checking the published folder instead of the build output:** would need a change to the dependency check. Setting the runtime identifier makes the two the same for what matters (assemblies and native files), with no change to the check.

### Layout
```
src/DocuSpend.Presentation/      view models, SlovakFormat, Texts, SampleCatalog, SpikeRuleStore
src/DocuSpend.Desktop/           App, MainWindow (XAML), chart mapping, Program
tests/DocuSpend.Presentation.Tests/
work/005-avalonia-spike/findings.md
```

## Prerequisites
- Item 004 merged (the privacy checks and the pipeline). ✔
- The maintainer pushes the branch for the CI build and reviews every proposed entry in the privacy lists.
- Windows Sandbox enabled on the maintainer's machine for the clean-machine check (an optional Windows feature).

## Steps
Test-first for everything in `DocuSpend.Presentation`. One commit per step, each after the maintainer's approval. Steps 1 and 3 stop for a review of privacy findings before going on.

1. **Telemetry off, then the projects.** First the `Avalonia.BuildServices` exclusion in `Directory.Build.targets`, the empty target, and the guard test with its failing fixture; the CI environment variable in the three workflows. Then the two projects and the test project, added to the solution, with the desktop project showing an empty window titled "DocuSpend". Build with the opt-out variable set until the guard passes.
2. **First privacy review (stop).** Run the dependency check, and propose an entry for each finding in `privacy/allowed-dependencies.json` or `privacy/native-libraries.json`, with the reason and what was looked at to reach it. They are added only after the maintainer reviews them. A finding that can't honestly be explained as never reached ends the spike with a no-go.
3. **Formatting, texts, samples and the statement view model**, with tests: `SlovakFormat`, `Texts`, `SampleCatalog`, `StatementViewModel` (slices, balance points, category list, selection).
4. **The statement screen:** the header, both charts, the category list, the transaction list, the sample switcher, and the typeface for diacritics in charts. This is the riskiest part, so it comes first, and an early verdict is possible here if it goes badly.
5. **The rule form and the rule store**, with tests: `RuleFormViewModel`, `SpikeRuleStore`, then the form and the rules list on screen, and re-categorizing on save.
6. **The headless test experiment.**
7. **The Windows build in CI:** the `desktop-build (windows)` job. If the published output brings anything the build output didn't, it goes back to step 2's review.
8. **Hand checks** on the maintainer's machine and in Windows Sandbox, recorded as they are done.
9. **Findings and documentation:** `findings.md`; decision 0005 updated with the verdict and the chart library confirmed or not; `PRIVACY.md` (native components), `privacy/README.md` and `CONTRIBUTING.md` (build telemetry), and `AGENTS.md` (the stack section says no interface code exists; the layout gains the two projects). The intent's status becomes `done`.

## Risks and unknowns
- **Avalonia 11 is the previous major version.** Moving to 12 later is a migration, and LiveCharts2's support for 12 is still in development. The findings estimate it from Avalonia's migration notes and how much of the spike touches changed APIs.
- **Diacritics in charts.** SkiaSharp picks its own default typeface, which may lack some Slovak letters; the plan sets one explicitly. If that fails, it's a significant finding against the chart library, not the toolkit.
- **Slovak formatting differs between platforms.** .NET uses ICU for culture data, and the group separator for `sk-SK` is a no-break space, whose exact character can differ between ICU versions. Tests compare amounts with the separator normalized, and say so.
- **Native files can't be proven harmless.** SkiaSharp, HarfBuzz and ANGLE are widely used and open source, but the check can only list them. That limit is stated in `PRIVACY.md` already and gets the concrete list.
- **The previewer protocol.** If `Avalonia.Remote.Protocol` turns out to be reachable from the running application, accepting it would need a stronger reason than "never reached", and that is the maintainer's call in step 2.
- **The build telemetry might come back.** A new Avalonia version could rename the package or the target; the guard fails if the package's build assets are included, and a renamed package would show in the dependency review of the update.
- **Headless testing with xunit v3** may not be supported on 11.3; the experiment is allowed to fail.
- **Package size.** A self-contained build with Avalonia and SkiaSharp is likely large (tens of megabytes); the findings record it.
- **SmartScreen** warns about the unsigned build in the Sandbox. That is expected ([decision 0005](../../docs/decisions/0005-avalonia-for-the-user-interface.md), consequences) and only noted.

## Verification

| Spec criterion | How it is checked |
|---|---|
| Starts showing `01` with header, pie, category list and balance line matching the categorizer | `StatementViewModel` tests on sample `01` against its expected record; by hand: the window shows the same |
| Each sample `01` to `05` can be switched to | `SampleCatalog` and view model tests for each; by hand: switching |
| Clicking a slice or choosing a category lists exactly its transactions, in booking-date order | View model tests (automated); by hand: clicking |
| The pie has exactly the categories with spending above zero; the balance line starts and ends at the printed balances | View model tests on every valid sample, including `04` (empty); a category whose money back is at least its spending, built in the test if no sample has one |
| Text, amount and type rules move the matching transactions, and the views update | `RuleFormViewModel` and view model tests, one per kind (automated); by hand: the charts update |
| Invalid input gives a Slovak message and saves nothing | `RuleFormViewModel` tests for each invalid input |
| A rule survives a restart; the file is in the application-data folder | `SpikeRuleStore` round-trip test in a temporary folder; by hand: add, close, start, and look at the file's location |
| English Windows: Slovak text and formatting | `SlovakFormat` tests under an English thread culture (automated); by hand in Windows Sandbox |
| Diacritics in the window and the charts | By hand, with a rule targeting a sub-category with many diacritics and the built-in names |
| CI builds a Windows artifact that runs without .NET installed | The `desktop-build (windows)` job; by hand in Windows Sandbox |
| `dotnet build` and `dotnet test` pass, with every privacy check and every new entry reviewed | Locally and in CI; the review in step 2, recorded with the reviewer's name in each entry |
| Works with the network off and opens no connections | By hand: Windows Sandbox with networking disabled; `Get-NetTCPConnection` and Resource Monitor on the maintainer's machine |
| The build telemetry is off | The guard test and its failing fixture (automated) |
| `findings.md` with the verdict and every note; decision 0005 reflects it | Review |

Run everything with `dotnet build` and `dotnet test` from the repo root.
