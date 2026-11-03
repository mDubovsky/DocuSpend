# Spec: Open statements with unfamiliar transaction kinds

Intent: [intent.md](intent.md)

## Behavior

### Reading a transaction of an unknown type

The reader recognizes a transaction's type from how its row reads ([work/002-statement-reader](../002-statement-reader/spec.md#how-the-printed-layout-maps-to-those-fields)). This item adds two types and a way through for everything else:

- **wallet card payment:** the row says "PLATBA KARTOU A-PAY" **or "PLATBA KARTOU G-PAY"**, and a card;
- **cash withdrawal** (new): the row says "VÝBER HOTOVOSTI" and a card. It is read like a card payment: card (last 4 digits), value date from the line below, description from that line, reference;
- **unknown type** (new): the row is led by a booking date, has text where the type is printed that is none of the above and not an account number, and an amount in the amount column.

A transaction of an unknown type keeps its **printed type**: the text in the type's place exactly as printed, words separated by single spaces (for example "PLATBA HODINKAMI"). Everything else on its row is read by the same rules as other rows: a card if one is printed, a value date from the line below if one is printed there (otherwise from the value-date column, otherwise the booking date), a description, symbols, a reference, and a details block if there is one. Fields that aren't printed are absent.

DocuSpend doesn't guess what an unknown type means: a transaction with a card number is still of an unknown type, not a card payment.

Every other type keeps no printed type: only unknown types have one.

### Details with a label the reader doesn't know

Added during the build, after the hand check found a transfer whose details block also prints "Pôvodný odosielateľ:" (original sender) and "Konečný príjemca:" (ultimate beneficiary), which the reader didn't know ([#41](https://github.com/mDubovsky/DocuSpend/issues/41)).

Inside a transaction, a line at the left edge that starts with a label the reader doesn't know (one to four words, the last ending in ":") is a detail like any other, and a value that wraps continues on the lines below it. Its value isn't kept: no field of the transaction holds it, so it doesn't show and no rule looks at it. A line at the left edge that is neither a date, nor a known label, nor a label at all still rejects the statement, as today.

### A mark after the value date, and rows low on the page

Also found by the hand check, and decided by the maintainer:

- **"#" after a value date.** A card payment's value date may be printed with a "#" right after it ("08/10#"); on the statement seen it was a card payment with money coming in, probably a refund. Its meaning isn't printed. The value date reads as the date, and the mark is dropped. Any other character after a date still makes it unreadable.
- **Rows down to the bottom of the page.** The table can run down to about 811 pt from the top of a page, lower than the 800 pt measured on the first statement, so a row near the bottom had its second line cut off. The reader now reads the table down to 820 pt (it still stops at the fee breakdown on the last page).

### What still rejects a statement

Nothing about rejections changes except that an unfamiliar type is no longer a reason. A statement is still rejected when:

- a transaction's booking date is missing or not a date, or its value date is printed but isn't a date;
- a transaction of any type, unknown included, has no amount in the amount column (and isn't a fee row);
- the transactions don't add up to the statement's printed totals, or the closing balance doesn't follow (the reconcile check runs over every transaction, unknown types included).

The rejection names the transaction's place (n-th transaction on page p), as before.

### Categorizing

Transactions of the two new types and of unknown types are categorized like any other ([work/003-categorize-transactions](../003-categorize-transactions/spec.md)), with these additions:

- **Type condition.** A type rule can choose cash withdrawal, like any other type ("Výber hotovosti"). It can't choose an unknown type, and no type rule matches a transaction of an unknown type.
- **Text condition.** For a transaction of an unknown type, the printed type counts as one more text, next to the description, payment purpose and counterparty name. So a text rule "contains hodinkami" catches every "PLATBA HODINKAMI". For every other type nothing changes.
- **Built-in rules don't place cash withdrawals.** No default rule matches a cash withdrawal, so it goes to Nezaradené výdavky unless a user rule catches it. (A withdrawal's description names the cash machine, often inside a shop, so a text rule would wrongly call it shopping.)

### Showing it

- **Type names.** The new type's Slovak name is "Výber hotovosti". It is offered in the rule form's type choices and shown in the rules list like the others. "PLATBA KARTOU G-PAY" shows as "Platba mobilom", the same as Apple Pay.
- **Transaction list.** A transaction of an unknown type shows its printed type before its description, joined by " · ", for example "PLATBA HODINKAMI · PEKAREN U MOSTA"; with no description, the printed type alone. A cash withdrawal shows "Výber hotovosti · " before its description, since the description alone names only a cash machine.
- **The note.** When the open statement has transactions of unknown types, the statement screen shows a short, quiet line next to the key figures: "1 transakcia neznámeho typu", "3 transakcie neznámeho typu", "5 transakcií neznámeho typu". It has a help mark whose tooltip says: "DocuSpend tento typ transakcie zatiaľ nepozná. Transakciu načítal so sumou aj dátumom a v zozname transakcií má typ uvedený pred popisom. Zaradiť ju môžete pravidlom s textom." A statement with none shows no line. The note can't be dismissed and takes no space when absent.

### Saved rules

- A rule on cash withdrawals is saved and loaded like any other rule ([work/013-keep-rules](../013-keep-rules/spec.md)).
- **An older DocuSpend never takes a newer file for a damaged one.** A file holding a rule on cash withdrawals is one an earlier version can't use; that earlier version must treat it as saved by a newer DocuSpend (file untouched, the newer-version message), not as damaged (file moved aside).
- From this item on, a saved type rule naming a type this DocuSpend doesn't know is treated as saved by a newer DocuSpend, too, so the next new type doesn't need this again.
- Files saved before this item load unchanged and categorize the same.

### Checking real statements (maintainer)

The statement check prints, per readable statement, also how many of its transactions are of an unknown type: a count only, never the printed types or anything else from the file.

## Screens

The statement screen's key figures, with the note. Everything else on the screen is unchanged.

```
+--------------------------------------------------------------------------------+
| [Otvoriť výpis…] 1. 10. 2026 – 31. 10. 2026 (…)   (Výpis|Pravidlá|Kategórie)  Vzhľad (…) |
| Výdavky spolu        Počiatočný zostatok     Konečný zostatok                  |
| 1 234,56 €           2 000,00 €              1 500,00 €      1 transakcia neznámeho typu (?) |
+--------------------------------------------------------------------------------+
```

The note sits at the right end of the key-figures row, in the muted text style, aligned with the amounts. The (?) is the same help mark as by "Moje pravidlá"; hovering it shows the tooltip.

The transaction list, unchanged except for the description column:

```
| Transakcie                                                        |
|  5. 10.  POTRAVINY JANKO TRNAVA       Potraviny          -12,40 € |
|  7. 10.  Výber hotovosti · ATM00417…  Nezaradené výdavky -50,00 € |
|  9. 10.  PLATBA HODINKAMI · PEKAREN…  Potraviny           -3,20 € |
```

## Acceptance criteria

Reading:
- [ ] Given a statement with a "PLATBA KARTOU G-PAY" row, then it is read as a wallet card payment with its card, value date, description and reference, the same as an A-PAY row.
- [ ] Given a statement with a "VÝBER HOTOVOSTI" row, then it is read as a cash withdrawal with its card, value date, description and reference.
- [ ] Given a statement with a row of an unknown type that is laid out like a card payment, then the statement opens and that transaction has an unknown type, its printed type, its card, value date, description and amount.
- [ ] Given a row of an unknown type laid out like a transfer row without an account number (symbols, a details block), then its symbols and details are read as for a transfer.
- [ ] Given a row of an unknown type with no amount in the amount column, then the statement is rejected as unreadable, naming that transaction's page and number.
- [ ] Given a row of an unknown type with a damaged booking date, then the statement is rejected as before.
- [ ] Given a statement with a transaction of an unknown type whose totals don't add up, then it is rejected as not reconciling.
- [ ] Known types carry no printed type; every committed sample reads exactly as its record says, and the existing reader tests pass unchanged.
- [ ] Given a transfer whose details block has lines with labels the reader doesn't know ("Pôvodný odosielateľ:", "Konečný príjemca:"), one of them wrapping, then the statement opens and the transfer reads as if those lines weren't there.
- [ ] Given a line at the left edge of the table that is not a date and not a label, then the statement is still rejected.
- [ ] Given a card payment whose value date is printed with "#" after it, then it reads with that value date; a value date with any other character after it is still rejected.
- [ ] Given a page whose last row's second line is printed below 800 pt (as low as real statements print), then the row reads completely.

Categorizing:
- [ ] A type rule for wallet card payments catches a G-PAY transaction; a type rule for cash withdrawals catches a cash withdrawal; no type rule catches a transaction of an unknown type.
- [ ] A text rule ("contains" or "equals") whose text appears only in an unknown type's printed type catches that transaction; the same rule doesn't catch a known-type transaction whose row prints that word as its type.
- [ ] A cash withdrawal whose description contains a word a default rule looks for (for example "TESCO") goes to Nezaradené výdavky when no user rule catches it; a user text or type rule does catch it.
- [ ] A transaction of an unknown type that no rule catches goes to Nezaradené výdavky if money out and Nezaradené príjmy otherwise, and counts in the totals; nothing is lost.

Showing:
- [ ] The rule form offers "Výber hotovosti" among the types, and a type rule with it reads "Typ: Výber hotovosti" in the rules list.
- [ ] In the transaction list, an unknown-type transaction reads "<printed type> · <description>", or the printed type alone without a description; a cash withdrawal reads "Výber hotovosti · <description>"; other rows are unchanged.
- [ ] Given a statement with 1, 3 and 5 transactions of unknown types, the note reads "1 transakcia neznámeho typu", "3 transakcie neznámeho typu" and "5 transakcií neznámeho typu", with the help mark and its tooltip; given a statement with none, no note shows.
- [ ] Screenshots of the statement screen with the note, in both looks, are reviewed by the maintainer (hand check).

Saving:
- [ ] A rule on cash withdrawals is saved and, after a restart, loaded and applied.
- [ ] The committed first-shape saved file still loads with the same rules and categorization.
- [ ] A saved file as this item writes it, when it holds a cash-withdrawal rule, is one the previous release would treat as newer, not damaged: checked by a test that reads such a file with the earlier reading rules (how is the plan's choice).
- [ ] A saved type rule naming a type this DocuSpend doesn't know is reported as saved by a newer DocuSpend, and the file is left untouched.

Checking and privacy:
- [ ] The statement check prints the count of unknown-type transactions per readable statement, and none of their text.
- [ ] Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)); all new samples are synthetic, with a fictional bank identity ([decision 0002](../../docs/decisions/0002-fictional-bank-identity-in-samples.md)).
- [ ] The maintainer opens the October statement that was rejected; it opens, the G-PAY payments are payments by mobile, the cash withdrawal is in Nezaradené výdavky, and the statement check reports 0 transactions of an unknown type (hand check, maintainer).

## Edge cases and errors

- **Printed type that looks like an account number** (country letters, two digits, groups of four): it is read as an account number and the row as a transfer, as today. Not worth telling apart.
- **Printed type spread over two words or more**, or very long: kept whole, with single spaces; the list row trims it with an ellipsis like any long description.
- **Unknown type on a fee-style row** (no amount in the amount column, a fee in the fee column, and type text): rejected, since the reader can't tell a fee from damage there. A plain fee row has no type text and reads as before.
- **Unknown type that happens to contain "PLATBA KARTOU"** (for example "PLATBA KARTOU X-PAY"): unknown type with that printed type. DocuSpend doesn't guess it is a wallet payment.
- **Many unknown-type transactions:** the note shows the count, whatever it is.
- **The same unknown type in several months:** each statement counts its own.
- **An unknown type in a month where a rule already catches it by text:** it is still counted in the note. The note is about what DocuSpend knows, not about what is uncategorized.
- **Rules file with a cash-withdrawal rule opened by an older DocuSpend:** newer-version message, file untouched (see Saved rules).

## Out of scope

- A built-in category for cash, or any default rule for cash withdrawals (intent decision).
- Types beyond G-PAY and cash withdrawal; new types are added by later items when one turns out to deserve it.
- Keeping the values of details labels the reader doesn't know, or making them searchable.
- A type condition that names a printed type; type rules for unknown types.
- Showing a transaction's type for known types in the transaction list.
- Accepting statements that don't reconcile, or rows with unreadable dates or amounts.
- Other banks or formats ([mvp.md](../../docs/mvp.md)).

## Decisions

- **"Typ", not "druh", on screen.** The rule form already says "Typ transakcie", so the note says "neznámeho typu" rather than the intent's rough "neznámeho druhu".
- **The note goes at the right end of the key figures, with a help mark.** It is where the user looks first, it takes no space when there is nothing to say, and the help mark gives the one-sentence explanation without crowding the screen. The existing message strip above is for problems the user may dismiss; this is not a problem.
- **The printed type counts as text only for unknown types.** Making "PLATBA KARTOU" searchable on every card payment would change what existing text rules catch.
- **Default rules don't place cash withdrawals.** A withdrawal's description names the cash machine and its place, which often contains a shop's name ("TESCO", "COOP"), so default rules would file cash under groceries. The intent says cash goes to Nezaradené výdavky until the user decides.
- **Cash withdrawals and unknown types show their type in the transaction list.** Without it a withdrawal reads as a cash machine code, and an unknown type gives no hint why it is there.
- **An unknown type with a card number stays unknown.** The intent rules out guessing.
- **The saved file must be recognised as newer by older versions.** Today an unknown type word makes a saved file "damaged", and a damaged file is moved aside, which would cost a user switching between versions their rules. How (for example raising the shape's version) is for the plan.
- **The reject message for an unknown kind goes away**, since that case no longer rejects.
- **A details label the reader doesn't know is accepted and its value dropped**, decided by the maintainer during the hand check. It is the same idea as unknown types: unfamiliar printed text doesn't block a statement whose numbers are right. Keeping the values as searchable fields was considered and left for later.

## Open questions

None.
