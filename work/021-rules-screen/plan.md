# Plan: Move the rules off the statement screen

Spec: [spec.md](spec.md)

## Approach

**No new dependency, and no change to what is saved.** Everything is built from what DocuSpend already uses: Avalonia 11.3.22 with its Fluent theme, LiveChartsCore.SkiaSharpView.Avalonia 2.0.5 and CommunityToolkit.Mvvm. `SavedRules`, `RuleStore` and the privacy lists don't change. The work is a new choice in the screen logic and a rearranged window.

Measured on 26 October 2026, on this branch:
- **Today's statement screen doesn't fit 1280 × 880 with five rows per list.** In the #19 screenshot of `02-busy-month`, drawn with Skia and the real fonts, the header card is 166 points tall and each chart card 378. The cards are 18 apart and the window margin is 22. A list row is 43 points apart from the next, and a list card needs about 88 points around its rows. The lists card would start at y = 602 and have 256 points: about four rows. **The charts go from 300 to 240 points** (the ring from 260 to 220), which gives about 316 points and five rows with a little to spare. The screenshot review settles the final number.
- **Filling the window and scrolling below a minimum both work in Avalonia 11.3.22.** In a throwaway headless test, a `Border` inside the window's `ScrollViewer` had its `Height` bound to the scroll viewer's `Bounds.Height` and a `MinHeight` of 700. Its `*` row then filled the window at 880 and 1200 high, with nothing to scroll. At 520 high it kept 700 and scrolled.
- **The window tests can't measure "fits" with real fonts.** They run with headless drawing, whose text measures differently (a row is 36 points there, 43 with Skia). The window tests therefore check the arrangement in their own terms: nothing to scroll, and room for five of the rows they measure. The real-font check is the screenshot review and the hand check.
- **LiveCharts leaves a chart empty after it has been hidden.** The window already works around this for the start screen: once the charts are visible again, it asks both to update after two frames (`MainWindow.axaml.cs`). Switching back to "Výpis" hides and shows them the same way, so the same update runs then.

### The screen logic, in `DocuSpend.Presentation`
- **`AppScreen { Statement, Rules }` and `AppScreenOption(AppScreen, Label)`**, with `Texts.StatementScreen = "Výpis"` and `Texts.RulesScreen = "Pravidlá"`. They are shaped like `RuleView` and `RuleViewOption`.
- **`MainViewModel`:**
  - `Screens` (the two options) and `SelectedScreen`, "Výpis" at start;
  - `ShowsStatement` (`HasStatement` and "Výpis") and `ShowsRules` (`HasStatement` and "Pravidlá"), for the window to bind to;
  - `OpenFileAsync` sets "Výpis" when a statement is shown, and leaves the choice as it is when a file fails. That covers the button, Ctrl+O, a drop and the example, which all go through it.
- **Nothing else moves.** The form, the rules, the selection and the scroll positions live in view models or controls that stay alive while hidden, so switching keeps them by itself. Tests prove it.

### The window, in `DocuSpend.Desktop`
- **The header card** (both screens):
  - Its first row is a grid of three columns. On the left a `WrapPanel` holds "Otvoriť výpis…", the period, the example badge and "Výpis sa číta…"; in the middle is the switch; on the right a `WrapPanel` holds "Vzhľad" and the look switch. At the default width this is one line; in a narrow window the side groups wrap inside their columns instead of cutting a control off.
  - The switch is `ListBox.segmented` named `ScreenSwitch`, like `LookSwitch`, shown while `HasStatement`. It gets the #19 hover with no new style.
  - The messages stay where they are. The key figures show only while `ShowsStatement`.
- **The content fills the window.** The outer `ScrollViewer` stays. Inside it, a `Border` carries the window margin as padding, has its `Height` bound to the scroll viewer's height, and has a `MinHeight` from the look (`Look.Height.ContentMin`). Below that height the window scrolls, as the spec allows.
- **Two screens in the same row**, each shown by its binding:
  - `StatementScreen`: a grid with the charts (`Auto`) and the lists (`*`). The two lists lose their `MinHeight` and `MaxHeight` and fill their cards.
  - `RulesScreen`: one `*` row, with the form card on the left (top-aligned, as tall as its fields) and the rules card on the right (full height). `ScrollViewer.ruleList` loses its maximum height and fills the card.
  - The start screen stays as it is.
- **Sizes into the look.** The chart and ring sizes that are literals in `MainWindow.axaml` today become `Look.Height.Chart` (300) and `Look.Size.Ring` (260) in `Look/Look.axaml`, with `Look.Height.ContentMin`. `Look.Height.RuleList` goes.
- **Switching screens** hides the charts' hover cards (`HideHoverCards`). On returning to "Výpis", it runs the existing two-frame chart update, which becomes one method used for both the start screen and the switch.
- **Nothing else changes:** the deletion pop-up, the drop overlay, Escape and Ctrl+O cover the whole window on both screens, as today.

### Tests and screenshots
- The window tests that use the form or the rules card today (`MainWindowTests`, `RulesWindowTests`, `RuleEditorWindowTests`, `LookSwitchWindowTests`, `ScrollBarWindowTests`) choose "Pravidlá" first. A hidden control can't be clicked or seen, so they would fail otherwise. That is the point: it shows the rules really moved.
- The screenshot tool draws at **1280 × 940**, the default size, instead of 1280 × 1660, so the pictures show what fits. The rules screens (`04`, `05`, `07`–`12`, `16`) choose "Pravidlá". New screens:
  - `18-rules-screen`: a few rules, the form empty;
  - `19-screen-switch-hover`: the pointer on "Pravidlá".

### Alternatives considered
- **Key figures in the header's first row**, to keep the charts at 300: lost. That row already holds the switch, the button, the period, the badge and the look switch. The spec keeps the key figures where they are.
- **A `TabControl`** for the two screens: lost. Its headers would need restyling to look like the other switches. A segmented `ListBox` is already styled, hovers (#19), and is tested.
- **Two windows, or a pop-up for the rules:** lost in the intent. The rules act on the open statement, and switching should lose nothing.
- **Recreating each screen on switching** (a `ContentControl` with templates): lost. It would throw away scroll positions and the controls' state, which the spec keeps. Hiding keeps them for free.
- **Sizing the content in code on every resize:** not needed. The binding was verified to work, and keeps the arrangement in XAML.
- **Checking the fit with real fonts in the window tests** (Skia instead of headless drawing): lost. The fonts on the Linux CI machine differ from Windows', so the check would test the wrong fonts; the screenshot review on Windows checks the real ones.

### Layout
No new folders. Changed files:
- `src/DocuSpend.Presentation/MainViewModel.cs`, `Texts.cs`
- `src/DocuSpend.Desktop/MainWindow.axaml`, `MainWindow.axaml.cs`, `Look/Look.axaml`
- `tools/DocuSpend.Screenshots/Screens.cs`, `Program.cs`
- tests in `tests/DocuSpend.Presentation.Tests` (`ScreensTests.cs`, new; `TextsTests.cs`) and `tests/DocuSpend.Desktop.Tests` (`ScreensWindowTests.cs`, new; the window tests above)
- docs: `README.md` and `AGENTS.md` where they describe the screen, `work/021-rules-screen/`

## Prerequisites
- #24 is merged into `main` (it is), and this branch was made from it.
- The maintainer reviews the screenshots (step 4) and does the hand check with a real statement (step 5).

## Steps
Test-first for logic; one commit per step, committed when it is done and every test passes.

1. **The choice of screen** (`MainViewModel`, `Texts`). Tests:
   - "Výpis" at start;
   - choosing either screen;
   - `ShowsStatement` and `ShowsRules` false on the start screen;
   - opening a file, the example, and a drop each show "Výpis" from "Pravidlá";
   - a failed file and a wrong drop leave "Pravidlá" chosen;
   - the form, a rule being edited and the rules card's view are kept across a switch and an open;
   - the two texts.

   Sets the intent to `in-progress`.
2. **Two screens in the window** (`MainWindow.axaml`, `.cs`). The window tests that use rules choose "Pravidlá" first. New `ScreensWindowTests`:
   - no switch on the start screen;
   - the switch in the header with a statement;
   - each screen shows its own cards and none of the other's;
   - the header shows the same controls on both;
   - Ctrl+O and a drop from "Pravidlá" show "Výpis";
   - a failed open stays on "Pravidlá" with the message;
   - the open and rules-not-loaded messages show on both screens, and "Zmeny pravidiel sa teraz neukladajú." only on "Pravidlá";
   - after switching away and back, both charts have their series and a measured size;
   - a hover card shown before switching is gone;
   - the selected category, its transactions, both lists' scroll offsets, a half-typed rule and "Vstavané pravidlá" survive a round trip.

   `LookSwitchWindowTests`' hover theory gets the screen switch.
3. **Fit the default window** (fill-height content, the sizes in the look). Window tests:
   - at 1280 × 940, for samples `01` to `05` in both looks, the window has nothing to scroll, and each list's height holds at least five of its rows (`02` with its longest category selected, so the transactions list overflows and scrolls inside);
   - at 1280 × 1200 the lists are taller and the window still has nothing to scroll;
   - at 720 × 520 the window scrolls, and every header control lies whole inside the content's width;
   - on "Pravidlá", the rules list fills its card, and with 200 rules it scrolls inside it.

   The look test passes with the new resources.
4. **Screenshots.** **Stop for review:** the maintainer looks at every screen in both looks, at the default size, against `work/019-look-switch-scroll-bars/screenshots/`, and approves the chart height, the five rows, the rules screen and the switch's hover. Fixes go in this step. The approved pictures of the two screens are committed to `work/021-rules-screen/screenshots/`.
5. **Hand check.** **Stop:** the maintainer opens a real statement, switches between the screens, adds and edits a rule on "Pravidlá", sees "Výpis" follow it with its selection kept, and sees "Výpis" fit the default-sized window without scrolling.
6. **Docs and done**, with `finish-item`:
   - `README.md` and `AGENTS.md` where they describe the screen;
   - the spec and this plan updated for anything the build changed;
   - the intent becomes `done`.

## Risks and unknowns

- **Found in the screenshot review: nine categories didn't fit the legend of a 240-point chart.** The ninth row sat under the legend's scroll bar. The maintainer kept the lists as they were and asked for a bigger window instead: the charts are back at 300 points (the ring at 260), the default window is 1280 × 940, and `Look.Height.ContentMin` is 820. The hover-card screenshots' positions now follow the ring's own size.
- **A month with more than nine categories** still scrolls the legend inside its own area, as it did before this item.
- **The hover-card screenshots (`13`, `14`) point at fixed places in the charts**, and smaller charts move them. Fallback: adjust the positions in `Screens.cs` in step 4; the hover-card window tests compute their positions from the charts and shouldn't change.
- **A scroll offset may be reset when a hidden list is measured again.** The window test in step 2 catches it. Fallback: the window remembers each list's offset on leaving "Výpis" and restores it on return.
- **Binding the content's height to the scroll viewer could flicker or loop on resize.** It didn't in the headless test, and the hand check covers a real window. Fallback: set the height in `SizeChanged`.
- **Tests that click hidden controls fail with unclear messages.** Each test that uses rules chooses "Pravidlá" in its own setup helper, so the reason is in one place.
- **Found in the build: the header's right group lost its vertical centring.** Moving "Vzhľad" and the look switch into a wrapping panel put them at the top of the row, which `LookSwitchWindowTests` caught. Both groups are centred vertically again, and the side groups wrap inside their columns so the header fits at the smallest window size.
- **Found in the build: the old window tests used rule controls that are now on the other screen.** Seven test classes (the form, the rules card, the look switches, the scroll bars, a drop onto the form) choose "Pravidlá" first, through `Headless.ShowRules`. The scroll bar test now checks each screen's bars in turn.
- **Found in the screenshot review: the switch's place and the legend.** The maintainer moved the switch to the middle of the header, with "Otvoriť výpis…" at the very left and "Vzhľad" at the very right, and asked for the default window to grow instead of the charts to shrink (see the legend bullet above). The spec, intent and this plan describe what was built.
- **Found in the hand check:** nothing; the maintainer approved it as built.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| No switch on the start screen | `ScreensWindowTests` (step 2) |
| A statement or the example opened from the start screen shows "Výpis" | `ScreensTests` (step 1), `ScreensWindowTests` (step 2) |
| Choosing "Pravidlá" and "Výpis" switches; choosing the chosen one changes nothing | `ScreensTests`; `ScreensWindowTests` (steps 1, 2) |
| The header shows the same controls in the same places on both screens | `ScreensWindowTests`: same controls visible, same bounds (step 2) |
| Opening from "Pravidlá" by button, Ctrl+O and drop shows "Výpis" | `ScreensTests` (button through the picker, drop); `ScreensWindowTests` (Ctrl+O, drop) (steps 1, 2) |
| The statement screen has the key figures, charts and lists, and no rule parts | `ScreensWindowTests` (step 2) |
| 1280 × 940, samples `01`–`05`, both looks: nothing to scroll, five rows per list | window test in step 3; screenshots (step 4) and hand check (step 5) with real fonts |
| More than fits: the window doesn't scroll, the lists do | window test with `02`'s longest category (step 3) |
| A taller window: more rows, nothing to scroll | window test at 1280 × 1200 (step 3) |
| The charts' hover cards work as before | `HoverCardWindowTests`, unchanged and passing (every step) |
| The rules screen has the form and the rules card, and no statement parts | `ScreensWindowTests` (step 2) |
| #24's window criteria hold on "Pravidlá" | `RuleEditorWindowTests`, `RulesWindowTests`, run on "Pravidlá" (step 2) |
| Adding, editing and deleting a rule on "Pravidlá" updates the count and "Výpis" | `ScreensTests` (step 1); `ScreensWindowTests` reading the charts and lists after switching back (step 2) |
| The selected category, its transactions and the lists' scroll positions survive a round trip | `ScreensWindowTests` (step 2) |
| A category emptied by a rule is no longer selected, with "Vyberte kategóriu v grafe alebo v zozname." | `ScreensTests` (step 1) |
| A half-typed rule and a rule being edited survive a round trip | `ScreensTests`; `ScreensWindowTests` (steps 1, 2) |
| "Vstavané pravidlá" survives a round trip | `ScreensTests`; `ScreensWindowTests` (steps 1, 2) |
| A file not opened: its message shows on both screens until closed | `ScreensWindowTests` (step 2) |
| Rules not loaded: the message shows on both screens until closed | `ScreensWindowTests` with a damaged rules file (step 2) |
| A failed open from "Pravidlá" stays there, and the statement is unchanged | `ScreensTests`; `ScreensWindowTests` (steps 1, 2) |
| "Zmeny pravidiel sa teraz neukladajú." and change messages only on "Pravidlá" | `ScreensWindowTests` (step 2) |
| The switch comes from the shared look; no literal values in screens | `LookCheckTests` (every step); hover theory in `LookSwitchWindowTests` (step 2) |
| Privacy checks pass, nothing new in the reviewed lists, nothing saved differently | `dotnet test` (every step); no change to `RuleStore` or `SavedRules` |
| No new text in English | `TextsTests`: "Výpis" and "Pravidlá" letter for letter (step 1) |
| Screenshots of both screens in both looks at the default size, with the hover, approved | maintainer, step 4 |
| Hand check with a real statement | maintainer, step 5 |
| Edge: a smaller window scrolls, the header wraps and nothing is cut off | window test at 720 × 520 (step 3) |
| Edge: a wider window | screenshot review (step 4) |
| Edge: no switching while the deletion pop-up is open | `ScreensWindowTests`: the switch can't be clicked under the scrim (step 2) |
| Edge: a hover card disappears on switching | `ScreensWindowTests` (step 2) |
| Edge: a drop on "Pravidlá" shows the overlay, opens to "Výpis", or stays with the message | `ScreensTests`, `ScreensWindowTests` (steps 1, 2) |
| Edge: a month with no spending still fits | the step 3 window test includes `04-empty-month` |
| Edge: no user rules | `RulesWindowTests`, on "Pravidlá" (step 2) |
| Edge: "Výpis sa číta…" on either screen, and the switch to "Výpis" when read | `ScreensTests` (step 1) |
