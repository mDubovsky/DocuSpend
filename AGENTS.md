# AGENTS.md

DocuSpend is an open-source tool that turns bank statements (PDF, spreadsheet, …) into charts of when and how you spent money.

## Rules

1. **Never commit without the human's approval.** Intent, spec and plan are each committed after the human approves them. Once the plan is approved, that approval covers the build: commit each build step when it is done and every test passes, without asking, and carry on to the next. Stop and wait only at the plan's marked review stops (screenshots, hand checks, privacy findings), when a decision is the human's to make, or when something goes against the spec or plan.
2. **Never push. Pushing is for humans only.**
3. **Never work or commit on `main`.** `main` is for merging PRs only. Work on a branch named after the item (`NNN-slug`, where NNN is the GitHub issue number), created from an up-to-date `main`.
4. Never commit real financial data. Do not read anything in `samples/private/` unless the human explicitly asks, and never reproduce its contents in committed files, tests, or docs. Use synthetic fixtures only.
5. Test samples may copy a bank's statement layout but never its identity (name, legal details, BIC, bank code, logo); see [decision 0002](docs/decisions/0002-fictional-bank-identity-in-samples.md).
6. Before working on an item, read its `intent.md`, `spec.md` and `plan.md` in `work/<NNN-slug>/`.
7. Unit tests are required from the start. Every behavior change ships with tests, and tests use synthetic statements from `samples/public/`, never real data.
8. Do not make technology choices outside a `plan.md`. Do not start implementing before the spec and plan are approved.
9. **No code that can see a statement may use the network, and no dependency that does.** No networking, telemetry, analytics, crash reporting or auto-update in the application. See [decision 0006](docs/decisions/0006-privacy-approach-and-network-boundary.md); changing this is a change to that decision. The build enforces it (see `privacy/README.md`): never suppress or work around a privacy check, and never add an entry to `privacy/allowed-dependencies.json`, `privacy/native-libraries.json` or `privacy/exceptions.json` without the human reviewing it.
10. Everything the user sees is in Slovak; code, docs, issues and commit messages are in English; see [decision 0003](docs/decisions/0003-slovak-interface-english-code.md).

## Where things are

- `docs/product.md`: what the product is and why (no technical detail)
- `docs/mvp.md`: what the first version includes and excludes
- `docs/glossary.md`: shared terms; use them consistently
- `docs/decisions/`: short records of non-obvious decisions
- `docs/sdlc.md`: the intent → spec → plan workflow, and how to choose the next item
- `work/`: one folder per backlog item, named after its GitHub issue; templates in `work/_templates/`
- `.claude/skills/`: project skills (`new-issue`, `new-intent`, `write-spec`, `write-plan`, `finish-item`)
- `PRIVACY.md`: what the application does with a statement, for users; `privacy/`: the lists the privacy checks enforce
- `.github/`: issue and PR templates, `workflows/` (CI, code scanning, release) and `dependabot.yml`
- `samples/public/`: committed synthetic statements for tests; `samples/private/` is gitignored

## Stack, build and test

.NET (C#), see [docs/decisions/0001-use-dotnet.md](docs/decisions/0001-use-dotnet.md). The SDK version is pinned in `global.json`. The user interface is a native Avalonia desktop application for Windows, with LiveCharts2 for the charts ([decision 0005](docs/decisions/0005-avalonia-for-the-user-interface.md), confirmed by the spike in [work/005-avalonia-spike/findings.md](work/005-avalonia-spike/findings.md)). Avalonia's build telemetry is switched off in the build; keep it off (see `privacy/README.md`). The look, light and dark, is defined once: colours in `src/DocuSpend.Presentation/Look/Palette.cs` (contrast is tested), sizes and styles in `src/DocuSpend.Desktop/Look/Look.axaml`. Screens use only its resources, and a test fails on a literal colour, size or spacing in any other XAML file ([work/011-modern-look/spec.md](work/011-modern-look/spec.md)). Scroll bars are the look's own `ControlTheme`, always shown beside the content: a control whose template binds `ScrollViewer.AllowAutoHide` (as `ListBox` and `TextBox` do) needs it set to `False` in the look, since a style on `ScrollViewer` can't override a template binding ([work/019-look-switch-scroll-bars/plan.md](work/019-look-switch-scroll-bars/plan.md)). The user's rules and categories are kept in `%APPDATA%\DocuSpend\rules.json` (`SavedRules` in `DocuSpend.Categorization` for the shape, `RuleStore` in `DocuSpend.Presentation` for the file). The shape is versioned: never change `tests/DocuSpend.Categorization.Tests/Saving/rules-v1.json` to follow a new shape; raise the version and add a reader for the old one ([work/013-keep-rules/plan.md](work/013-keep-rules/plan.md)). A change may give a user rule a priority from 1 to 255 (`CategorizationConfig.MaxUserPriority`); rules saved earlier with a higher number still load ([work/024-edit-rules/spec.md](work/024-edit-rules/spec.md)).

Run from the repo root:

- Build: `dotnet build`
- Test: `dotnet test` (includes the privacy checks; one check alone: `dotnet test --project tests/DocuSpend.Privacy.Tests --filter-trait "Check=privacy-dependencies"`, see CONTRIBUTING.md)
- Writing tests (where each kind goes, how to make saving fail, the headless window), screenshots, and what the look test allows in screens: CONTRIBUTING.md, "Writing tests"
- Run the desktop application (Windows; it opens a statement you choose, or the example): `dotnet run --project src/DocuSpend.Desktop`
- Screenshots of every screen in both looks, without a screen: `dotnet run --project tools/DocuSpend.Screenshots` (writes to `artifacts/screenshots/`; CI uploads them too). Compare a change that touches the look against `work/011-modern-look/screenshots/`.
- Verify the read-and-categorize flow on the samples: `dotnet run --project tools/DocuSpend.StatementCheck -- --verify samples/public`
- Regenerate test samples: `dotnet run --project tools/DocuSpend.SampleGenerator -- generate` (see [samples/README.md](samples/README.md); never edit samples by hand)
- Check the reader on real statements (maintainer, locally, only with the human's go-ahead): `dotnet run --project tools/DocuSpend.StatementCheck` (reads `samples/private/`; prints counts or a rejection, never content). Add `--categorize` to also print how many transactions each built-in category got (counts only).

Layout: `src/` for application code (`DocuSpend.Statements`: reading statements into transactions; `DocuSpend.Categorization`: sorting transactions into categories; `DocuSpend.Presentation`: screen logic, Slovak texts and formatting, with no dependency on Avalonia; `DocuSpend.Desktop`: the Avalonia application, kept thin), `tools/` for developer tooling (`DocuSpend.Screenshots` renders the screens), `tests/` for test projects (`DocuSpend.Privacy.Tests` holds the privacy checks; `DocuSpend.Desktop.Tests` drives the real window without a screen). Every project under `src/` and `tools/` counts as shipping for the dependency check.
