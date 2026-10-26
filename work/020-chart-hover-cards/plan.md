# Plan: Charts' hover cards are always whole and always there

Spec: [spec.md](spec.md)

## Approach

Both charts stop using the chart library's own hover card. DocuSpend draws its own two cards and the dot, as ordinary Avalonia controls in a layer over the whole window, styled from `Look.axaml`. The chart library still says *what* is under the pointer; DocuSpend decides what the card says and where it goes. No new dependency and no version change: Avalonia 11.3.22 and LiveCharts 2.0.5, as today (2.0.5 is LiveCharts' newest stable release; checked on NuGet on 2026-10-26).

### Why the cards fail today (verified 2026-10-26)
- **Clipped ring card.** In LiveCharts 2.0.5 the default hover card (`SKDefaultTooltip`) is a shape drawn on the chart's own canvas, so nothing of it can appear outside the chart's 260 × 260 area. No setting moves it out.
- **Missing balance card.** The library's card attaches to data points, and the balance line has points only on the period's first day, on booking days, and on the last day (`StatementViewModel.Balances`). Its finding strategies choose the nearest point, which would show another day's date and balance, not the day under the pointer.

### Verified in a throwaway project (scratchpad, 2026-10-26, not committed)
On the headless platform with Skia (as the screenshot tool runs), with `02-busy-month` open:
- `PieChart.HoveredPointsChanged` still fires with the library's card turned off (`TooltipPosition = Hidden`), and names the slice under the pointer. Its answer matches each slice's drawn angles at 12 positions round the ring, and matches `GetPointsAt`. At four positions exactly on the ring's horizontal and vertical axes, both gave a wrong or empty answer. That is a quirk of the library at exact multiples of 90°. A pointer rarely lands there, and when it does, the card and the slice that moves out still agree, because both come from the same event.
- `CartesianChart.ScalePixelsToData` and `ScaleDataToPixels` convert the pointer's x into a date and back exactly. `CoreChart.DrawMarginLocation` and `DrawMarginSize` give the plotting area.
- The date axis is padded beyond the period (for May: 28 April to 2 June), so the pointer can be on a day outside the period. Such positions belong to the nearest day of the period (see "The day under the pointer").
- On the headless platform *without* Skia, which the window tests use, the charts are not laid out: slices have no shapes and nothing can be hovered. So the window tests drive the hover handling by its inputs (a slice, a position), and real hovers are rendered by the screenshot tool, which uses Skia.
- `02-busy-month` has bookings on the period's first day (1 May), so the first-day case is covered by a sample.

### Daily balances (`DocuSpend.Presentation`)
- `StatementViewModel.BalancePoints` becomes **one point per day of the period**, each the end-of-day balance: the opening balance plus every transaction booked up to and including that day. The line is drawn from these points and the card reads them, so the card can't disagree with the line. The stepped line looks as before. The one visible change is the one the spec decided: a booking on the 1st shows on the 1st, without a separate point at the opening balance.
- `BalancePoint` gets the texts the card shows (`DateText`, `BalanceText`), formatted by `SlovakFormat` as everywhere else.
- **The day under the pointer**: `BalanceDays.At(DateTime pointer, period)` takes the pointer's position as a date and time, and returns the day whose stretch of the line it is on. A stepped line holds a day's balance from that day's mark until the next day's, so the day is the date part of the position, with positions before the first day or after the last one clamped to the period. The dot then sits at the start of the flat stretch under the pointer, on the line.

### Where a card goes (`DocuSpend.Presentation`)
`HoverCardPlacement.Place(anchor, cardSize, area, gap)` is plain geometry with its own small types, no Avalonia. By default the card goes to the right of and below the anchor (the pointer, or the dot), with a gap. If it would cross the area's right edge it goes to the left; if it would cross the bottom it goes above. Finally it is moved fully inside the area. The area is the window's content less a margin from the look. Unit tested on its own.

### The cards in the window (`DocuSpend.Desktop`)
- **Library cards off:** `TooltipPosition="Hidden"` on both charts. `Charts.Tooltip` and the code that sets the tooltip paints go.
- **A hover layer:** a `Canvas` (`HoverLayer`, not hit-testable) in the window's root `Panel`, above the scrolling content and below the delete dialog and the drop overlay. It holds:
  - `RingCard`: the category name in one line with `TextTrimming="CharacterEllipsis"`, then "amount (share)", which isn't trimmed.
  - `BalanceCard`: the date, then "Zostatok" and the amount.
  - `HoverDot`: a small circle.
  Each card's `MaxWidth` is the layer's width less the margin, so a name is shortened only when the window has no room for it.
- **The ring:** `HoveredPointsChanged` gives the slice, and the pointer position comes from `PointerMoved`. The window shows `RingCard` for that slice, placed at the pointer. When no slice is hovered (including over the hole) it hides the card.
- **The balance:** on `PointerMoved` over `BalanceLine`, if the pointer is inside the plotting area, the window takes the pointer's date (`ScalePixelsToData`) and the day under it (`BalanceDays.At`). It places `HoverDot` at that day's balance (`ScaleDataToPixels`) and places `BalanceCard` next to the dot. Outside the plotting area, it hides both.
- **Hiding:** both cards and the dot are hidden when the pointer leaves a chart, when the shown statement changes, and when the window's content scrolls (the card would otherwise stay where the pointer was).
- **Testable inputs:** the window's handlers call two internal methods, `ShowRingCard(PieSlice, Point)` and `ShowBalanceDay(DateOnly)`. The window tests call these directly, since the charts can't be hovered there (see above).
- **The look:** new styles in `Look/Look.axaml`:
  - `hoverCard`: the card colour, the control rounding, padding, a soft shadow and a hairline edge.
  - `hoverDot`: the accent fill with a ring in the card's colour, so it stands out on the fill under the line.
  - The layer's margin, as a resource.
  Everything uses `DynamicResource`, so switching the look recolours an open card. The look check covers the new XAML as it is.

### Screenshots (`tools/DocuSpend.Screenshots`)
New screens, each opening `02-busy-month` and moving the headless mouse (`window.MouseMove`) to a point the window computes from the chart's own layout:
- `13-ring-card-<edge>`: the middle of a slice on the ring's left, right, top and bottom edges. The top-right one is "Elektronika, komunikácia a médiá", the longest built-in name with spending in that sample.
- `14-balance-card-gap`: a day without bookings.
- `14-balance-card-busy`: a day with several bookings.

These are real hovers through the library, so they also check that the wiring works end to end.

### Alternatives considered
- **Keep the library's card and enlarge the chart's area.** The card would still be drawn inside the chart, and the ring's square is fixed by #11 so a long legend can't shrink it.
- **A custom library card (`IChartTooltip`) shown as an Avalonia `Popup`.** A popup is a separate window on Windows, so it can go outside DocuSpend's window (the spec keeps it inside), and the screenshot tool captures only the window, so it couldn't be reviewed. The library only hands it data points either, so the balance would still need the day logic.
- **A point for every day plus the library's "closest x" finding.** It would choose the *nearest* day rather than the one whose stretch the pointer is on. It would show no dot, and the balance card could still be clipped at the chart's edges.
- **Our own hit test for the ring from the slices' angles.** It is possible, but it would duplicate the library's and could disagree with the slice that moves out on hover.

### Layout
- `src/DocuSpend.Presentation/BalanceDays.cs`: the day under the pointer.
- `src/DocuSpend.Presentation/HoverCardPlacement.cs`: where a card goes.
- `src/DocuSpend.Presentation/StatementViewModel.cs`: daily `BalancePoints` with texts.
- `src/DocuSpend.Desktop/MainWindow.axaml` and `.axaml.cs`: the hover layer and its handling.
- `src/DocuSpend.Desktop/Charts.cs`: tooltip paints removed.
- `src/DocuSpend.Desktop/Look/Look.axaml`: `hoverCard`, `hoverDot` and the margin.
- `tests/DocuSpend.Presentation.Tests/BalanceDaysTests.cs` and `HoverCardPlacementTests.cs`, plus changes in `StatementViewModelTests.cs`.
- `tests/DocuSpend.Desktop.Tests/HoverCardWindowTests.cs`, plus a change in `LookTests.cs`.
- `tools/DocuSpend.Screenshots/Screens.cs`: the new screens.

## Prerequisites
None: #24 is merged into `main`, and this branch starts from it. The maintainer reviews the screenshots (step 4) and does the hand check (step 5).

## Steps
1. **Daily balances**, test-first. `BalancePoints` gets one point per day with its texts. Tests:
   - For every sample that reads, on every day, the balance is the opening balance plus every booking up to and including that day.
   - The last day shows the closing balance.
   - The 1st of `02-busy-month` shows the balance after its bookings.
   - A day after a day with several bookings carries the balance over.
   - In `05-single-transaction-month`, the balance changes on 14 November.
   - In `04-empty-month`, every day shows the opening balance.

   The existing balance-line test is replaced. Set the intent to `in-progress`.
2. **The day under the pointer and where a card goes**, test-first:
   - `BalanceDays.At`: the start, the middle and the end of a day; before the first day and after the last.
   - `HoverCardPlacement.Place`: the default spot; flipping at the right and at the bottom; a card larger than the area pinned to its top-left; never outside the area.
3. **The cards in the window:**
   - Turn the library's cards off; add the hover layer, the two cards, the dot and their styles; add the handlers and the hiding.
   - Window tests, through `ShowRingCard` and `ShowBalanceDay`:
     - The texts shown.
     - The card stays inside the window near each edge.
     - A long user category name is trimmed and the amount isn't.
     - At 1280 × 880 (the size DocuSpend opens at), the card has room for every built-in category name.
     - The dot and the card follow the look after a switch.
     - Everything is hidden after a new statement is opened.
     - The library's cards are off.
   - `LookTests` stops checking the tooltip paints and checks the new styles instead.
4. **Screenshots** of the new screens in both looks. **Stop: the maintainer reviews them** against the spec's sketches, with `01-statement` and `02-busy-month` compared against `work/011-modern-look/screenshots/` to see that nothing else changed.
5. **Stop: hand check by the maintainer.** On a real statement, in both looks, hover the ring all the way round, including the smallest slices, and move along the whole balance line, including over the axis padding at both ends. Every card should be whole, and every day should have a card and a dot.
6. **Finish** with `finish-item`: the approved screenshots of the new screens go into `work/020-chart-hover-cards/screenshots/`, `AGENTS.md` and `CONTRIBUTING.md` are updated only if a command or a testing note changed, and the intent is set to `done`.

## Risks and unknowns
- **The library's hover misses at the ring's exact axes** (seen in the throwaway check). Fallback, if the hand check finds it noticeable: find the slice from its drawn start and sweep angles, which the check showed are exact, and drive the slice's move-out from the same answer.
- **Pointer events while a card covers the pointer.** The hover layer isn't hit-testable, so the chart keeps getting `PointerMoved`. Checked in the screenshots and the hand check.
- **The dot at a step.** The dot sits where the line rises or falls to the day's balance. If the review finds that it reads as sitting on the wrong level, the fallback is to place it in the middle of the day's flat stretch, while the card stays the same.
- **Scrolling with the pointer over a chart.** The card hides on scroll and comes back on the next pointer move. This is covered by a window test of the hiding, and the rest by the hand check.
- **Display scaling.** Positions are in Avalonia's device-independent units, both from the chart and in the layer, so scaling doesn't change the placement. This is part of the hand check if the maintainer's display is scaled; otherwise it is a known gap, noted in the finish.

- **Found in the build:** the charts aren't laid out on the headless platform the window tests use, so a hover can't be simulated there, as the plan expected. The window tests give the window a slice or a day and a position instead, and the screenshot tool, which draws for real, covers the real hovers.
- **Found in the screenshot review:** the first ring screenshots hovered the wrong slices, because the ring starts at 3 o'clock, not at the top. The angles in the screenshot tool were corrected and the screenshots drawn again; the application was not affected. The balance card also hides a small dip in the line where it sits over it, which is expected for a card.
- **Found in the hand check:** nothing. The risk of the library missing a slice at the ring's exact axes did not show up, so its fallback was not needed.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| Any slice of `02` shows a whole card with name, amount and share, both looks | Screenshots `13-ring-card-*`, reviewed (step 4); hand check (step 5) |
| A slice near the chart's frame: the card extends past it | Screenshots `13-ring-card-left/right/top/bottom`, reviewed |
| A slice near the window's edge: the card moves inwards, stays whole | `HoverCardPlacementTests` (automated); window test of the card near each edge (automated) |
| A name too long for the room ends in an ellipsis; amount and share whole | Window test with a long user category name in a narrow window (automated) |
| At the opening size, no built-in name is shortened | Window test over every built-in category name at 1280 × 880 (automated); screenshot with the longest name, reviewed |
| Clicking a slice still selects its category | Existing slice-click test (automated); hand check |
| Every day's balance = opening + bookings up to that day, every sample | `StatementViewModelTests` over every sample that reads (automated) |
| A day without bookings shows the earlier booking day's balance | `StatementViewModelTests` (automated) |
| Before the first booking: opening balance; last day: closing balance | `StatementViewModelTests` (automated) |
| A day with several bookings shows the balance after all of them | `StatementViewModelTests` (automated); screenshot `14-balance-card-busy` |
| A booking on the first day shows that day's balance after it | `StatementViewModelTests` on `02` (automated) |
| Anywhere over the plotting area, at any height: the day's card and an accent dot | `BalanceDaysTests` (automated); window test of `ShowBalanceDay` (automated); screenshot `14-balance-card-gap`; hand check |
| Moving to another day moves both; leaving the area hides both | Window tests of moving and hiding (automated); hand check |
| The empty month: empty message, opening balance every day | Existing empty-month tests; `StatementViewModelTests` on `04` (automated) |
| Cards and dot follow the look; a switch leaves nothing old | Window test after a look switch (automated); screenshots in both looks |
| The look check passes | Existing look check (automated) |
| Hand check on a real statement, both looks | Maintainer (step 5) |
| Privacy checks pass, no new entries | `dotnet test` (automated); no dependency changes in this item |
