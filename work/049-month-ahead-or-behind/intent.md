---
status: in-progress
issue: 49
---

# Intent: See whether the month ended ahead or behind

Issue: [#49](https://github.com/mDubovsky/DocuSpend/issues/49)

## Problem
The header on "Výpis" shows "Výdavky spolu" and the opening and closing balances. To know whether the month left them ahead or behind, the user has to work out income minus spending themselves.

The balances don't answer it either. They also move with money that is neither spending nor income, such as transfers between the user's own accounts or investments. A month can end with a lower balance only because money went to savings.

## Desired outcome
- **One number answers the question.** The header on "Výpis" shows income minus spending for the statement's period, beside "Výdavky spolu".
- **Its colour gives the answer.** Green when the month ended ahead or even, red when it ended behind.
- **It counts what the totals already count.** Spending is the same as in "Výdavky spolu" ([glossary](../../docs/glossary.md), "Spending"). Income is everything in categories with the role "Príjmy". Categories with the role "Mimo výdavkov" are left out, as they are from "Výdavky spolu" today.
- **It follows the user's changes.** When a rule, a hand choice or an undo moves money between spending, income and "Mimo výdavkov", the number and its colour follow at once, as "Výdavky spolu" does.
- **Readable in both looks.** The new colours are as readable as the existing ones, in light and dark.

## Success criteria
- With a statement open, the header on "Výpis" shows income minus spending beside "Výdavky spolu".
- A statement whose income is larger than its spending shows a positive number with a plus sign, in green; one whose spending is larger shows a negative number in red; one where they are equal shows zero in green.
- Beside the number, a help mark explains that it is income minus spending, without "Mimo výdavkov".
- Money in a category with the role "Mimo výdavkov" doesn't change the number.
- After adding or changing a rule, categorizing a transaction by hand, or undoing a hand choice, the number and its colour are updated.
- The new colours pass the contrast check in both looks.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)).
- The maintainer opens a real statement and the number matches income minus spending worked out by hand, as a hand check.

## Non-goals
- A separate income total in the header, or an income chart.
- Comparing months, or any period other than the statement's own.
- Leaving a single transaction out of the number while it keeps its own category. Today the user does that with a category with the role "Mimo výdavkov", or with "Zaradiť" → "Len túto transakciu".
- Changing what spending, income or "Mimo výdavkov" mean, or what "Výdavky spolu" shows.

## Decisions
- **Income minus spending, not the change in balance**, as the issue says: the balance also moves with own transfers and investments, which are neither.
- **"Mimo výdavkov" is left out**, as the issue says, the same way it is left out of "Výdavky spolu" today.
- **Uncategorized money counts by its direction:** uncategorized money in as income, uncategorized money out as spending. That is how the totals already count them, so the number and "Výdavky spolu" agree.
- **Green ahead, red behind**, as the issue says.
- **Zero is green**, decided by the maintainer: a month that spent no more than it earned did not end behind.
- **A help mark says how the number is made**, agreed by the maintainer: a short help beside it, like the role help on "Kategórie", saying it is income minus spending without "Mimo výdavkov". The user needs to know why it differs from the change in balance.
- **The Slovak label is left to the spec**, agreed by the maintainer, with "Bilancia" as the first candidate: it is short and fits beside "Výdavky spolu". Other candidates from the issue: "Výsledok mesiaca", "Príjmy mínus výdavky".
- **A positive number has a plus sign** ("+120,00 €"), agreed by the maintainer, so the answer doesn't rest on colour alone, which matters for colour-blind users.

## Open questions
None.
