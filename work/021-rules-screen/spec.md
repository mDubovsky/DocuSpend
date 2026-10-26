# Spec: Move the rules off the statement screen

Intent: [intent.md](intent.md)

## Behavior

### Two screens
- With a statement open, the window has two screens: **"Výpis"** (the statement) and **"Pravidlá"** (the rules). Before any statement is open there is only the start screen ([#12](../012-open-statement/spec.md)), unchanged, with no switch.
- The **header** is the same on both screens and stays where it is. On the left: "Otvoriť výpis…", the statement's period, the "Ukážka s vymyslenými údajmi" badge when the example is open, and "Výpis sa číta…" while reading. In the middle: the switch. On the right: "Vzhľad" with the light and dark switch.
- The switch is a pair of pills, **"Výpis"** and **"Pravidlá"**, in the middle of the header. It has the look of the light and dark switch, including its hover ([#19](../019-look-switch-scroll-bars/spec.md)). The chosen screen's pill is highlighted. Choosing the other pill switches the screen; choosing the highlighted one does nothing.
- **Opening another statement always shows "Výpis"**, whichever screen it was opened from: with the button, with Ctrl+O, or by dropping a file.

### The statement screen ("Výpis")
- It shows, from the top: the header with the key figures ("Výdavky spolu", the opening and closing balance), as today; the two charts side by side; the categories ("Kategórie") and the transactions of the selected category side by side.
- It has **no rule form and no rule list**.
- **At the default window size it fits the window without scrolling.** The two lists take the height that is left, and each shows at least five rows. A list longer than that scrolls inside its card, as today. A taller window gives the lists more rows; the charts keep their height.
- The charts may be shorter than today to make that room. Both stay fully readable: the pie with its total in the middle and its legend beside it, the balance line with its axes. Their hover cards work as today ([#20](../020-chart-hover-cards/spec.md)).

### The rules screen ("Pravidlá")
- It shows the header (without the key figures), then, side by side, the **"Nové pravidlo" form** on the left and the **rules card** on the right, with its "Moje pravidlá" and "Vstavané pravidlá" pills.
- Both work exactly as [#24](../024-edit-rules/spec.md) specifies: every kind of condition, editing, the priority field with its help mark, deleting through the pop-up box, the built-in rules, and every message, including "Zmeny pravidiel sa teraz neukladajú." and the messages for a change that can't be saved.
- The rules card takes the window's remaining height, and its list scrolls inside it. The form is as tall as its fields.
- Adding, editing or deleting a rule categorizes the open statement again straight away, and the form or the list says how many transactions the rule catches, as today. "Výpis" then shows the new categorization.

### Switching keeps what the user left
- Going to "Pravidlá" and back keeps the selected category, its transactions, and where each list was scrolled to. If a rule change leaves the selected category empty, it is no longer selected and the transactions card says so, as it does today when a category empties.
- Going to "Výpis" and back keeps the form as it was: a half-typed new rule, or a rule being edited with its unsaved changes. It also keeps the chosen view of the rules card ("Moje pravidlá" or "Vstavané pravidlá") and its messages.
- Opening another statement keeps the form too, as [#24](../024-edit-rules/spec.md) says; saving then categorizes the newly opened statement.

### Messages
- The messages at the top of the header show **on both screens**: that a file couldn't be opened (#12), and that the saved rules couldn't be loaded (#13). They close the same way as today.
- Messages that answer a change made on the rules screen stay on the rules screen, next to the change: the form's and the list's confirmations and errors, and "Zmeny pravidiel sa teraz neukladajú.".

## Screens

The statement screen, at the default window size. Nothing below the lists; the window doesn't scroll.

```
+--------------------------------------------------------------------------------+
| [Otvoriť výpis…] 1. 9. 2026 – 30. 9. 2026 (Ukážka…)   (Výpis|Pravidlá)   Vzhľad (…) |
| [! Message about a file that wasn't opened, or rules that weren't loaded  [x]] |
| Výdavky spolu        Počiatočný zostatok     Konečný zostatok                  |
| 1 234,56 €           2 000,00 €              1 500,00 €                        |
+---------------------------------------+----------------------------------------+
| Výdavky podľa kategórií               | Zostatok na účte                       |
|   ( pie, total in the middle )  legend|   balance line                         |
|   (shorter than today if needed)      |                                        |
+---------------------------------------+----------------------------------------+
| Kategórie                             | Transakcie                             |
|  Potraviny          12   -345,67 €    |  12. 9.  LIDL ...           -23,45 €   |
|  Bývanie             3   -800,00 €    |  ...                                   |
|  ... (at least 5 rows, scrolls inside)|  ... (at least 5 rows, scrolls inside) |
+---------------------------------------+----------------------------------------+
```

The rules screen. The header is the same, without the key figures.

```
+--------------------------------------------------------------------------------+
| [Otvoriť výpis…] 1. 9. 2026 – 30. 9. 2026 (Ukážka…)   (Výpis|Pravidlá)   Vzhľad (…) |
| [! Message about a file that wasn't opened, or rules that weren't loaded  [x]] |
+---------------------------------------+----------------------------------------+
| Nové pravidlo                         | (Moje pravidlá|Vstavané pravidlá)      |
|  Podmienka   [Text v popise v]        | [Zmeny pravidiel sa teraz neukladajú.] |
|              [obsahuje v] [        ]  |  Text obsahuje „lidl“                  |
|  Kategória   [               v]       |  Potraviny › Supermarkety              |
|  Podkategória[               v]       |               [Upraviť] [Odstrániť]    |
|              [Pridať pravidlo]        |  ...                                   |
|  [Pravidlo zaradilo 3 transakcie.]    |  ... (fills the height, scrolls inside)|
+---------------------------------------+                                        |
                                        |                                        |
                                        +----------------------------------------+
```

The header is one line at the default width; in a narrow window the left and right groups wrap inside their columns, and the switch stays in the middle. The pop-up box for deleting a rule is unchanged: over the whole dimmed window.

## Acceptance criteria

Two screens:
- [ ] Given the start screen, then there is no "Výpis" / "Pravidlá" switch.
- [ ] Given a statement opened from the start screen, and separately the example, then "Výpis" is chosen and the statement screen shows.
- [ ] Given a statement open, when the user chooses "Pravidlá", then the rules screen shows; when they choose "Výpis", the statement screen shows again. Choosing the pill already chosen changes nothing.
- [ ] Given either screen, then the header shows the switch, "Otvoriť výpis…", the period, the example badge when the example is open, and the look switch, in the same places.
- [ ] Given the rules screen, when the user opens another statement with the button, with Ctrl+O, and by dropping a file, then each time "Výpis" is chosen and the new statement shows.

Statement screen:
- [ ] Given the statement screen, then it shows the key figures, both charts, "Kategórie" and the transactions card, and no rule form, rules card, "Moje pravidlá" or "Vstavané pravidlá".
- [ ] Given the default window size (1280 × 880), each valid sample (`01` to `05`), and both looks, then the window has nothing to scroll, and the categories and transactions lists each show at least five rows.
- [ ] Given a statement with more categories and transactions than fit, at the default size, then the window still has nothing to scroll and the lists scroll inside their cards.
- [ ] Given the window made taller than the default, then the lists show more rows and the window still has nothing to scroll.
- [ ] The charts' hover cards work on the statement screen as before (#20's window tests still pass).

Rules screen:
- [ ] Given the rules screen, then it shows the "Nové pravidlo" form and the rules card, and no key figures, charts, "Kategórie" or transactions.
- [ ] Every acceptance criterion of [#24](../024-edit-rules/spec.md) that drives the screen still holds on the rules screen (its window tests pass, run on "Pravidlá").
- [ ] Given a statement open and the rules screen, when the user adds a rule that catches transactions, then the form says how many, and on "Výpis" the charts, the key figures, "Kategórie" and the selected category's transactions follow the new rule.
- [ ] The same holds for editing and for deleting a rule.

Switching keeps what the user left:
- [ ] Given a category selected and both lists scrolled down, when the user goes to "Pravidlá" and back, then the same category is selected, its transactions are listed, and both lists are scrolled where they were.
- [ ] Given a category selected, when a rule moves all its transactions elsewhere and the user goes back to "Výpis", then no category is selected and the transactions card says "Vyberte kategóriu v grafe alebo v zozname."
- [ ] Given a half-typed new rule, when the user goes to "Výpis" and back, then the form shows exactly what was typed. The same for a rule being edited with unsaved changes, which is still marked in the list.
- [ ] Given "Vstavané pravidlá" chosen in the rules card, when the user goes to "Výpis" and back, then "Vstavané pravidlá" is still chosen.

Messages:
- [ ] Given a file that couldn't be opened, then its message shows on "Výpis" and on "Pravidlá" until closed.
- [ ] Given saved rules that couldn't be loaded, then the message shows on both screens until closed.
- [ ] Given a failed open while on "Pravidlá", then the window stays on "Pravidlá" with the message, and the statement that was open is still the one shown on "Výpis".
- [ ] "Zmeny pravidiel sa teraz neukladajú." and the messages after a rule change show on the rules screen only.

Look, privacy and hand checks:
- [ ] The switch comes from the shared look; the look test still finds no literal colour, size or spacing in the screens.
- [ ] All privacy checks pass, with no new entries in the reviewed lists. What is saved doesn't change.
- [ ] No new text is in English.
- [ ] The maintainer has approved screenshots, in both looks, at the default window size, of the statement screen and the rules screen (the switch's hover included).
- [ ] Hand check (maintainer): open a real statement, switch between the screens, add and edit a rule on "Pravidlá", check that "Výpis" follows it and kept its selection, and that "Výpis" fits the default-sized window without scrolling.

## Edge cases and errors

- **A window smaller than the default.** Down to its minimum size (720 × 520), the lists get shorter down to a few rows, and below that the window scrolls, as today. Nothing in the header is cut off or unreachable: it may wrap onto a second line.
- **A message at the top of the header.** It takes its height from the lists, which may show fewer than five rows until it is closed; the window still doesn't scroll at the default size. The five rows are with no message shown.
- **A window wider than the default.** The cards widen; nothing else changes.
- **Switching while the pop-up box for deleting a rule is open.** It can't be done: the box covers the window until it is answered, as today.
- **A hover card showing when the screen switches.** It disappears; no hover card or dot shows on the rules screen.
- **Dropping a file on the rules screen.** The drop overlay shows as on the statement screen; a valid file opens and shows "Výpis", a failed one leaves the window on "Pravidlá" with the message.
- **A statement with no spending.** The statement screen shows "V tomto mesiaci neboli žiadne výdavky." in the pie's card, as today, and still fits.
- **No user rules.** The rules screen's list says "Zatiaľ žiadne pravidlá.", as today.
- **Reading a statement takes a moment.** "Výpis sa číta…" shows in the header on whichever screen is shown, and the switch to "Výpis" happens when the statement is read.

## Out of scope

- Changing what the rules can do or how the form and the rules card look ([#24](../024-edit-rules/spec.md)).
- Managing categories and sub-categories ([#25](https://github.com/mDubovsky/DocuSpend/issues/25)); it may later join "Pravidlá" or get a pill of its own.
- Showing which rule decided a transaction, and categorizing one transaction from the statement screen ([#22](https://github.com/mDubovsky/DocuSpend/issues/22)).
- The rules screen without a statement open.
- Fitting a window smaller than the default without scrolling.
- Switching screens with a keyboard shortcut, or reaching the switch with the keyboard (a later accessibility item, as #11 and #19 left it).
- Remembering the chosen screen after DocuSpend closes.
- Restyling the charts, lists or header beyond the room the statement screen needs.

## Decisions

- **The key figures belong to the statement screen.** They describe the month, which the rules screen isn't about; the header still shows the period and the example badge there, so the user knows which statement the rules act on.
- **The charts may be shorter than today.** At the default size the header, the charts and two lists with five rows each don't fit at today's chart height (300 points); the lists are where the detail is, and a pie and a month's balance line read well a little smaller. The plan sets the height, and the screenshot review settles it.
- **At least five rows in each list at the default size**, so "fits without scrolling" can't be met by lists too short to use.
- **The extra height of a taller window goes to the lists**, not the charts: they are what grows with the statement.
- **Messages about the whole application show on both screens; messages about a rule change stay with the change.** The header's messages (a file not opened, rules not loaded) can arrive on either screen. A change's confirmation or failure, and "Zmeny pravidiel sa teraz neukladajú.", only make sense next to the rules; the rules-not-loaded message in the header already tells the user on "Výpis" that their rules aren't there. This narrows the intent's "loading or saving rules, visible whichever screen is shown" to loading.
- **The switch is in the middle of the header, "Otvoriť výpis…" at the very left and "Vzhľad" at the very right**, as the maintainer chose after the screenshot review. The intent placed the switch next to the button; the middle keeps both ends where they were.
- **A failed open doesn't switch screens.** Nothing changed, so the user stays where they were, with the message.
- **The chosen screen isn't remembered between sessions**: a statement isn't kept either ([#12](../012-open-statement/spec.md)), so every start begins on the start screen.

## Open questions

None.
