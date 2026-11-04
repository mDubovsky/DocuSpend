---
status: planned
issue: 47
---

# Intent: See the largest transactions and categories first

Issue: [#47](https://github.com/mDubovsky/DocuSpend/issues/47)

## Problem
On "Výpis", the "Kategórie" card lists categories in the catalog's order, and a category's "Transakcie" list is ordered by booking date. To find where most of the money went, or came from, the user has to read both lists and compare the amounts by eye.

The big amounts are usually what the user wants to check first: the categories that took most of the money, and within them the large payments, such as rent, salary or an unexpected charge. Today they are wherever the catalog or the calendar happens to put them.

## Desired outcome
- **Largest first, on both cards.** The "Kategórie" card on "Výpis" lists categories by the size of their total, largest first. A category's "Transakcie" list shows its transactions by the size of their amount, largest first.
- **Size, whichever way the money moved.** Money out and money in are compared by size alone: −500 € sits above +200 €, and +200 € above −50 €.
- **Always this order.** There is one order, with nothing to choose or switch. Ordering the transactions by date goes away.
- **The same amount always comes out the same way.** Items of the same size are always in the same order, so the list doesn't shuffle when the user opens the statement again.
- **Nothing else changes order.** The "Kategórie" screen, the categories offered in the rule form and in "Zaradiť", the charts and what DocuSpend saves keep their order.
- **What the user is doing stays put.** The open row ([#45](../045-transaction-details/intent.md)) and "Zaradiť" ([#22](../022-categorize-from-list/intent.md)) keep working. When a change moves amounts (a new rule, a hand choice, an undo), the lists take their new order and the category the user selected stays selected.

## Success criteria
- On "Výpis", the "Kategórie" card lists categories by the size of their total, largest first, whether the total is money out or money in.
- A category's "Transakcie" list shows its transactions by the size of their amount, largest first, money out and money in compared by size alone.
- Items of the same size come out in the agreed order every time the statement is opened.
- The "Kategórie" screen, the category choices in the rule form and in "Zaradiť", the charts and the saved file keep their order.
- After categorizing a transaction, or undoing a hand choice, the cards show the new order and the selected category is still selected.
- The open row and "Zaradiť" work as before in the new order, with the mouse and the keyboard.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)).
- The maintainer opens a real statement and finds the largest categories and transactions at the top, as a hand check.

## Non-goals
- Choosing or switching the order, or sorting by a column.
- Ordering anything outside the two cards on "Výpis".
- Searching or filtering the lists.
- Changing what a category's total means ([glossary](../../docs/glossary.md), "Balance") or what the cards show.

## Decisions
- **Ordered by size, money out and money in alike**, as the issue says: a large salary is as worth seeing as a large payment.
- **Only the two cards on "Výpis" change**, as the issue says. The catalog's own order must stay, since the saved file and the slice colours follow it, and other screens show categories in the catalog's order on purpose.
- **No choice of order**, as the issue says. One order is simpler, and the date is still on every row.
- **A category's size is the size of the total the card shows for it**, agreed by the maintainer. That is the number the user sees beside it, so the order can be checked by eye.
- **The uncategorized buckets are sorted by size like every other category**, agreed by the maintainer. A large uncategorized amount is exactly what the user should notice and fix.
- **Items of the same size: transactions by booking date, then as the statement lists them; categories in the catalog's order**, agreed by the maintainer. Both are orders the user already knows.

## Open questions
None.
