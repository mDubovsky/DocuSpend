# Contributing

DocuSpend is built with an AI-assisted workflow: every change starts as an idea (a GitHub issue), becomes an intent, then a spec, then a plan, then code. See [docs/sdlc.md](docs/sdlc.md).

1. **Open an issue** describing the problem or idea (use the issue templates). Wait for it to be picked up before starting work.
2. **Branch** from an up-to-date `main`, named `NNN-slug` after the issue (e.g. `042-synthetic-statement-generator`). Never commit directly to `main`.
3. **Write the docs first:** intent, spec and plan in `work/NNN-slug/` (see [work/README.md](work/README.md)). Get them agreed before implementing.
4. **Implement with unit tests**, using synthetic data only.
5. **Open a PR** that says `Closes #NNN`. PRs are squash-merged into `main`.

Small fixes (typos, minor doc edits) can go straight to a branch and PR without an issue.

**Privacy:** never commit real bank statements or other personal financial data. Use synthetic samples in `samples/public/`. `samples/private/` is gitignored for your own local files.

## Writing tests

Every behavior change ships with tests on the synthetic samples (`Samples.Path` and `Samples.Read` in each test project). Where each kind of test goes:

- **`DocuSpend.Categorization.Tests`**: rules, categories and saving, below any screen. Only this project sees the categorization's internals (`InternalsVisibleTo`), such as `CategorizationConfig.Create`, which builds a configuration from given rules, identifiers and creation orders included.
- **`DocuSpend.Presentation.Tests`**: what a screen does, through the view models, with no window. Start a `MainViewModel` on a `RuleStore` in a temporary folder, open a sample with `OpenFileAsync`, and run commands as a click would. To see a second session, start a new `MainViewModel` on the same file. To reach the problems the screen must report:
  - **saving fails:** put a file where the store's folder should be, so the folder can't be created;
  - **saving is off for the session:** write `{ "version": 2 }` to `rules.json` before starting (a file from a newer DocuSpend);
  - **damaged rules:** write `{ damaged` to `rules.json`.
- **`DocuSpend.Desktop.Tests`**: what only the real window shows, without a screen:
  - wrap each test in `Headless.Run`, and wait for async commands with `Headless.Wait`;
  - after changing state, call `Dispatcher.UIThread.RunJobs()` before looking at controls;
  - find controls by `x:Name` with `FindControl`, and give anything a test needs to find a name;
  - press a button by running its `Command` with its `CommandParameter`; send a key with `window.KeyPress(...)` from `Avalonia.Headless`;
  - a hidden control can't take focus, so `Focus()` on it does nothing;
  - `IsEffectivelyVisible` doesn't say whether a part of a control's own template is shown (an `Expander`'s content, a popup). Check the control's own property instead (`IsExpanded`, `ToolTip.GetIsOpen`).

Keep timing tests generous (a second for what should feel instant), since CI machines vary.

### Screenshots

A change to a screen adds or updates a screen in `tools/DocuSpend.Screenshots/Screens.cs`. Each `Screen` has:
- `Arrange`: gets the window into the state through the view models, as a user would;
- `Prepare`: writes the rules file before the window starts, when needed;
- `InWindow`: does what only the window can, such as opening a tooltip.

The tool renders a 1280 × 1660 window so nothing needs scrolling, in both looks, into `artifacts/screenshots/`. Look at both looks of every changed screen before asking for a review. A tooltip or popup is drawn at the window's top-left corner there, because there is no mouse pointer; that is the tool, not the application.

### The look in screens

`LookCheck` (in `DocuSpend.Desktop.Tests`) fails on a literal value in any screen's XAML for these properties: `FontSize`, `FontWeight`, `CornerRadius`, `Margin`, `Padding`, `Spacing`, `RowSpacing`, `ColumnSpacing`, `Foreground`, `Background`, `BorderBrush`, `BorderThickness`, `BoxShadow` and `Opacity`, and on any colour. `0` and `Transparent` are allowed. Sizes that are structure (`Width`, `Height`, `MinWidth`, column widths) may be written in place. A new size or style goes in `src/DocuSpend.Desktop/Look/Look.axaml`; a new colour goes in `Palette.cs`, where its contrast is tested.

Avalonia's Fluent controls bring their own colours, which may not match the palette in one of the looks; an `Expander`'s header was grey in the light look and black in the dark one, for example. Style the control's template parts in `Look.axaml` from the palette. The Fluent theme is compiled, so its part names can't be searched for in the package. To find them, list `GetVisualDescendants()` of the control (type and `Name`) in a throwaway headless test, and delete the test afterwards.

Avalonia 11 doesn't open a tooltip on keyboard focus. `MainWindow` does it for every `Button.help`, so a new help mark needs only that class and a `ToolTip.Tip`.

## Privacy checks

DocuSpend's rule is [decision 0006](docs/decisions/0006-privacy-approach-and-network-boundary.md): **no code that can see a statement may use the network**. These checks enforce it on every change; [PRIVACY.md](PRIVACY.md) explains them for users, and [privacy/README.md](privacy/README.md) has the detail. Each check runs under its own name, so any of them can be made required for merging (see below).

| Check | What it checks | Locally |
|---|---|---|
| `build-and-test` | The whole repository builds and every test passes, on Windows and Linux. | `dotnet build`, `dotnet test` |
| `privacy-no-networking-code` | No networking in our own code, in any project, tools and tests included: a ban list compiled into every project, and tests that the ban can't be switched off or suppressed. | `dotnet build` |
| `privacy-dependencies` | Nothing that ships brings networking, telemetry, or unreviewed native code through a package, directly or indirectly. | `dotnet test` |
| `privacy-no-real-data` | No real statement, or look-alike, is tracked; `samples/public` holds exactly what the sample generator produces. | `dotnet test` |
| `privacy-no-network-run` | Reading and categorizing the samples works with the network removed, after a canary shows the sandbox really has no network. | CI only (Linux); `dotnet run --project tools/DocuSpend.StatementCheck -- --verify samples/public` runs the same flow with the network on |
| `dependency-vulnerabilities` | No package with a known vulnerability of moderate severity or higher, direct or indirect; Dependabot proposes updates. | `dotnet build` (NuGet audit) |
| `code-scanning` | CodeQL scanning of the C# code, on every change and weekly. | CI only, once public |
| `secret-scanning` | No tokens or keys in what is pushed. A repository setting, not a workflow. | GitHub only, once public |
| `release-provenance` | Release builds come only from CI, with a list of contents and, once public, a signed record of origin. | CI only (Release workflow) |

To run one privacy check's tests on its own: `dotnet test --project tests/DocuSpend.Privacy.Tests --filter-trait "Check=privacy-dependencies"` (likewise for the other names).

**When a check fails**, its message names the rule, where it was broken, and what to do. Usually the answer is to do the work without the network, the package or the file. If you believe a change is justified:

- **Networking in our own code** needs an exception: a change to decision 0006, `PrivacyException` in the project, and an entry in `privacy/exceptions.json`. An excepted project can never reach the statement code. See [privacy/README.md](privacy/README.md#requesting-an-exception).
- **A package that references networking it never uses** needs a review: an entry in `privacy/allowed-dependencies.json` with the version range, the reason and the reviewer, in the same pull request. Native files go in `privacy/native-libraries.json` the same way. If the application doesn't need the file at all, leave it out of what ships instead (`PrivacyRemovedFromOutput`). See [privacy/README.md](privacy/README.md#networking-in-what-ships-privacy-dependencies).
- **Avalonia's build telemetry** is switched off in the build, and a check fails if it comes back. See [privacy/README.md](privacy/README.md#avalonias-build-telemetry-is-off).
- **Real statements** belong in `samples/private`, which git ignores; tests use the synthetic samples in `samples/public`.

The lists in `privacy/` are reviewed like code. Adding to a ban list is a normal change; removing from one, or loosening a check, is a change to decision 0006.

## Repository settings

The workflows in `.github/workflows/` run the checks; a few protections are repository settings instead. Some of them are free only for public repositories, so while the repository is private they are prepared but off: the `code-scanning` job and the dependency-review step are skipped, and release builds come without a signed record of origin.

**Works while private:** `build-and-test`, `privacy-no-networking-code`, `privacy-dependencies`, `privacy-no-real-data`, `privacy-no-network-run`, `dependency-vulnerabilities` (NuGet's audit), release archives with their list of contents, and Dependabot's update proposals (`.github/dependabot.yml`).

**When the repository goes public**, switch these on in Settings:

1. **Actions → General → Workflow permissions:** "Read repository contents and packages permissions" (each job asks for more only where it needs it).
2. **Advanced Security** (Code security): turn on **Dependency graph**, **Dependabot alerts**, **Dependabot security updates**, **Secret scanning** and **Push protection**. Secret scanning is the `secret-scanning` check: it is a setting, not a workflow, and push protection blocks a push that contains a secret.
3. **Code scanning:** keep the advanced setup (`codeql.yml`); don't turn on the default setup, which would replace it. Under **Protection rules**, set **Check runs failure threshold** to "High or higher" for security alerts and "Errors" for other alerts, so a new high or critical finding fails the pull request and lower ones are only reported.
4. Run the **Release** workflow by hand once (Actions → Release → Run workflow) and verify the record with the command in `PRIVACY.md`.

**Checks required for merging:** the branch ruleset "Protect main" (Settings → Rules → Rulesets) allows changes to `main` only through a squash-merged pull request, blocks force pushes and deletion, and requires these checks by name: `build-and-test (windows)`, `build-and-test (linux)`, `privacy-no-networking-code`, `privacy-dependencies`, `privacy-no-real-data`, `privacy-no-network-run`, `dependency-vulnerabilities` and `code-scanning`. It also holds the code scanning thresholds from step 3. No approving review is required while there is a single contributor. The names are an interface; renaming a job is a documented change, and a test (`WorkflowTests`) checks that each name exists.

**Before the first real release of the application:** a Slovak version of `PRIVACY.md`, since its readers are the product's users ([decision 0003](docs/decisions/0003-slovak-interface-english-code.md)), and real releases published from the Release workflow, which today only keeps its files as workflow artifacts.
