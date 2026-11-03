# Spec: See all of a transaction's details in the list

Intent: [intent.md](intent.md)

## Behavior

### Opening and folding a row
- In the transactions card on "Výpis", the selected row is the **open row**: selecting a transaction opens it, and only one row is open at a time. Nothing is open when a category is chosen.
- An open row keeps its usual first line (date, description, sub-category, the "ručne" tag, "Zaradiť", "× Zrušiť zaradenie" and the amount, as in [#22](../022-categorize-from-list/spec.md)) and shows the transaction's **details** under it.
- Clicking another row opens that one and folds the first. Clicking the open row again, anywhere but on its buttons, folds it, and no row is selected; "Zaradiť" goes with it.
- "Zaradiť" and "× Zrušiť zaradenie" work as before and don't fold the row. Whatever changes the list (another category, a rule change, a hand choice, an undo, another statement) folds the row, as it clears the selection today.
- Opening a row scrolls the list as little as needed to show the whole open row; if the row is taller than the list, its first line is shown at the top.

### With the keyboard
- With the list focused, **Up** and **Down** move the selection, so they open the previous or next transaction; the open row follows.
- **Escape** folds the open row and nothing is selected; the keyboard focus stays on that row. Escape with a pop-up box open closes the box only, as today, and the row stays open.
- **Space** or **Enter** opens the row that has the keyboard focus, and folds it when it is open.
- **Tab** moves from the open row to its buttons, "× Zrušiť zaradenie" (for a hand choice) and then "Zaradiť", and from the last one out of the list; **Shift+Tab** goes back the same way. Before this item, Tab left the list from the row and never reached the buttons.

### What the details show
Each detail is a label and a value, one per line, in this order. A detail the transaction doesn't have is left out, with its label. Values are shown as the statement prints them, except where the table says otherwise.

| Group | Label | Value |
|---|---|---|
| Who | **Protistrana** | the counterparty name |
| | **Účet protistrany** | the counterparty account (IBAN, as printed) |
| | **BIC banky protistrany** | the counterparty bank's BIC |
| What for | **Popis** | the description, in full |
| | **Účel platby** | the payment purpose, only when it differs from the description |
| | **Variabilný symbol** | |
| | **Konštantný symbol** | |
| | **Špecifický symbol** | |
| | **Referencia platiteľa** | the payer's reference |
| How | **Typ transakcie** | the type's Slovak name, as the rule form uses it ("Platba kartou", "Prevod", …); for an unknown type, the type exactly as printed |
| | **Karta** | `**** 2178`, the last four digits of the card |
| When | **Dátum zaúčtovania** | the booking date, formatted as in the list |
| | **Dátum valuty** | the value date, formatted the same way |
| Bank's codes | **Kód účelu platby** | the purpose category code (e.g. `SALA`) |
| | **Kód poplatku** | the fee-type mark printed next to the transaction |
| | **Referencia transakcie** | the bank's reference of the transaction |

- Every transaction has at least "Typ transakcie" and both dates, so an open row always has details.
- The amount and currency are not repeated: the first line shows the amount.
- The groups are not headed; a little space separates one group from the next.
- A long value **wraps** in its column and is shown in full; labels don't wrap, and their column is as wide as the longest label shown.
- The details are text to read; nothing in them can be clicked, chosen or copied.

### What is kept
- Opening, folding and moving between rows saves nothing. What DocuSpend keeps stays as [PRIVACY.md](../../PRIVACY.md) says.

## Screens

The transactions card on "Výpis", with "Bývanie" chosen and the second row open (a transfer with most details):

```
+------------------------------------------------------------------+
| Transakcie  Bývanie                                              |
|  1. 5. 2026  PLATBA FAKTURY      Internet    -95,26 €            |
| ( 3. 5. 2026 PLATBA FAKTURY      [Zaradiť]  -440,93 €)           |
| (            Protistrana          Telekomunikácie Sever a.s.    )|
| (            Účet protistrany     SK07 9900 0000 0018 7346 5290 )|
| (            BIC banky protistr.  VZORSKXX                      )|
| (                                                               )|
| (            Popis                PLATBA FAKTURY                )|
| (            Variabilný symbol    50484290                      )|
| (            Konštantný symbol    0308                          )|
| (                                                               )|
| (            Typ transakcie       Prevod                        )|
| (                                                               )|
| (            Dátum zaúčtovania    3. 5. 2026                    )|
| (            Dátum valuty         3. 5. 2026                    )|
| (                                                               )|
| (            Referencia transakcie 2605033BFAFUB                )|
|  5. 5. 2026  ...                                                 |
+------------------------------------------------------------------+
```

The details start under the description, in the description's column, so the date column stays clear and the open row reads as one transaction. The whole open row has the selected row's background.

An open card payment is shorter:

```
| ( 1. 5. 2026 CERPACIA STANICA SEVER ZVOLEN  [Zaradiť]  -96,28 €)  |
| (            Popis              CERPACIA STANICA SEVER ZVOLEN   )|
| (                                                               )|
| (            Typ transakcie     Platba kartou                   )|
| (            Karta              **** 2178                       )|
| (                                                               )|
| (            Dátum zaúčtovania  1. 5. 2026                      )|
| (            Dátum valuty       29. 4. 2026                     )|
| (                                                               )|
| (            Referencia transakcie  0105220023757               )|
```

A long payment purpose wraps under itself:

```
| (            Účel platby        Doplatok za služby spojené s    )|
| (                               užívaním bytu za rok 2025, byt  )|
| (                               č. 14, vchod B                  )|
```

## Acceptance criteria

Opening and folding:
- [ ] Given a category chosen, then no row is open; when the user clicks a row, then it is selected and shows its details under its first line.
- [ ] Given an open row, when the user clicks another row, then that one is open and the first is folded.
- [ ] Given an open row, when the user clicks it outside its buttons, then it folds and no row is selected.
- [ ] Given an open row, when the user clicks "Zaradiť", then the "Zaradiť" box opens as before and the row stays open; "Zrušiť" in the box returns to the open row.
- [ ] Given an open row with a hand choice, then "× Zrušiť zaradenie" shows and works as before.
- [ ] Given an open row, when another category is chosen, a rule is added, or a hand choice is made or undone, then no row is open.
- [ ] Given a row near the bottom of the list, when it opens, then the list scrolls so the whole open row is visible.

Keyboard:
- [ ] Given the list focused with a row open, when the user presses Down or Up, then the next or previous transaction is open and the first is folded.
- [ ] Given an open row, when the user presses Escape, then it folds and no row is selected; Space or Enter opens it again.
- [ ] Given the "Zaradiť" box open, when the user presses Escape, then the box closes and the row stays open.
- [ ] Given an open row, when the user presses Tab, then the focus reaches "× Zrušiť zaradenie" (for a hand choice) and "Zaradiť", then leaves the list; Shift+Tab goes back.

Details:
- [ ] Given a transfer with every detail (from `02-busy-month`), then the details show, in the table's order and with its labels: Protistrana, Účet protistrany, BIC banky protistrany, Popis, Variabilný symbol, Konštantný symbol, Špecifický symbol, Typ transakcie, Dátum zaúčtovania, Dátum valuty, Referencia transakcie.
- [ ] Given a card payment, then "Karta" shows `**** ` and the card's last four digits, and no counterparty, symbol or purpose line shows.
- [ ] Given a payment purpose equal to the description, then only "Popis" shows; given one that differs, then both show.
- [ ] Given the transfer with a payer's reference (from `03-awkward-month`), then "Referencia platiteľa" shows it.
- [ ] Given a transfer with only an account (from `03-awkward-month`), then only "Účet protistrany", "Typ transakcie" and the dates show.
- [ ] Given an unknown type (from `10-new-types-month`), then "Typ transakcie" shows the type exactly as printed.
- [ ] Given a fee, then "Kód poplatku" or the symbols show when it has them, and no line repeats the amount.
- [ ] Given each type the reader knows, then "Typ transakcie" shows its Slovak name as the rule form does.
- [ ] Given every transaction of every valid sample, then each detail the transaction has shows once, and no label shows without a value.
- [ ] Given a value longer than its column, then it wraps and every character of it is on screen.

Look, privacy and hand checks:
- [ ] The open row uses only the shared look; the look test passes.
- [ ] Every new text on screen is in Slovak.
- [ ] Opening, folding and moving between rows writes nothing to DocuSpend's folder.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.
- [ ] The maintainer has approved screenshots, in both looks, at the default window size: an open transfer, an open card payment, and an open row with a long wrapped purpose.
- [ ] Hand check (maintainer): with a real statement open, open transactions of each kind and find that what each shows matches the statement, nothing is missing and nothing is shown twice.

## Edge cases and errors

- **A transaction with only a type, dates and an amount** (no description, no counterparty). It opens with those three lines; the first line says "(bez popisu)" as today.
- **An unknown type or a cash withdrawal**, whose first line puts the type before the description. The details still show "Typ transakcie" and "Popis" separately.
- **A detail made only of spaces.** The reader doesn't produce one; if it did, it would be left out like a missing one.
- **A very long value with no spaces** (a long reference). It breaks at the column's edge rather than widening the row.
- **An open row taller than the list** (a long purpose in a small window). The list scrolls as usual; the first line is shown first.
- **The last transaction in a list.** It opens as any other; the list scrolls so its details are visible.
- **A window narrower than the default.** Values wrap more; labels keep their width.
- **Opening a row while the list is changing** (a rule just saved). The change folds the row, as it clears the selection today.

## Out of scope

- Reading more from a statement than the reader reads today (a separate item).
- Copying a detail out of the list (a follow-up issue, if wanted).
- Editing a transaction's details.
- Showing which rule put a transaction where it is ([#22](../022-categorize-from-list/spec.md) leaves it out too).
- Details in the "Zaradiť" box, on "Pravidlá", or in the chart hover cards ([#20](../020-chart-hover-cards/spec.md)).
- Searching or filtering by a detail.
- Showing the statement's own details (account holder, IBAN, period); they are not a transaction's.

## Decisions

- **The open row is the selected row**, as the intent says, so selecting, opening and "Zaradiť" are one thing.
- **Clicking the open row folds it and clears the selection.** Leaving it selected but folded would be a second state the user can't tell apart from an open one without looking for "Zaradiť".
- **"Účel platby" shows only when it differs from the description.** On VÚB statements the description is usually the purpose word for word; showing it twice is noise, and nothing is lost.
- **The fee column's amount is not shown.** The reader only reads it when it equals the transaction's amount, which the first line already shows.
- **The currency is not shown on its own line.** The amount on the first line carries it.
- **"Dátum valuty" always shows, even when it equals the booking date.** It keeps every row of a kind the same shape, and the intent asks for the same thing in the same place.
- **The card is shown as `**** 2178`**, so it reads as the end of a card number rather than a code.
- **"Popis" shows in the details even though the first line shows it too.** The first line cuts it with "…" and may put a type before it or show the counterparty instead; the details show it alone and in full.
- **The bank's codes come last and keep the statement's text** ("SALA", "E"), since DocuSpend doesn't know what every code means and the user can match them against the statement.
- **Groups are separated by space, not headed.** Headings would add five lines to every open row for little help; the order already groups them.
- **Space and Enter toggle the focused row, Escape folds it**, so the keyboard can do all the mouse does without new shortcuts.

## Open questions

None.
