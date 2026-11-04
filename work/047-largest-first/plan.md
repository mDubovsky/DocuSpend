# Plan: See the largest transactions and categories first

Spec: [spec.md](spec.md)

## Approach

No new dependency, package, file format or saved data. The change is two orderings in screen logic, `StatementViewModel` in `DocuSpend.Presentation`, built on what is there (.NET SDK 10.0.100, Avalonia 11.3.22). The privacy lists don't change.

### Choices to know about first
- **The order is worked out in screen logic only.** The categorization (`Categorizer`, `CategorizedStatement.Categories`) keeps the catalog's order, since the saved file, the slice colours and other screens follow it. Only the two lists `StatementViewModel` hands to the "Výpis" cards are sorted.
- **No window code for scrolling.** The categories list already scrolls to its selected row whenever the selection moves, so the chosen category comes into view after a re-sort with nothing added (verified below). A window test holds that in place.
- **The first screenshot changes content.** `tools/DocuSpend.Screenshots` shows the first category of the card; that is now the largest one, not the first in the catalog. The screenshots are reviewed (step 3).

### Verified before planning (2026-11-04)
I checked these with a throwaway headless test in `DocuSpend.Desktop.Tests`, deleted afterwards, on Avalonia 11.3.22:
- A `ListBox` bound like the categories card (items and selected item from a view model, `AutoScrollToSelectedItem` left at its default, `true`) scrolls to the selected row after its items are replaced by a re-sorted list in which that row moved from the top to the bottom. A 30-row list 120 pixels high ended scrolled to the bottom with the row in view.
- After the items are replaced by equal records, the list keeps an equal item selected.
- On "Výpis" at the default window size, `02-busy-month` has 12 categories in a card that shows about 7 (content 432 pixels in a 276-pixel view), so the card does scroll and the spec's "scrolls to show it" is tested with that sample.

In the code:
- `CategorizedStatement.Categories` follows `catalog.Categories`, the stored order, where the user's own categories come after the uncategorized buckets. `CatalogOrder.Categories` gives the order the other screens use (built-in, the user's own, uncategorized).
- `CategoryRow.Amount` is the total the card shows (the category's balance). `TransactionRow.Index` is the transaction's place in the statement.
- LINQ's `OrderBy` and `ThenBy` are stable, so ties keep the order they had before sorting.

### Screen logic (`DocuSpend.Presentation/StatementViewModel.cs`)
- **Categories:** the categories with transactions, taken in `CatalogOrder.Categories(Config.Catalog)`, then `OrderByDescending(|Amount|)`. The stable sort leaves equal sizes in the catalog order of the screens. A total of 0,00 € is the smallest size, so it comes last with no special case.
- **Transactions:** `OrderByDescending(|Amount|)`, then `ThenBy(BookingDate)`, then `ThenBy(Index)`. The explicit last key keeps the statement's order even if the source order ever changes.
- The doc comments of `Categories` and `Transactions` ("In the catalog's order", "by booking date") say the new order.
- Selection after a change works as today: `Refresh` sets `SelectedRow` to the chosen category's new row, or clears the choice if it has no transactions left.

### Alternatives considered
- **Sorting in the window** (a sorted view over the lists). It puts an order that needs tests where only window tests reach it, and Avalonia 11 has no built-in sorted collection view.
- **Sorting the categorization's own list.** It would change the order the saved file, the slice colours and other screens rely on, which the spec rules out.
- **Keeping the card's old tie order** (the stored catalog). The spec chose the screens' order, since that is the one the user sees elsewhere.
- **Scrolling the chosen category into view in `MainWindow`.** Not needed: the list does it already (verified above).

### Layout
```
src/DocuSpend.Presentation/StatementViewModel.cs                 the two orders
tests/DocuSpend.Presentation.Tests/StatementViewModelTests.cs    the order tests
tests/DocuSpend.Desktop.Tests/MainWindowTests.cs                 the chosen category scrolled into view
```
Existing tests that relied on the date order or on the first category being the catalog's first are updated where they break.

## Prerequisites
- #45 is merged into `main` (it is: `31dd6c0`).
- The maintainer reviews the screenshots (step 3) and does the hand check on a real statement (step 4).

## Steps
1. **The orders in screen logic**, test first, in `StatementViewModelTests`. Sets the intent to `in-progress`.
   - For every valid sample: the categories are by size, largest first; each category's transactions are by size, then booking date, then the statement's order (this replaces `Selecting_a_category_lists_exactly_its_transactions_in_booking_date_order`, which keeps checking that exactly the category's transactions are listed).
   - Built in the test: money in larger than money out comes first (+1 850 before −732,40); −500 before +200 before −50; equal sizes on different dates, earlier first, out and in alike; equal sizes on the same date, in the statement's order; a 0,00 € transaction last.
   - Categories of the same size in the screens' order, including a user's own category tied with an uncategorized bucket (the user's own first, unlike the stored catalog); an uncategorized bucket placed by its size among the others; a category whose payment and refund cancel out comes last.
   - The same statement shown twice gives the same order.
   - After adding a rule, a hand choice and an undo, the new order shows and the chosen category stays chosen with its transactions; a chosen category emptied by a rule is no longer chosen.
   - The `CategorizedStatement` keeps the catalog's order, and the slices stay by spending.

   Existing tests elsewhere that break because they took the first row or the first category are updated to pick what they mean by name or by property.
2. **The window**, in `MainWindowTests`: with `02-busy-month`, choose the largest category at the top of the card, move all but its smallest transaction elsewhere by hand so it drops below the visible part of the card, and find it chosen and in view. The existing window tests for the open row and "Zaradiť" (`TransactionDetailsWindowTests`, `CategorizeWindowTests`) pass in the new order, and the look test passes.
3. **Screenshots** in both looks, with `dotnet run --project tools/DocuSpend.Screenshots`. **Stop: the maintainer reviews them** against the spec's sketch. Changes from the review go into the spec and are built before going on.
4. **Hand check. Stop: the maintainer** opens a real statement and finds the largest categories and transactions at the top of each card, and the order unchanged on the "Kategórie" screen and in the rule form.
5. **Finish** with the `finish-item` skill:
   - AGENTS.md: the "Výpis" cards are ordered by size in `StatementViewModel`, and the categorization's own order is untouched;
   - README, if it describes the lists;
   - the intent set to `done`.

## Risks and unknowns
- **A test or screenshot relies on the old order** (the first category, the first transaction). Step 1 and 2 find them by running the whole suite; they are changed to name what they need, never to sort back.
- **The re-sort doesn't scroll the card in the real window**, unlike the probe (for instance because the selection is set before the new items are laid out). Step 2's test catches it. The fallback is calling `ScrollIntoView` on the categories list in `MainWindow` when `SelectedRow` changes.
- **Found in the build: a small move doesn't test the scrolling.** Moving one large transaction only took a category four rows, which stayed in sight, and the window test passed with the list's scrolling switched off. Rules on the description couldn't move more, since the sample's payments share descriptions. The test now moves transactions by hand, takes the chosen category from row 0 to row 10 of 12, first checks that the new row lies outside the visible part, and fails when `AutoScrollToSelectedItem` is switched off (checked once by hand). No window code was needed.
- **Found in the build:** two tests picked rows by their place in the date order (`UnknownTypesTests`, `CategorizeTests`). They now pick by the transaction's place in the statement.
- **Found in the screenshot review:** nothing to change. The first screenshot now opens on "Príjmy", the largest category of `01-typical-month`, with its one transaction; the maintainer accepted it as it is.
- **Found in the hand check:** nothing. On a real statement, the largest categories and transactions were at the top of each card, and the "Kategórie" screen and the rule form kept their order.
- **Equal records keep the old selection object.** After a change that leaves the chosen category's count and total the same, its row is equal to the old one and no change is raised. The probe showed the list keeps it selected; step 1's test checks the view model side.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| Categories by the size of their total, largest first | `StatementViewModelTests` (every valid sample) |
| More money in comes before less money out | `StatementViewModelTests` |
| Uncategorized buckets placed by size | `StatementViewModelTests` |
| Equal-size categories in the screens' order | `StatementViewModelTests` |
| A 0,00 € category last | `StatementViewModelTests` |
| Transactions by size, out and in mixed | `StatementViewModelTests` (every valid sample, and built in the test) |
| Equal sizes, different dates: earlier first | `StatementViewModelTests` |
| Equal sizes, same date: the statement's order | `StatementViewModelTests` |
| The same order every time | `StatementViewModelTests` |
| After a rule, a hand choice or an undo: new order, same category chosen | `StatementViewModelTests` |
| A chosen category that moves out of sight is scrolled into view | `MainWindowTests` |
| A chosen category left empty leaves the card, nothing chosen | `StatementViewModelTests` |
| The open row with mouse and keyboard, in the new order | the existing `TransactionDetailsWindowTests` |
| "Zaradiť" and "× Zrušiť zaradenie" in the new order | the existing `CategorizeWindowTests`, `CategorizeTests` |
| "Kategórie" screen, rule form, "Zaradiť" choices, pie, saved file keep their order | `StatementViewModelTests` (categorization and slices), the existing `CategoriesTests`, `RuleFormViewModelTests`, `RuleStoreTests` |
| Rules and categories from earlier versions load the same | the existing saving tests (`rules-v1.json` to `rules-v3.json`) |
| Privacy checks, no new entries | `DocuSpend.Privacy.Tests` |
| Screenshots reviewed | step 3, maintainer |
| Hand check on a real statement | step 4, maintainer |
