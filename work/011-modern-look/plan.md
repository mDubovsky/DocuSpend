# Plan: Give the MVP screens a modern look, light and dark

Spec: [spec.md](spec.md)

## Approach

The look is data first and styles second. Its colours live in one plain C# class in `DocuSpend.Presentation`, where they can be tested for contrast without the toolkit; the desktop application turns them into Avalonia resources and LiveCharts paints. Sizes, rounding and spacing live in one Avalonia styles file. Screens refer only to named resources, and a test fails if one doesn't.

Avalonia 11.3 provides what the look needs, checked before planning: theme variants (`Light`, `Dark`, `Default`, which follows Windows and its changes), per-variant resources, Fluent theme palettes per variant, the `ActualThemeVariantChanged` event, box shadows, and on Windows a dark title bar that follows the variant (seen in the spike's dark probe). LiveCharts 2.0.5 has ring charts (`InnerRadius`), gradient paints and settable tooltip and legend paints.

### The look as data (`DocuSpend.Presentation/Look`)
- **`Palette`**: a record of named colours as hex strings: window background, card, card edge, accent, text on accent, text, muted text, positive, error, error tint, chart grid, and the list of slice colours. `Palette.Light` and `Palette.Dark` are its two values. The slice colours are shared by both.
- **`Contrast`**: the WCAG contrast ratio of two colours. Tests check every pair the spec names: text, muted text, positive and error on the card and on the background at 4.5 to 1; key numbers, accent, chart line and every slice colour against the card at 3 to 1; text on accent at 4.5 to 1.
- **Slice colours**: fourteen mid-tone colours that meet 3 to 1 against both the white and the dark card. A quick calculation before planning found fourteen of fifteen candidates already do, so the constraint is achievable. A category's colour comes from its position in the category catalog, so it is the same in both looks, in every statement, and doesn't depend on which categories a month happens to have.
- **`LookChoice`** (`System`, `Light`, `Dark`) with its Slovak labels in `Texts`.

### Remembering the choice
- **`SettingsStore`**: `%APPDATA%\DocuSpend\settings.json`, holding only `{ "version": 1, "look": "dark" }`. It is written through a temporary file, like the spike's rules file, with source-generated JSON. A missing, damaged or unknown file reads as `System`, and a failed write is ignored (spec: the setting is a preference, not data). It is a separate file from the rules, so #13 can design the rules file freely.
- `MainViewModel` gains `LookChoice`, loaded at start and saved on change.

### The look in the application (`DocuSpend.Desktop`)
- **`Look/Look.axaml`**: the styles file. Text sizes (heading, key number, body, label), corner radii (card, control, pill), spacing (card padding, gaps), the card style with its shadow, list row styles with hover and the accent pill for the selected row, the primary button, message styles, and empty-state text.
- **`Look/LookResources.cs`**: builds Avalonia `ThemeDictionaries` for `Light` and `Dark` from `Palette`, and sets the Fluent theme's palettes (accent, region colour, base colours) for each, so the toolkit's own controls (choice boxes, text fields, scroll bars) match too.
- **Applying the choice**: `Application.RequestedThemeVariant` is `Default` for `System`, otherwise `Light` or `Dark`. Avalonia then follows Windows by itself.
- **Charts**: `Charts.cs` takes the palette of the current variant. The ring uses `InnerRadius` and slice colours by category, and the spending total is an Avalonia text block centred over the ring. The balance line uses the accent with a `LinearGradientPaint` fading to transparent, faint grid lines and muted axis labels. LiveCharts' tooltip and legend paints are global settings, so on `ActualThemeVariantChanged` the window sets them again for the new variant and redraws both charts. Selections are untouched, because they live in the view models.
- **The header control**: three choices bound to `MainViewModel.LookChoice`.
- **The window's XAML** is reworked to use only resources: no hex colours, named colours, or literal font sizes, corner radii, margins, paddings or spacings.

### The "only resources" check
A test in `DocuSpend.Desktop.Tests` reads every `.axaml` file of the application except `Look/Look.axaml`. It fails on a colour literal (`#…`, or a named colour such as `Firebrick`) in any attribute, and on a literal value in `FontSize`, `CornerRadius`, `Margin`, `Padding`, `Spacing`, `RowSpacing`, `ColumnSpacing`, `Foreground`, `Background` or `BorderBrush`. Layout values that are structure rather than look (grid column widths, chart heights) stay allowed. A fixture file with a violation shows the check failing.

### Screenshots for review
- **`tools/DocuSpend.Screenshots`**: a console tool that starts the application on Avalonia's headless platform with real Skia drawing, so the charts render. For each look it shows the states the spec lists, renders each with `CaptureRenderedFrame`, and saves PNGs to a folder given on the command line (default `artifacts/screenshots/`, which is added to `.gitignore`). The states are: sample 01 with a category selected, the empty month, the busy month, the rule form with an error and with a confirmation, and the rule list with rules. Chart animations are switched off for the capture. One command: `dotnet run --project tools/DocuSpend.Screenshots`.
- It uses a temporary rules file and setting, never the user's own.
- The screenshots the maintainer approves are committed in `work/011-modern-look/screenshots/` as the record of the approval. There's no automatic pixel comparison, because font rendering differs between machines; a later change is compared by eye against the committed set.
- The tool is under `tools/`, so the dependency check covers it. It adds `Avalonia.Headless`, which brings no networking: the spike's window tests already used it, although tests don't ship.
- CI's `desktop-build (windows)` job also runs the tool and uploads the screenshots as an artifact, so every change's look can be seen without building it.

### Tests
- **`DocuSpend.Presentation.Tests`**: `Contrast` against known WCAG values; every palette pair the spec names; the slice colours are distinct, enough for every spending category, and the same for a category in both looks and in every sample; `SettingsStore` round trip, missing, damaged and unwritable files (in a temporary folder); `MainViewModel` loads the saved choice and saves a change; incoming amounts are marked for the positive colour.
- **`DocuSpend.Desktop.Tests`** (headless): the "only resources" check and its fixture; switching the variant changes a card's background and the charts' paints, and keeps the selected category and the typed form text; the ring has its inner radius and the slice colours by category; the header control offers the three choices.
- Following Windows while DocuSpend runs is done by Avalonia, so it is a hand check (switch Windows' setting while DocuSpend runs), recorded with the screenshot approval.

### Alternatives considered
- **Colours in XAML only**, in theme dictionaries: the usual Avalonia way, but contrast couldn't be tested without parsing XAML, and the charts would need the same colours a second time in C#.
- **A third-party theme package** (for example a Material or "semi" design theme): a new dependency to review, and its look would still need adapting to the references.
- **Smooth chart lines**: rejected in the spec, because a smoothed balance would be wrong.
- **Committing screenshots and comparing pixels in CI**: too brittle across machines and fonts; the approved set is a record, not a test.

### Layout
```
src/DocuSpend.Presentation/Look/     Palette, Contrast, LookChoice, SettingsStore
src/DocuSpend.Desktop/Look/          Look.axaml, LookResources.cs
tools/DocuSpend.Screenshots/         the screenshot tool
work/011-modern-look/screenshots/    the approved screenshots
```

## Prerequisites
- Item 005 merged (the desktop application). ✔
- The maintainer reviews the screenshots and does the hand check on following Windows.

## Steps
Test-first for everything in `DocuSpend.Presentation`. One commit per step, each after the maintainer's approval.

1. **The palette and contrast**: `Palette`, `Contrast`, slice colours by category, with the contrast tests. The two palettes' colours are proposed in this step, as a small table, before any screen uses them.
2. **The screenshot tool** (and `artifacts/` added to `.gitignore`), first on today's screens, so every later step can be compared and the "before" set exists.
3. **The look in the application**: `Look.axaml`, `LookResources.cs`, the reworked window XAML, and the "only resources" check with its fixture. Screenshots of both looks for a first review (stop).
4. **Charts in the look**: the ring with the total in its centre, slice colours by category, the gradient balance line, tooltip and legend paints, and redrawing on a variant change. Screenshots (stop).
5. **The choice**: `LookChoice`, `SettingsStore`, the header control, applying it, with tests.
6. **CI**: the screenshot tool in the `desktop-build (windows)` job, with the screenshots as an artifact.
7. **Approval and documentation**: the maintainer's final screenshot review and the hand check on following Windows; the approved screenshots committed; `AGENTS.md` (the look and the screenshot command), `privacy/README.md` only if anything new needed review. The intent's status becomes `done`.

## Risks and unknowns
- **Fourteen distinct mid-tone colours** can look muddy next to each other. If the review finds them so, the fallback is a thin separator in the card colour between slices, which keeps slices apart without changing the colours.
- **LiveCharts' global paints.** Tooltip and legend paints are global, not per chart; that is fine with one window but means a variant change must set them again before redrawing. If a tooltip that is open during a switch keeps the old colours, it is closed on the switch (spec: edge cases).
- **Headless rendering with Skia** needs the native Skia library, which is in the output on Windows; the tool runs on Windows, in CI too. LiveCharts animates by default; the tool switches animations off and renders after layout, so captures are complete.
- **Fluent palette coverage.** Some built-in controls (the choice box's drop-down, scroll bars) take colours from the Fluent theme rather than from our resources; setting the Fluent palettes from `Palette` covers most of them, and the screenshots show what's left.
- **Shadows** cost rendering time on large windows; the cards' shadows are kept soft and small.
- **Following Windows live** is only checked by hand, since a test can't change Windows' setting.

## Verification

| Spec criterion | How it is checked |
|---|---|
| Every colour, text size, rounding and spacing comes from the shared look | The "only resources" check and its failing fixture (automated) |
| Contrast in both looks | Palette contrast tests for every pair the spec names (automated) |
| Key figures large, labels muted | Screenshots, reviewed |
| Incoming amounts in the positive colour | View model test that marks incoming rows (automated); screenshots |
| The ring: total in its centre, colours by category in both looks, hover, click | Slice colour tests (automated); headless test for the inner radius and colours; screenshots; clicking is the spike's existing behavior, tested |
| The stepped balance line with a fading fill, hover | Headless test for the series' paints; screenshots |
| Selected row in the accent; one primary button | Screenshots |
| Messages styled and readable in both looks | Contrast tests for error and positive colours; screenshots of both messages |
| Following Windows while running | Hand check |
| "Svetlý" and "Tmavý" ignore Windows; back to following | Headless test of the variant set for each choice (automated) |
| A change of look keeps selection, typed text and statement | Headless test (automated) |
| The choice survives a restart | `SettingsStore` and `MainViewModel` tests (automated) |
| A missing or damaged setting | `SettingsStore` tests (automated) |
| Everything still works in both looks | The existing tests, plus the headless tests run in both variants |
| One command for screenshots; approved | The tool; the committed approved set |
| Privacy checks, no new reviewed entries | `dotnet test`; the dependency check covers the new tool |

Run everything with `dotnet build` and `dotnet test` from the repo root.
