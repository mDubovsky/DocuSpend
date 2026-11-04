# Spec: See the largest transactions and categories first

Intent: [intent.md](intent.md)

## Behavior

### Size
- An item's **size** is its amount without its sign: a transaction's amount, or a category's total (the balance the card shows beside it, [glossary](../../docs/glossary.md)). −500,00 € and +500,00 € are the same size.
- Sizes are compared exactly, to the cent, as the statement gives them.

### The "Kategórie" card on "Výpis"
- The card lists the categories that have transactions, as today, ordered by size, largest first. Money out and money in are mixed: −500,00 € comes before +200,00 €, and +200,00 € before −50,00 €.
- The uncategorized buckets ("Nezaradené výdavky" and the uncategorized income) are ordered by size like every other category.
- A category whose transactions add up to 0,00 € (a payment and its refund) comes last.
- Categories of the same size keep the order the other screens use: built-in categories, then the user's own, then the uncategorized buckets, each in the catalog's order.
- What a row shows doesn't change: the name, the number of transactions and the total.

### The "Transakcie" card on "Výpis"
- The chosen category's transactions are ordered by size, largest first, money out and money in mixed the same way. Ordering by date goes away.
- Transactions of the same size are ordered by booking date, earliest first, and those of the same date as the statement lists them.
- What a row shows doesn't change, and nor does the open row ([#45](../045-transaction-details/spec.md)): selecting, folding, the arrow keys, Escape, Space, Enter and Tab work as before, in the new order. "Zaradiť" and "× Zrušiť zaradenie" ([#22](../022-categorize-from-list/spec.md)) work as before.

### When amounts move
- After anything that changes the categories (adding, editing or deleting a rule, a hand choice, undoing one, a change on the "Kategórie" screen), both cards take their new order straight away.
- The category the user chose stays chosen, wherever it moves in the card, and its transactions stay listed. If it moved out of sight, the card scrolls as little as needed to show it. If it has no transactions left, it leaves the card and nothing is chosen, as today.
- The open row folds, as any change to the list already folds it ([#45](../045-transaction-details/spec.md)).

### What else keeps its order
- The "Kategórie" screen, the category and sub-category choices in the rule form on "Pravidlá" and in the "Zaradiť" box, the rules on "Pravidlá", the pie chart and its legend (already by spending, largest first), the balance chart and its hover cards, and what DocuSpend saves don't change.

## Screens

Nothing new is added; only the order of rows changes. The two cards on "Výpis", with "Bývanie" chosen (amounts made up):

```
+------------------------------------+  +----------------------------------------------------+
| Kategórie                          |  | Transakcie  Bývanie                                |
|  Príjmy                 2  +1 850  |  |  3. 5. 2026  PLATBA NÁJMU    Nájom      -650,00 €  |
| [Bývanie                4   -732 ] |  |  1. 5. 2026  PLATBA ZA BYT   Ostatné     -82,40 €  |
|  Stravovanie           11   -412   |  |  5. 5. 2026  NÁKUP NÁBYTKU  Vybavenie   -45,00 €  |
|  Nezaradené výdavky     3   -260   |  | 20. 5. 2026  VRATKA PREPLATKU Ostatné   +45,00 €  |
|  Auto-moto              5    -98   |  |                                                    |
|  Nezaradené príjmy      1    +12   |  |                                                    |
+------------------------------------+  +----------------------------------------------------+
```

In "Transakcie", −45,00 € and +45,00 € are the same size, so the one booked earlier comes first.

## Acceptance criteria

### Order of categories
- [ ] Given a statement, when it opens, then the "Kategórie" card on "Výpis" lists its categories by the size of their total, largest first.
- [ ] Given a category with money out and one with more money in, then the one with more money in comes first (e.g. +1 850,00 € before −732,40 €).
- [ ] Given uncategorized transactions, then each uncategorized bucket is placed by its size among the other categories.
- [ ] Given two categories of the same size, then they come in the order the "Kategórie" screen uses (built-in, the user's own, uncategorized).
- [ ] Given a category whose total is 0,00 €, then it comes last.

### Order of transactions
- [ ] Given a chosen category, then its transactions are listed by the size of their amount, largest first, money out and money in mixed (−500,00 € before +200,00 € before −50,00 €).
- [ ] Given two transactions of the same size on different dates, then the earlier one comes first, whether they go out or come in.
- [ ] Given two transactions of the same size on the same date, then they come in the order the statement lists them.
- [ ] Given the same statement opened twice, then both cards show the same order both times.

### When amounts move
- [ ] Given a chosen category, when a rule, a hand choice or an undo changes the totals, then both cards show the new order and the same category is still chosen with its transactions listed.
- [ ] Given a chosen category that moves out of sight in the card, then the card scrolls to show it.
- [ ] Given a chosen category left with no transactions, then it leaves the card and nothing is chosen.

### Unchanged
- [ ] The open row opens, folds and moves with the mouse and the keyboard (Up, Down, Escape, Space, Enter, Tab) as in #45, in the new order.
- [ ] "Zaradiť" and "× Zrušiť zaradenie" work from a row in the new order.
- [ ] The "Kategórie" screen, the category choices in the rule form and in "Zaradiť", the pie chart's slices and legend, and the saved file keep their order.
- [ ] Rules and categories saved by an earlier version load and categorize the same way.
- [ ] Nothing in this item adds networking, telemetry or an unreviewed dependency (the privacy checks pass).

### By hand
- [ ] The maintainer reviews screenshots of "Výpis" in both looks.
- [ ] The maintainer opens a real statement and finds the largest categories and transactions at the top of each card (hand check).

## Edge cases and errors
- **One category, one transaction** (sample 05): the cards show it alone, as today.
- **No transactions** (sample 04): both cards are empty, as today.
- **Many transactions of the same size** (e.g. several fees of 0,50 €): they come by date, then in the statement's order, every time.
- **A transaction of 0,00 €**, if a statement has one: it comes last in its category.
- **An unknown transaction type** ([#41](../041-unknown-transaction-kinds/spec.md)): ordered by its amount like any other.
- Ordering can't fail and saves nothing; there is no new message.

## Out of scope
- Choosing or switching the order, or sorting by a column.
- Ordering anything outside the two cards on "Výpis".
- Searching or filtering the lists.
- Changing what a category's total means or what the rows show.

## Decisions
- **A category's size is the total the card shows**, as the intent says, not its spending or its money out alone: the user can check the order against the number beside each name. So a category with a large payment and a nearly equal refund sits low, as its small total says.
- **Equal sizes among categories follow the order of the other screens** (built-in, the user's own, uncategorized), not the order the card used before. Before, the card followed the catalog as stored, where the user's own categories come after the uncategorized buckets; the order the other screens show is the one the user knows.
- **Equal sizes among transactions: earliest date first, then the statement's order**, as the intent says. Earliest first, because that is how the card read before.
- **The chosen category is scrolled into view when it moves**, so a change made from the list doesn't leave the user looking at a card where their category has disappeared from sight.

## Open questions
None.
