---
status: done
issue: 45
---

# Intent: See all of a transaction's details in the list

Issue: [#45](https://github.com/mDubovsky/DocuSpend/issues/45)

## Problem
A row in a list of transactions on "Výpis" shows only a few things about a transaction: its date, description, sub-category and amount. The statement often says much more: the counterparty's name and account, the variable, constant and specific symbols, the payment purpose, the payer's reference, the card, the value date, a fee. To recognise a transaction ("what was this payment?"), the user has to open the statement itself and look it up.

Knowing what a transaction was is the first step to categorizing it well, by hand or with a rule ([#22](../022-categorize-from-list/intent.md)). Today that step happens outside DocuSpend.

## Desired outcome
- **The details are in the list.** The user opens a transaction in a list on "Výpis" and sees, in that row, everything DocuSpend read about it from the statement, without leaving the screen.
- **Only what the transaction has.** A detail the statement doesn't give for this transaction isn't shown, not even as an empty label. A card payment shows its card; a transfer shows its account and symbols.
- **Each detail is named.** Every detail has a short Slovak label ("Protistrana", "Účet protistrany", "Variabilný symbol", …), always in the same order, so the user finds the same thing in the same place on every transaction.
- **It fits with categorizing.** The open row still offers "Zaradiť", and "× Zrušiť zaradenie" for a transaction categorized by hand, so the user can read the details and then categorize the transaction from the same place.
- **Closing it is as easy as opening it.** The user folds the row back to its usual size, or opens another transaction instead.
- **It works with the keyboard** as well as the mouse.
- **It looks like the rest of DocuSpend**, in the light and the dark look.
- **Nothing new is kept.** Showing the details saves nothing; what DocuSpend keeps stays as [PRIVACY.md](../../PRIVACY.md) says ("What DocuSpend keeps").

## Success criteria
- A user opens a transaction on "Výpis" and sees each detail DocuSpend read for it, with its label, in the agreed order.
- A detail the transaction doesn't have is not shown; a transaction with only a date, a type and an amount (some fees) still opens, showing just those.
- The open row offers "Zaradiť" (and "× Zrušiť zaradenie" for a hand choice), and both work as before.
- The user can fold the open row back, and opening another transaction folds the first.
- Everything above can be done with the keyboard alone.
- A long detail (a payment purpose, a long counterparty name) can be read in full.
- Screenshots of the open row in both looks are reviewed by the maintainer.
- Nothing DocuSpend saves changes because a transaction was opened.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)).
- The maintainer opens transactions of a real statement and finds that what each shows matches the statement, as a hand check.

## Non-goals
- Reading more from a statement than the reader reads today. That is a separate item.
- Editing a transaction's details.
- Showing which rule put a transaction in its category ([#22](../022-categorize-from-list/intent.md) leaves it out too).
- Showing the details anywhere else: in the "Zaradiť" box, on "Pravidlá", or in the hover cards of the charts ([#20](../020-chart-hover-cards/intent.md)).
- Searching or filtering the list by a detail.
- Copying a detail out of the list (see Decisions).

## Decisions
- **Only details the reader already reads**, as the issue says. Reading more is its own item, with its own checks against real statements.
- **Showing details keeps nothing**, as the issue says, so PRIVACY.md and [decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md) don't change.
- **Every detail the reader reads, grouped by what the user asks first**, agreed by the maintainer: who (counterparty name, account, the bank's BIC), what for (payment purpose, the three symbols, payer's reference), how (type, card, fee), when (booking date, value date), then the bank's own codes (purpose category, reference). "Who" and "what for" are what recognises a payment; the bank's codes rarely do, but the issue asks for everything. The exact labels and order go in the spec.
- **A long text wraps and is shown in full**, agreed by the maintainer. The point is to read what the statement says; a cut-off purpose would send the user back to the statement.
- **One row is open at a time, and the open row is the selected row**, agreed by the maintainer. The selected row already carries "Zaradiť"; making selection and opening the same thing keeps one idea, and a list with several large rows is hard to scan.
- **The open row follows the selection**, agreed by the maintainer: the arrow keys move from transaction to transaction with the details showing, and Escape folds the open row. Reading transactions one after another is the common case, and it needs no new keys to learn.
- **Copying a detail is not in this item**, agreed by the maintainer. It is useful, but clicking the row folds it, and making the text selectable at the same time needs its own design; it can be a follow-up issue.

## Open questions
None.
