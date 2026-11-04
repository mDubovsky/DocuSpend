# Spec: See whether the month ended ahead or behind

Intent: [intent.md](intent.md)

## Behavior

### The number
With a statement open, the header of the window shows a new key figure, "Bilancia", right after "Výdavky spolu" and before "Počiatočný zostatok". It is in the header, so it is there on all three screens ("Výpis", "Pravidlá", "Kategórie"), like the other key figures.

Its value is income minus spending for the statement:

- **Spending** is exactly the number "Výdavky spolu" shows ([glossary](../../docs/glossary.md), "Spending"): the balances of every category with the role "Výdavky", including "Nezaradené výdavky", as money out.
- **Income** is the balance of every category with the role "Príjmy", including the uncategorized bucket for money in. Money going out of an income category (a returned payment) lowers it, as money coming back into a spending category lowers spending.
- Categories with the role "Mimo výdavkov" don't count at all, whichever way their money moved.

Put together, "Bilancia" is the sum of the balances of every category that isn't "Mimo výdavkov". It equals the closing balance minus the opening balance, minus the balance of the "Mimo výdavkov" categories.

### How it looks
- The amount is written like the other key figures ("1 234,56 €"), at the size of "Počiatočný zostatok" and "Konečný zostatok"; "Výdavky spolu" stays the largest number.
- A positive amount has a plus sign: "+555,00 €". A negative one has a minus sign, written the way the window already writes negative amounts. Zero is "0,00 €", with no sign.
- Positive and zero are green. Negative is red. Both are the green and red the window already uses (the green of incoming amounts, the red of error messages), so both looks keep one green and one red.
- A help mark ("?") beside the label shows, when pointed at or focused, the text:

  > Príjmy mínus výdavky za obdobie výpisu. Kategórie mimo výdavkov, napríklad prevody medzi vlastnými účtami alebo investície, sa nezapočítajú, preto sa bilancia môže líšiť od rozdielu zostatkov.

  It behaves like the existing help marks (the role help on "Kategórie", the note about unknown transaction types).

### When it changes
"Bilancia" is worked out from the same categorization as "Výdavky spolu", so it changes whenever that does: when a rule is added, changed or deleted, when a transaction is categorized by hand or a hand choice is undone, and when a category is added. Its colour and sign follow the new value at once. Opening another statement shows that statement's value.

## Screens

The header with a statement open (shown on every screen). Only the "Bilancia" figure is new:

```
+--------------------------------------------------------------------------------------------+
| DocuSpend          [ Výpis | Pravidlá | Kategórie ]             [Otvoriť výpis…] [look]    |
|                                                                                            |
| Výdavky spolu        Bilancia (?)         Počiatočný zostatok    Konečný zostatok          |
| 1 345,00 €           +555,00 €            1 500,00 €             1 955,00 €                |
|  (largest)            (green; red when                                    [unknown types ?] |
|                        negative)                                                           |
+--------------------------------------------------------------------------------------------+
```

The help text appears as the usual tooltip under the "?", over the window; nothing else moves. When the window is too narrow, the key figures wrap to a second line as they do today, each label staying with its number.

## Acceptance criteria
- [ ] Given a statement is open, then the header shows "Bilancia" with its amount after "Výdavky spolu" and before "Počiatočný zostatok", on "Výpis", "Pravidlá" and "Kategórie".
- [ ] Given a statement with no "Mimo výdavkov" money, then "Bilancia" equals the closing balance minus the opening balance.
- [ ] Given a statement, then "Bilancia" equals the income total minus "Výdavky spolu", and money in "Mimo výdavkov" categories, in or out, doesn't change it.
- [ ] Given uncategorized money in and out, then the money in counts as income and the money out as spending.
- [ ] Given money going out of an income category, then it lowers income; given money coming back into a spending category, then it lowers spending.
- [ ] Given income larger than spending, then the amount has a plus sign ("+555,00 €") and is green.
- [ ] Given spending larger than income, then the amount has a minus sign and is red.
- [ ] Given income equal to spending, including a month with no transactions, then the amount is "0,00 €", with no sign, and is green.
- [ ] Given the help mark beside "Bilancia", when it is pointed at or reached with the keyboard, then it shows the help text above, exactly.
- [ ] Given a statement is open, when a rule is added, changed or deleted, a transaction is categorized by hand, or a hand choice is undone, so that money moves into or out of a "Mimo výdavkov" category, then "Bilancia", its sign and its colour change with it.
- [ ] Given a value that turns from positive to negative or back, then the colour turns with it, without reopening the statement.
- [ ] The green and the red are readable on the header in both looks (contrast at least 4.5 to 1, checked by the existing contrast test).
- [ ] Screenshots of every screen in both looks show "Bilancia" in the header; the maintainer checks them by hand before the build is finished.
- [ ] Nothing in this item adds networking, telemetry or an unreviewed dependency; the privacy checks pass.
- [ ] Hand check (maintainer): on a real statement, "Bilancia" matches income minus spending worked out by hand from the "Kategórie" card.

## Edge cases and errors
- **Empty month** (no transactions): "Bilancia" is "0,00 €", green, like "Výdavky spolu" showing zero.
- **No income at all:** "Bilancia" is minus "Výdavky spolu", red (or "0,00 €", green, when spending is zero too).
- **Spending below zero** (refunds larger than the month's payments in spending categories): counted as it is, so it raises "Bilancia".
- **Transactions of an unknown type:** they land in an uncategorized bucket unless a rule catches them, and count by their direction like any uncategorized money.
- **Large amounts:** the number is written in full, like the other key figures, and the header wraps rather than cutting it off.
- **A statement that doesn't add up** is rejected by the reader before anything is shown, so "Bilancia" is never shown for one.
- **No statement open:** no header figures, as today.
- Nothing here reads or writes a file, so there is no new failure to report.

## Out of scope
- A separate income total in the header, or an income chart.
- Comparing months, or any period other than the statement's own.
- Leaving a single transaction out of "Bilancia" while it keeps its own category (with a category with the role "Mimo výdavkov" or "Zaradiť" → "Len túto transakciu" it can be done today).
- Changing what "Výdavky spolu", "Počiatočný zostatok" or "Konečný zostatok" show, or the total in the middle of the spending chart.

## Decisions
- **The label is "Bilancia"**, the first candidate the intent named. It is one short word, so it fits beside "Výdavky spolu" without crowding the header, and the help mark says exactly what it counts.
- **In the header, after "Výdavky spolu"**, as the issue asks: it is read together with spending, and the two balances stay side by side.
- **The size of the balances, not of "Výdavky spolu"**: spending stays the header's main number, and "Bilancia" doesn't compete with it.
- **The existing green and red.** The green is already what incoming amounts use, so green keeps meaning "money in your favour". The red is already the error colour. Reusing them keeps the look to one green and one red, both already contrast-checked; only the pairs on the header's background are added to the check if they aren't there.
- **Zero has no sign.** "+0,00 €" and "−0,00 €" would both read oddly; zero is green, as the intent decided.
- **The help text names the difference from the balances**, since that is the first question a user who compares the numbers will ask.

## Open questions
None.
