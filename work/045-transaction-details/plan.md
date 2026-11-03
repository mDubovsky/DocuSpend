# Plan: See all of a transaction's details in the list

Spec: [spec.md](spec.md)

## Approach

No new dependency, package or file format. Everything is built from what the projects already use (.NET SDK 10.0.100, Avalonia 11.3.22). The privacy lists don't change, and nothing new is saved.

### Choices to know about first
- **The list's own toggle selection does the opening and folding.** The transactions `ListBox` gets `SelectionMode="Single,Toggle"`. Avalonia then folds the open row on a second click, and opens or folds it with Space or Enter. Up and Down still move the selection. There is no click or key handling of our own, except Escape (below).
- **Escape goes through the window's existing `EscapeCommand`.** It already closes the box on top. With no box and no question open, it now clears `SelectedTransaction`, which folds the row.
- **The details are worked out in screen logic, not in the window.** `TransactionRow` gains `Details`, a list of label–value pairs already in the spec's order, with the missing ones left out. The window only draws them. All the rules about what to show and in what order are tested without a window.
- **The open row keeps the selected row's look** (the accent tint and the corner radius 20 of `Look.Radius.Pill`). If a tall open row looks wrong with that radius, the screenshot review decides, and the fallback is the card radius for a selected row in this list only.

### Verified before planning (2026-11-03)
I checked these with a throwaway headless test in `DocuSpend.Desktop.Tests`, deleted afterwards, on Avalonia 11.3.22:
- A plain `Single` ListBox does not fold the selected row on a second click. `Single,Toggle` does. In Toggle mode, Space and Enter on the focused row both open and fold it, and Down moves the selection as before.
- A `KeyBinding` for Escape on the window fires while a `ListBoxItem` has the focus. The list doesn't swallow Escape.
- When a row near the bottom is selected and grows, the list scrolls so that the whole grown row is in view, with no code of ours. A 111-pixel row selected at index 28 of 30 in a 300-pixel list ended fully visible.
- `ColumnDefinition.SharedSizeGroup` with `Grid.IsSharedSizeScope` gives every row's label column the width of the longest label.
- `TextWrapping="Wrap"` breaks a long run with no spaces at the column's edge (80 × "A" in a 90-pixel column wrapped to several lines).
- A headless click must land inside the row's content. At 5 pixels from the left edge it hit the list behind the row, so window tests click at the row's text.
- Samples: `03-awkward-month` has a transfer whose description is cut at about 40 characters and whose payment purpose is 140 characters long. It is the screenshot with a wrapped purpose, and the case where "Účel platby" shows beside "Popis". In `02-busy-month`, most purposes equal the description.

### Screen logic (`DocuSpend.Presentation`)
- **`TransactionDetails.cs`** (new): `TransactionDetail(string Label, string Value, bool StartsGroup)`, and `TransactionDetails.Of(Transaction)`.
  - It returns the spec's table in order, leaving out every detail that is null or empty.
  - `Účel platby` shows only when it differs from the description, compared exactly.
  - `Typ transakcie` uses `Texts.TypeName` for a known type and `PrintedType` for an unknown one.
  - The card is shown as `**** ` plus its digits.
  - Dates go through `SlovakFormat.Date`. The other values are shown as read.
  - `StartsGroup` is true on the first detail shown of each group after the first group, so the window can space the groups.
- **`Texts.cs`**: the sixteen labels, each a constant beside the others for the transactions card. Where a label is already used (`KindType`, `KindCounterpartyAccount`), the new constant reuses it so the two can't drift.
- **`StatementViewModel`**: `TransactionRow` gets `IReadOnlyList<TransactionDetail> Details`, filled where the rows are built. For the 70 rows of the busiest sample this costs nothing noticeable.
- **`MainViewModel.Escape`**: after the box and the question, a third branch clears `Statement.SelectedTransaction`.

### The window (`DocuSpend.Desktop/MainWindow.axaml`)
- `TransactionList` gets `SelectionMode="Single,Toggle"`.
- The row template's `Grid` gets a second row. It holds an `ItemsControl` named `TransactionDetails` (class `details`, `Grid.IsSharedSizeScope="True"`) in columns 1 to 4, so the details start under the description.
- Each detail is a two-column `Grid`: the label column is `Auto` with `SharedSizeGroup="DetailLabel"`, the value column is `*`. The label is a `TextBlock.muted`, and the value is a `TextBlock` with `TextWrapping="Wrap"`. The class `groupStart` comes from `StartsGroup`.
- Visibility follows the row-action pattern already in `Window.Styles`: `ItemsControl.details` is hidden, and shown under `ListBoxItem:selected`. A row that isn't selected lays out as it does today.
- **`Look.axaml`** gets the details' spacing: the gap between label and value, the space above a group, and the space above the first detail. These are new `Look.*` resources only if the existing `Look.Space.*` and `Look.Margin.*` don't fit. No new colour is needed: muted text on the accent tint is already used and tested in the selected row.

### Alternatives considered
- **A handler of our own for clicks and keys on the list.** It isn't needed: Toggle mode does exactly what the spec asks (verified above), and our own pointer handling would have to tell apart clicks on "Zaradiť".
- **An `Expander` in each row.** It would add its own arrow and header, bring Fluent colours that need restyling ([CONTRIBUTING.md](../../CONTRIBUTING.md)), and give the row a second "open" state beside selection. The intent asks for one.
- **Details in a panel beside or under the list** instead of in the row. The intent and spec chose the row, so the details stay next to the transaction they belong to.
- **Working out the details in the window with converters.** That puts rules that need tests (order, omissions, the purpose rule) where only window tests reach them.

### Layout
```
src/DocuSpend.Presentation/TransactionDetails.cs     new: TransactionDetail, TransactionDetails.Of
src/DocuSpend.Presentation/StatementViewModel.cs     TransactionRow.Details
src/DocuSpend.Presentation/MainViewModel.cs          Escape folds the open row
src/DocuSpend.Presentation/Texts.cs                  the labels
src/DocuSpend.Desktop/MainWindow.axaml               toggle selection, the details in the row template
src/DocuSpend.Desktop/Look/Look.axaml                the details' spacing
tests/DocuSpend.Presentation.Tests/TransactionDetailsTests.cs   new
tests/DocuSpend.Presentation.Tests/StatementViewModelTests.cs, MainViewModelTests.cs, NothingKeptTests.cs  extended
tests/DocuSpend.Desktop.Tests/TransactionDetailsWindowTests.cs  new
tools/DocuSpend.Screenshots/Screens.cs               three new screens
```

## Prerequisites
- #22 is merged into `main` (it is: `af636b0`).
- The maintainer reviews the screenshots (step 4) and does the hand check on a real statement (step 5).

## Steps
1. **The details in screen logic**, test first. `TransactionDetailsTests` covers:
   - the full order on a statement built in the test with every detail set;
   - each sample shape: a transfer and a card payment from `02-busy-month`, the payer's reference and the account-only transfer from `03-awkward-month`, the unknown types from `10-new-types-month`, and a fee;
   - "Účel platby" equal to the description, and different from it;
   - the card format, and every known type's Slovak name;
   - `StartsGroup`;
   - for every transaction of every valid sample: each detail it has appears once, with no empty value, and nothing repeats the amount.

   `StatementViewModelTests` checks that the rows carry their details. Sets the intent to `in-progress`.
2. **Escape folds the open row**, test first, in `MainViewModelTests`:
   - with a row selected and no box, Escape clears the selection;
   - with the "Zaradiť" box open, Escape closes the box and the row stays selected;
   - with the deletion question open, Escape closes only the question.

   `NothingKeptTests` gets a test that selects and clears every row of a sample and finds DocuSpend's folder unchanged.
3. **The window**: toggle selection, the details in the row, the spacing in `Look.axaml`. `TransactionDetailsWindowTests` drives the real window headless:
   - a click opens a row, a second click on its text folds it, and a click on another row moves the open row;
   - a mouse click on "Zaradiť" opens the box and leaves the row open, and "Zrušiť" returns to it;
   - Down and Up move the open row, Space and Enter open and fold it, Escape folds it and the focus stays on the row, and Escape with the box open closes only the box;
   - Tab from the list reaches "Zaradiť";
   - a row near the bottom of `02-busy-month`'s longest category is fully in view once opened, and a row taller than the list shows its first line at the top;
   - the labels share one width, and the 140-character purpose of `03-awkward-month` wraps within its column;
   - a folded row is as tall as before this change.

   The look test passes.
4. **Screenshots**, in both looks, added to `tools/DocuSpend.Screenshots`: an open transfer (`02-busy-month`), an open card payment, and the transfer with the long purpose (`03-awkward-month`). The tool's tall window hides scrolling, so one screen also shows the open row at the default 1280 × 940 size. **Stop: the maintainer reviews them.** Changes from the review go into the spec and are built before going on.
5. **Hand check. Stop: the maintainer** opens transactions of each kind on a real statement and finds that what each shows matches the statement, nothing is missing and nothing shows twice.
6. **Finish** with the `finish-item` skill:
   - AGENTS.md: the open row is the selected row, through the list's toggle selection;
   - the glossary, if "open row" is wanted as a term;
   - README, if it lists features;
   - the intent set to `done`.

## Risks and unknowns
- **A click on "Zaradiť" also toggles the row.** The button handles the press, so the list shouldn't see it, but this wasn't probed. Step 3's mouse-click test catches it. The fallback is marking the press as handled on the row's buttons.
- **Avalonia's own scrolling puts the bottom of a row taller than the list in view**, hiding its first line. If step 3's test shows that, the window brings the row's first line into view after the selection changes (`BringIntoView` with the first line's rectangle).
- **The pill radius on a tall open row looks odd.** The screenshot review decides. The fallback is the card radius for a selected row in this list (a style in `Look.axaml`).
- **Labels make the details column too wide in a narrow window.** "Referencia transakcie" is the longest label. If the review finds values squeezed at the minimum window width, the label column gets a maximum width and long labels wrap. That would change the spec's "labels don't wrap", so the spec changes with it.
- **Keyboard focus is lost when Escape clears the selection.** The step-3 test checks that the focus stays on the row. If it doesn't, the window gives the focus back to the row's container.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| Nothing open when a category is chosen; a click opens a row with its details | `TransactionDetailsWindowTests`, `StatementViewModelTests` |
| A click on another row moves the open row | `TransactionDetailsWindowTests` |
| A click on the open row outside its buttons folds it, nothing selected | `TransactionDetailsWindowTests` |
| "Zaradiť" opens the box, the row stays open; "Zrušiť" returns to it | `TransactionDetailsWindowTests` |
| "× Zrušiť zaradenie" on an open row works as before | `TransactionDetailsWindowTests`, the existing `CategorizeWindowTests` |
| Another category, a rule, a hand choice or an undo folds the row | the existing `CategorizeTests` (selection cleared), `StatementViewModelTests` |
| A row near the bottom is fully in view once opened | `TransactionDetailsWindowTests` |
| Down and Up move the open row | `TransactionDetailsWindowTests` |
| Escape folds; Space or Enter opens again | `TransactionDetailsWindowTests`, `MainViewModelTests` |
| Escape with the "Zaradiť" box open closes only the box | `TransactionDetailsWindowTests`, `MainViewModelTests` |
| Tab reaches "Zaradiť" (and "× Zrušiť zaradenie") | `TransactionDetailsWindowTests` |
| A full transfer's details, in order and with the labels | `TransactionDetailsTests` |
| A card payment: the card format, no counterparty lines | `TransactionDetailsTests` |
| "Účel platby" only when it differs | `TransactionDetailsTests` |
| The payer's reference (`03-awkward-month`) | `TransactionDetailsTests` |
| An account-only transfer shows only its lines | `TransactionDetailsTests` |
| An unknown type shows the printed type | `TransactionDetailsTests` |
| A fee: its code and symbols, no repeated amount | `TransactionDetailsTests` |
| Each known type's Slovak name | `TransactionDetailsTests` |
| Every sample transaction: each detail once, no empty label | `TransactionDetailsTests` |
| A long value wraps and is shown in full | `TransactionDetailsWindowTests`; screenshot |
| Shared look only | the existing look test |
| Slovak texts | `TextsTests` |
| Opening, folding and moving write nothing | `NothingKeptTests` |
| Privacy checks, no new entries | `DocuSpend.Privacy.Tests` |
| Screenshots approved | step 4, maintainer |
| Hand check on a real statement | step 5, maintainer |
