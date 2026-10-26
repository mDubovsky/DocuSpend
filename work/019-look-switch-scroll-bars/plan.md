# Plan: The look switch and scroll bars follow the look

Spec: [spec.md](spec.md)

## Approach

Everything happens in the shared look: `Look/Look.axaml` for sizes and styles, `Palette.cs` for three new colours. No screen changes, no new dependency, no version change (Avalonia 11.3.22, as today). The screenshots are the review.

### Why the switch changes shape (measured 2026-10-26)
A throwaway headless test (not committed) measured the look switch on the start screen and with `01-typical-month` open:

| | Switch height | Chosen pill height | Text's top inside the pill |
|---|---|---|---|
| Start screen | 26 | 20 | 5 |
| Statement open | 34 | 28 | 5 |

The header's row grows when "Otvoriť výpis…" appears. The switch sits in a horizontal stack, which stretches it to the row's height, and its text stays at the top. That is the off-centre text and the changed pill. (The headless platform's font is smaller than Windows', so the numbers differ from the real window, but the behaviour is the same.)

**Fix:** `ListBox.segmented` gets `VerticalAlignment="Center"`, so it keeps its own height. Its items get `VerticalContentAlignment="Center"`, so the text stays centred even if a pill is ever taller than its text. This also covers the rules card's switch, which uses the same style.

### The switches' hover
- A new palette colour, **`SwitchHover`**, for a choice under the pointer that isn't chosen: `ListBox.segmented ListBoxItem:pointerover` takes it. The chosen choice keeps `AccentTint` under the pointer, which the existing `ListBoxItem:selected:pointerover` rule already does.
- It needs its own colour because the switch's background is `Hover`, the tone list rows use on hover. On the switch that tone would be invisible.
- Proposed values (measured with the project's own contrast formula, 2026-10-26):

| | `SwitchHover` | Against the switch (`Hover`) | Against the chosen pill (`AccentTint`) | Text on it |
|---|---|---|---|---|
| Light | `#DCDFEA` | 1.21 | 1.08, and grey rather than blue | 12.5 |
| Dark | `#3A3F63` | 1.41 | 1.15, and grey rather than blue | 8.8 |

  For comparison, the chosen pill against the switch is 1.12 (light) and 1.23 (dark) today. A test pins `SwitchHover` at least 1.15 from `Hover`, and text on it at 4.5. The exact tones are part of the screenshot review.

### Scroll bars (Avalonia 11.3.22's Fluent theme, read from its source at tag `11.3.22` on 2026-10-26)
- Fluent's `ScrollBar` has arrow buttons and a thumb that shrinks to a thin line until the pointer comes near (`IsExpanded`). When it expands, the arrows show. Its colours come from Fluent's own resource keys. Overriding those keys can't remove the arrows, so DocuSpend replaces the scroll bar's look as a whole: a **`ControlTheme` for `ScrollBar`** in `Look.axaml`'s resources, found by type like Fluent's own. Its template is a `Track` only: transparent page buttons on each side of the thumb (so clicking the track still pages) and a rounded `Thumb`. There are no arrow buttons and nothing shrinks. One template for vertical and horizontal, since `Track` takes the orientation.
- **Always visible, in its own strip.** Fluent's `ScrollViewer` lays its content *over* the scroll bar only while `AllowAutoHide` is true (`^[AllowAutoHide=True]` gives the content both columns). With `AllowAutoHide` false, the content keeps column 0 and the scroll bar gets its own `Auto` column. That column is empty when nothing overflows: with `VerticalScrollBarVisibility="Auto"`, `ScrollBar` hides itself while `Maximum` is 0. So:
  - a `ScrollViewer` style sets `AllowAutoHide="False"`. It reaches the window's, the legend's, the rule lists' and the choice boxes' pop-up lists' scroll viewers, whose templates don't bind the property;
  - a `ListBox` style sets `ScrollViewer.AllowAutoHide="False"`, because `ListBox`'s template binds its inner scroll viewer's `AllowAutoHide` to that attached property, and a template binding outranks a style.
- **Colours:** the handle is a new palette colour, **`ScrollThumb`**. It becomes `Muted` under the pointer and while dragged ("stronger"). The track is `Hover`, the faint tone the look already has. Proposed `ScrollThumb` values, measured 2026-10-26:

| | `ScrollThumb` | On the card | On the window's background | On the track (`Hover`) |
|---|---|---|---|---|
| Light | `#7E84A2` | 3.68 | 3.34 | 3.3 |
| Dark | `#7077A0` | 3.72 | 4.18 | 3.4 |

  `Muted` already exceeds 4.5 on the card in both looks. Tests pin the handle at least 3 to 1 on the card, the background and the track.
- **Sizes**, new resources in `Look.axaml`: `Look.Size.ScrollBar` 12 (the strip), `Look.Size.ScrollThumb` 6 (the handle's thickness, centred in the strip), `Look.Size.ScrollThumbMin` 32 (the handle's shortest length, so a long list's handle stays easy to grab), `Look.Radius.ScrollThumb` 3 (fully round ends).
- **What changes besides the bars:** a list that scrolls is 12 units narrower inside than before, because the bar no longer lies over it. Screenshots of every screen will show that.

### Alternatives considered
- **Overriding Fluent's scroll bar resource keys** (`ScrollBarSize`, `ScrollBarThumbBackgroundColor`, …). Lost: the arrow buttons still appear once the bar expands, and DocuSpend's look would depend on Fluent's internal key names.
- **Keeping Fluent's auto-hiding bar over the content.** Lost: the spec decided on an always-visible bar in its own strip, so right-aligned amounts stay readable.
- **Replacing only the template with a style** instead of a `ControlTheme`. Lost: Fluent's theme styles for `IsExpanded` would still reach into the new template, for example by setting the thumb's `RenderTransform`. A `ControlTheme` replaces the whole theme, so nothing of Fluent's scroll bar is left to interfere.
- **Fixing the header's layout in `MainWindow.axaml`** (centring the switch's stack panel there). Lost: the cause is the switch's own style, which also serves the rules card's switch, and the look file is where #11 put such fixes.

### Layout
No new files in `src/`. Changed:
- `src/DocuSpend.Presentation/Look/Palette.cs`: `SwitchHover`, `ScrollThumb`.
- `src/DocuSpend.Desktop/Look/Look.axaml`: the switch's alignment and hover, the scroll bar's `ControlTheme`, `AllowAutoHide`, the sizes.
- `tests/DocuSpend.Presentation.Tests/LookTests.cs`: the new pairs.
- `tests/DocuSpend.Desktop.Tests/LookSwitchWindowTests.cs` (new) and `ScrollBarWindowTests.cs` (new).
- `tools/DocuSpend.Screenshots/Screens.cs`: three new screens.
- `work/019-look-switch-scroll-bars/screenshots/`: the reviewed screenshots.

## Prerequisites
- None to merge: `main` already has #20.
- The maintainer reviews the screenshots (step 4) and does the hand check (step 5).

## Steps
1. **The switch keeps its shape.** Set `status: in-progress` in the intent. Test first in `LookSwitchWindowTests`: on the start screen and with a statement open, in each look, the look switch has the same height and the chosen pill the same size, and the pill's text is centred in it (its top and bottom gaps within one unit of each other). The same for the rules card's switch with a statement open. Then the alignment in `Look.axaml`. Commit.
2. **The switches react to the pointer.** Tests first: `LookTests` (Presentation) adds `Text` on `SwitchHover` at 4.5 and `SwitchHover` against `Hover` at 1.15, in both looks. `LookSwitchWindowTests` moves the headless pointer onto an unchosen choice of each switch, checks that its pill takes `SwitchHover`, then moves the pointer away and checks that the pill is gone. The chosen choice under the pointer keeps `AccentTint`. Clicking a choice still sets the look and the shown rules (the existing tests stay). Then the colour and the style. Commit.
3. **Scroll bars.** Tests first: `LookTests` adds `ScrollThumb` on `Card`, `Background` and `Hover` at 3, in both looks. `ScrollBarWindowTests`, with `02-busy-month` open and the built-in rules shown:
   - every `ScrollBar` in the window, and in an opened choice box's list, uses the look's template: it has no arrow buttons, its handle has the look's thickness and colour, and its `AllowAutoHide` is off;
   - a list that overflows shows its bar without the pointer; a list that fits shows none;
   - a list that scrolls is narrower inside by the strip, so no row reaches under the bar;
   - the mouse wheel, dragging the handle, and pressing the track below the handle each move the list down;
   - the handle takes `Muted` under the pointer;
   - switching the look while a list is scrolled recolours its handle and keeps the scroll position.
   
   Then the palette colour, the `ControlTheme`, the `AllowAutoHide` styles and the sizes in `Look.axaml`. Run the whole suite, including the look test and the privacy checks. Commit.
4. **Screenshots. Review stop.** Add to `Screens.cs`: `15-look-switch-hover` (statement open, pointer over "Podľa systému", which is never chosen in the captures), `16-rules-switch-hover` (pointer over "Vstavané pravidlá"), and `17-scroll-bar` (`02-busy-month`, the transaction list scrolled part way, pointer on its handle). Run the tool and copy every screen in both looks to `work/019-look-switch-scroll-bars/screenshots/`. Compare `00-start` against `01-statement`, and every screen against `work/011-modern-look/screenshots/` and #20's. Commit, then **stop for the maintainer's review** of the switch's shape, the hover tones and the scroll bars. Adjust and re-shoot if asked.
5. **Hand check. Review stop.** The maintainer opens a real statement, in both looks, and checks the switch, both switches' hover and the scroll bars, including a choice box's list. Then `finish-item`: the intent becomes `done`, and AGENTS.md, the glossary and this plan are brought up to date if the build changed anything.

## Risks and unknowns
- **The `ControlTheme` isn't found from `Look.axaml`'s resources** (for example in the choice box's pop-up, which is its own visual root). The step 3 test that checks every scroll bar, including the pop-up's, finds it. Fallback: register the theme in the application's resources from `LookResources.cs`, still defined in the look.
- **The headless platform can't drag or page a scroll bar**: its window API does have `MouseDown`, `MouseMove`, `MouseUp` and `MouseWheel` (checked in `Avalonia.Headless` 11.3.22 on 2026-10-26). But the non-Skia platform the window tests use may not lay out the thumb. Fallback: drive the `Thumb`'s drag events and the page buttons' clicks directly, and check by hand in step 5.
- **The narrower lists change other screens.** Expected and small (12 units). Step 4 compares every screen, not only the new ones.
- **The hover and handle tones look wrong on screen** despite passing the numbers. That is what the step 4 review is for; the tests pin minimums, not exact values.
- **Display scaling.** The bar's sizes are in the same device-independent units as the rest of the look; checked by hand in step 5 if the maintainer's screen scales.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| The look switch's chosen pill has the same size on both screens, text centred, in each look | `LookSwitchWindowTests` (step 1); screenshots `00-start`, `01-statement` (step 4) |
| With a statement open, the switch is no taller than on the start screen and centred in the header row | `LookSwitchWindowTests` (step 1) |
| A choice under the pointer, not chosen, shows a pill differing from the switch's background; it goes when the pointer leaves | `LookSwitchWindowTests`; `LookTests` (`SwitchHover` vs `Hover`) (step 2); screenshots `15`, `16` |
| The chosen choice under the pointer keeps its accent pill | `LookSwitchWindowTests` (step 2) |
| Clicking a choice still changes the look or the shown rules | Existing `LookTests.The_choice_in_the_header_sets_the_applications_look` and the rules window tests; `LookSwitchWindowTests` (step 2) |
| Every scroll bar takes its look from the shared look; the look test finds no literal in screens | `ScrollBarWindowTests` (every bar, including a choice box's list) (step 3); existing `LookCheckTests` |
| The handle has at least 3 to 1 against the card behind it | `LookTests` (Presentation), `ScrollThumb` on `Card`, `Background`, `Hover` (step 3) |
| A list that overflows shows its bar without the pointer; one that fits shows none | `ScrollBarWindowTests` (step 3) |
| A list that scrolls is narrower by the strip; the bar covers no row | `ScrollBarWindowTests` (step 3) |
| Each list scrolls with the wheel, by dragging the handle and by clicking the track | `ScrollBarWindowTests` (step 3); hand check (step 5) |
| Changing the look while scrolled or hovering repaints in the new look and keeps the position | `ScrollBarWindowTests` (step 3) |
| Screenshots of the switch on both screens, each switch under the pointer, and a list with its bar; approved | Screenshot tool, review stop (step 4), by the maintainer |
| Hand check with a real statement, both looks | Review stop (step 5), by the maintainer |
| All privacy checks pass, no new entries in the reviewed lists | `dotnet test` (every step); no file in `privacy/` changes |
