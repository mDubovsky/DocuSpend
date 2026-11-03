# Spec: Categorize a transaction from the list

Intent: [intent.md](intent.md)

## Behavior

Two new terms, for the glossary: a **hand choice** is a sub-category the user chose for one transaction alone; its **fingerprint** is what DocuSpend keeps to recognise that transaction again, made from its details so that the details can't be read back from it.

### Choosing a transaction
- In the transactions card on "Výpis", the user can select a row, in any category, uncategorized or not. One row is selected at a time; selecting another category clears the selection.
- A selected row shows the button **"Zaradiť"** at its right end, before the amount. For a transaction with a hand choice, the row also shows **"× Zrušiť zaradenie"** (below).
- The selection, and the buttons, go away when the list changes (another category, a rule change, another statement).

### The "Zaradiť" box
- "Zaradiť" opens a **pop-up box** over the dimmed window, drawn as the box for deleting a rule is ([#24](../024-edit-rules/spec.md)): it waits until it is answered, and Escape closes it as "Zrušiť" does.
- The box is headed **"Zaradiť transakciu"** and shows the transaction as the list does: its date, its description and its amount.
- **"Kategória"** and **"Podkategória"** list every category and sub-category that a rule could target, in the order the rule form uses ([#25](../025-own-categories/spec.md)): "Nezaradené výdavky" and "Nezaradené príjmy" are not offered. If the transaction is already in a sub-category, that one is chosen at first; if it is uncategorized, nothing is.
- Below, a choice of two, **"Len túto transakciu"** (chosen at first) and **"Pravidlom pre všetky podobné transakcie"**.
- With "Pravidlom…" chosen, the condition fields of the rule form appear under it: **"Podmienka"** with the six kinds, and each kind's fields, with the same checks and Slovak reasons as on "Pravidlá" (#24). They start filled in from this transaction (table below). The user can change the kind and every field. With "Len túto transakciu" chosen, no condition is shown: it applies to this transaction only.
- The buttons are **"Zaradiť"** and **"Zrušiť"**. Nothing is chosen when the box opens.

### What the condition starts with

The kind chosen at first is the one most likely to catch the same kind of transaction: **"Text v popise"** for a card or mobile payment, **"Protistrana"** for any other transaction with a counterparty name, **"Účet protistrany"** for one with only an account, and "Text v popise" otherwise. Switching to another kind shows that kind filled in from the transaction too:

| Kind | Starts with |
|---|---|
| "Text v popise" | "obsahuje", and the transaction's description; without one, its counterparty name; without either, empty |
| "Protistrana" | "obsahuje", and the counterparty name, or empty |
| "Účet protistrany" | the counterparty account, or empty |
| "Suma" | "Od" and "Do" both the size of the amount (`23,45` for −23,45 €) |
| "Typ transakcie" | the transaction's type ticked; nothing ticked for an unknown type, which the form doesn't offer |
| "Variabilný symbol" | the variable symbol, or empty |

An empty field is refused on "Zaradiť" with the usual reason, as on "Pravidlá".

### "Len túto transakciu"
- "Zaradiť" first shows the **privacy box** (below), unless the user has switched it off. Then the transaction moves to the chosen sub-category, and only it: another transaction that looks the same, in this statement or any other, stays where it was.
- A hand choice wins over every rule, built-in or the user's own, and over a newer rule added later, until it is undone.
- The box closes and the transactions card says, under its heading, **"Transakcia je zaradená do „<kategória> › <podkategória>“."** The message shows even if the category the user was looking at is now empty; it stays until the next change or another category is chosen.
- Choosing "Len túto transakciu" again for a transaction that already has a hand choice replaces it.

### "Pravidlom pre všetky podobné transakcie"
- "Zaradiť" checks the condition as the rule form does. It also refuses a condition that doesn't match this transaction: **"Táto transakcia podmienke nezodpovedá. Upravte podmienku."** The box stays open with what was typed.
- Otherwise it adds an ordinary user rule with that condition and target, on top of the user's rules, as "Pridať pravidlo" does on "Pravidlá" (#24): no number asked for, newest wins. It shows in "Moje pravidlá" and can be edited and deleted there.
- If this transaction had a hand choice, the hand choice is removed, so the new rule decides it. Hand choices of other transactions it matches stay, and win.
- The box closes, the open statement is categorized again, and the transactions card says how many transactions the rule catches, with the texts of "Pravidlá": "Pravidlo zaradilo 3 transakcie."

### The privacy box
When the user confirms "Len túto transakciu", a second pop-up box opens over the first, until the user switches it off. It says, in full:

> **Čo si DocuSpend zapamätá**
>
> Aby DocuSpend túto transakciu zaradil rovnako aj nabudúce, uloží si jej odtlačok. Neuloží jej dátum, sumu, popis ani protistranu, len odtlačok vypočítaný z nich. Odtlačok zostáva len v tomto počítači, v priečinku DocuSpend, a nikam sa neposiela.
>
> Z odtlačku sa tieto údaje nedajú spätne zistiť. Kto by však mal ten istý výpis a prístup k tomuto počítaču, vedel by overiť, či ste túto transakciu zaradili ručne.
>
> Odtlačok je uložený spolu s vašimi pravidlami v súbore %APPDATA%\DocuSpend\rules.json. Vymaže sa, keď zaradenie zrušíte tlačidlom „Zrušiť zaradenie“. Všetko, čo si DocuSpend pamätá, odstránite vymazaním priečinka %APPDATA%\DocuSpend.

- Under the text: a tick box **"Túto správu už nezobrazovať"** (not ticked), and the buttons **"Zaradiť"** and **"Späť"**.
- "Zaradiť" makes the hand choice. If the tick box is ticked, the privacy box doesn't show again, also after a restart; this is kept with the look setting.
- "Späť", or Escape, closes the privacy box and returns to the "Zaradiť" box as it was, with nothing changed; the tick box is not kept.
- The box fits the default window without scrolling.

### The mark and undoing it
- A transaction with a hand choice shows a small tag **"ručne"** in its row, after the sub-category, wherever it is listed. Pointing at the tag shows the tooltip "Zaradené ručne, len táto transakcia." and turns the tag into the button **"× Zrušiť zaradenie"**; so does selecting the row.
- "× Zrušiť zaradenie" removes the hand choice at once, without asking, and its fingerprint is removed from what is kept. The transaction goes to wherever the rules put it, the open statement's charts and lists follow, and the transactions card says **"Zaradenie je zrušené. Transakcia je opäť v „<kategória › podkategória, or Nezaradené výdavky>“."**

### What is kept, and recognising a transaction again
- A hand choice is saved as soon as it is made or undone, with the rules ([#13](../013-keep-rules/spec.md)): the fingerprint and the chosen sub-category, nothing else. No date, amount, description, counterparty, account or other detail of the transaction is saved, and nothing about the statement file.
- The fingerprint is made from the transaction's booking date, amount, description and counterparty (name and account), and from which one it is, in the order the statement lists them, among transactions of the statement that have all of those the same.
- Whenever a statement is categorized, a transaction whose fingerprint has a hand choice goes to that sub-category: the same statement opened again, the same statement downloaded again, and a statement whose period overlaps and contains the same transaction.
- A transaction that differs in any of those details (for example a description the bank changed) has a different fingerprint and goes where the rules put it. A hand choice no statement matches is kept, but does nothing.
- What is saved gets a new version of its shape. Files saved in the earlier shapes still load as they did (`rules-v1.json`, `rules-v2.json`), with no hand choices.

### When a change can't be saved
As for rules (#13, #24), the change applies until DocuSpend closes, and the transactions card says so under the confirmation:

| | Saving is off this session | Saving failed |
|---|---|---|
| Hand choice made | "Zaradenie platí len do zatvorenia DocuSpend." | "Zaradenie sa nepodarilo uložiť, preto po zatvorení DocuSpend zmizne. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova." |
| Hand choice undone | "Zaradenie sa po zatvorení DocuSpend vráti." | "Zrušenie sa nepodarilo uložiť, preto sa zaradenie po zatvorení DocuSpend vráti. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova." |
| Rule added | the texts of "Pravidlá" for a new rule ("Pravidlo platí len do zatvorenia DocuSpend.") | "Pravidlo sa nepodarilo uložiť, …" as on "Pravidlá" |

If "Túto správu už nezobrazovať" can't be saved, it holds until DocuSpend closes and the privacy box shows again next time; nothing else is said.

### PRIVACY.md and mvp.md
- **PRIVACY.md**, "What DocuSpend keeps", says in plain words:
  - that `rules.json` now also holds a fingerprint for each transaction the user categorized by hand, with the sub-category chosen, and nothing else about it;
  - what the fingerprint is made from, that the details can't be read back from it, and that someone with the same statement and access to the computer could check whether a transaction in it was categorized by hand;
  - that a rule made from a transaction keeps the text, account, symbol or amount it was filled in with, as a rule the user types does: the existing sentence "unless you type it into a rule yourself" becomes "unless you put it into a rule yourself, by typing it or by making a rule from a transaction";
  - how to remove a fingerprint ("Zrušiť zaradenie") and everything (delete the folder), and that `settings.json` also keeps whether the privacy box is switched off.
- **mvp.md**'s "only rules are saved" says that fingerprints of transactions categorized by hand are saved too, and nothing else from statements.

## Screens

The transactions card on "Výpis", with "Nezaradené výdavky" chosen, the second row selected and the third categorized by hand (pointed at, its tag would be a button too):

```
+------------------------------------------------------------------+
| Transakcie  Nezaradené výdavky                                   |
| [Transakcia je zaradená do „Potraviny › Supermarkety“.]          |
|  3. 9.   KAVIAREŇ U ZLATEJ LYŽIČKY   Nezaradené výdavky  -4,20 € |
| (12. 9.  LIDL BRATISLAVA…    [Zaradiť]                  -23,45 €)|
| 14. 9.   Ján Vzorový         Ostatné  ručne             -50,00 € |
|  ...                                                             |
+------------------------------------------------------------------+
```

A selected row with a hand choice: `(14. 9.  Ján Vzorový…  [Zaradiť] [× Zrušiť zaradenie]  -50,00 €)`; the description is cut with "…" to make room.

The "Zaradiť" box over the dimmed window, with "Pravidlom…" chosen:

```
        +----------------------------------------------------+
        | Zaradiť transakciu                                  |
        | 12. 9. 2026   LIDL BRATISLAVA 1234       -23,45 €   |
        |                                                     |
        | Kategória     [Potraviny            v]              |
        | Podkategória  [Supermarkety         v]              |
        |                                                     |
        | ( ) Len túto transakciu                             |
        | (•) Pravidlom pre všetky podobné transakcie         |
        |     Podmienka [Text v popise     v]                 |
        |               [obsahuje v] [LIDL BRATISLAVA 1234 ]  |
        |     Táto transakcia podmienke nezodpovedá. …        |
        |                                                     |
        |                          [Zaradiť]  [Zrušiť]        |
        +----------------------------------------------------+
```

With "Len túto transakciu" chosen, the "Podmienka" lines are not there and the box is shorter.

The privacy box, over the "Zaradiť" box:

```
        +----------------------------------------------------+
        | Čo si DocuSpend zapamätá                            |
        | Aby DocuSpend túto transakciu zaradil rovnako aj    |
        | nabudúce, uloží si jej odtlačok. … Odtlačok zostáva |
        | len v tomto počítači, v priečinku DocuSpend, …      |
        |                                                     |
        | Z odtlačku sa tieto údaje nedajú spätne zistiť. …   |
        |                                                     |
        | Odtlačok je uložený … %APPDATA%\DocuSpend\rules.json|
        |                                                     |
        | [ ] Túto správu už nezobrazovať                     |
        |                          [Zaradiť]  [Späť]          |
        +----------------------------------------------------+
```

## Acceptance criteria

Choosing a transaction and the box:
- [ ] Given a category chosen on "Výpis", when the user selects a transaction, then its row shows "Zaradiť"; given another category chosen, then no row is selected.
- [ ] Given "Zaradiť", then the box "Zaradiť transakciu" shows over the dimmed window with the transaction's date, description and amount; "Zrušiť" and Escape close it and nothing changes.
- [ ] Given an uncategorized transaction, then no sub-category is chosen at first; given a categorized one, its sub-category is. Neither "Nezaradené" bucket is offered.
- [ ] "Len túto transakciu" is chosen at first and shows no condition; choosing "Pravidlom…" shows the condition fields.
- [ ] Given a card payment, a transfer with a counterparty name, a transfer with only an account, and a fee, then the kind chosen at first is "Text v popise", "Protistrana", "Účet protistrany" and "Text v popise".
- [ ] Given each of the six kinds, then its fields start as the table above says, also after switching from another kind; given a transaction missing that detail, the field is empty and "Zaradiť" gives the usual reason.
- [ ] Given no sub-category chosen, then "Zaradiť" says "Vyberte podkategóriu." and nothing changes.

Just this transaction:
- [ ] Given an uncategorized transaction and a sub-category, when the user confirms "Len túto transakciu" (and the privacy box), then that transaction is in the sub-category, every other transaction is where it was, including one with the same date, amount, description and counterparty, and the card says "Transakcia je zaradená do „…“."
- [ ] Given a transaction a user rule and a built-in rule both match, then a hand choice still decides it; a rule added later that matches it doesn't change it.
- [ ] Given a transaction with a hand choice, when the user makes another, then the new one replaces it.
- [ ] Given the last transaction of a category moved away, then the message still shows.
- [ ] The charts, "Výdavky spolu", "Kategórie" and the transactions follow at once.

A rule from a transaction:
- [ ] Given "Pravidlom…" with the condition as filled in, then a user rule with that condition and target is added on top, every matching transaction of the open statement moves, the card says how many, and "Moje pravidlá" lists it first.
- [ ] Given a condition changed so it doesn't match the transaction, then "Táto transakcia podmienke nezodpovedá. Upravte podmienku." shows, the box stays open, and no rule is added.
- [ ] Given an invalid condition (empty text, upside-down amounts, no type ticked), then the reason of "Pravidlá" shows and nothing is added.
- [ ] Given a transaction with a hand choice, when a rule is made from it, then its hand choice is gone and the rule decides it; another matching transaction with a hand choice keeps it.

The privacy box:
- [ ] Given "Len túto transakciu" confirmed, then the privacy box shows the text above exactly, with "Túto správu už nezobrazovať" not ticked.
- [ ] Given "Späť" or Escape, then the "Zaradiť" box is back as it was and nothing changed.
- [ ] Given "Zaradiť" without the tick, then the next hand choice shows the box again; with the tick, it doesn't, also after a restart.
- [ ] Given "Pravidlom…", then no privacy box shows.
- [ ] At the default window size, in both looks, the privacy box shows all its text without scrolling.

The mark and undoing:
- [ ] Given a hand choice, then the row shows "ručne", with the tooltip "Zaradené ručne, len táto transakcia."; pointing at it, or selecting the row, shows "× Zrušiť zaradenie". A transaction placed by a rule shows no tag.
- [ ] Given "× Zrušiť zaradenie", then without a question the transaction goes where the rules put it, the tag is gone, the card says "Zaradenie je zrušené. Transakcia je opäť v „…“.", and after a restart it is still undone.

Kept and recognised:
- [ ] Given a hand choice, when DocuSpend restarts and the same sample is opened, then the transaction is in the chosen sub-category with the tag.
- [ ] Given two statements whose periods overlap and both contain the transaction (synthetic), then a hand choice made in one applies in the other.
- [ ] Given two transactions with the same date, amount, description and counterparty, when the second is categorized by hand, then after a restart the second has the hand choice and the first doesn't.
- [ ] Given the same transaction with its description changed, then the hand choice doesn't apply to it.
- [ ] After making hand choices on every valid sample and closing DocuSpend, nothing DocuSpend wrote contains the date, amount, description, counterparty name or account of a transaction categorized by hand, nor a statement's file name or folder.
- [ ] A file of the new shape keeps the user's rules, categories and hand choices; the committed `rules-v1.json` and `rules-v2.json` still load with the same rules and no hand choices; a new committed file of the new shape is added and fixed the same way.
- [ ] Given a file of the new shape with a hand choice whose target sub-category doesn't exist, then it is treated as #13 treats such a rule (damaged for a user sub-category, newer version for a built-in one).

Saving:
- [ ] Every hand choice and every undo is saved before its message shows.
- [ ] Given a folder that can't be written, and separately saving off this session, then a hand choice and an undo apply with the texts in the table above.

Docs, look, privacy and hand checks:
- [ ] PRIVACY.md and mvp.md say what "PRIVACY.md and mvp.md" above lists; the privacy box and PRIVACY.md agree.
- [ ] The boxes, the tag and the buttons use only the shared look; the look test passes.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.
- [ ] Every new text on screen is in Slovak.
- [ ] The maintainer has approved screenshots, in both looks, at the default window size: the transactions card with a selected row and a row tagged "ručne" (and with the tag pointed at), the "Zaradiť" box with each choice, a refused condition, and the privacy box.
- [ ] Hand check (maintainer): with a real statement open, categorize one transaction by hand and another by a rule, undo a third hand choice, restart DocuSpend, reopen the statement and find everything as left; then open the folder and check that `rules.json` holds no detail of the transactions.

## Edge cases and errors

- **A transaction with no description and no counterparty** (some fees). It can be categorized by hand; "Text v popise" starts empty, so a rule needs a text or another kind.
- **An unknown type.** The type field starts with nothing ticked, as the form doesn't offer the unknown type; "Text v popise" works, as the note on unknown types says.
- **Income.** A transaction coming in can be categorized like one going out, into any category; its role decides how it counts, as for a rule.
- **Many identical transactions** (three coffees of the same price at the same place on one day). Each is told apart by its place among them; categorizing the second by hand moves only the second.
- **A statement downloaded again with a transaction added or removed among identical ones on the same day.** The places shift and a hand choice may move to its neighbour. This is the limit of recognising transactions by their details; it is rare, and visible from the tag.
- **A hand choice in a category the user later empties of rules.** It stays; hand choices don't depend on rules.
- **Long descriptions** are cut with "…" in the row and in the box; the condition field holds the full text.
- **Opening another statement while a box is open.** It can't be done: the box covers the window until answered, like the deletion box.
- **Many hand choices.** A thousand load and save without a noticeable pause, as a thousand rules do.
- **The hand choice's target sub-category is in a category excluded from spending.** The transaction leaves the pie, as for a rule.

## Out of scope

- Editing an existing rule from the transaction ([#24](../024-edit-rules/spec.md), on "Pravidlá").
- Categorizing several transactions at once.
- Showing which rule put a transaction where it is.
- Creating a category or sub-category from the box ([#25](../025-own-categories/spec.md), on "Kategórie").
- A list of all hand choices, or managing them anywhere but on the transaction.
- Bringing the privacy box back once switched off.
- Showing, before confirming, how many transactions a rule would catch.
- Recognising a transaction whose details the bank changed.
- Sharing hand choices; they are never part of rule sharing ([mvp.md](../../docs/mvp.md), future directions).

## Decisions

- **A choice of two, "Len túto transakciu" and "Pravidlom pre všetky podobné transakcie", instead of a tick box.** The issue sketched a tick box; two named options say what each does, and only the rule needs a condition, so its fields show only with it.
- **"Len túto transakciu" is chosen at first.** It changes nothing but this transaction, so a quick "Zaradiť" can't move others by surprise.
- **The kind chosen at first depends on the transaction**: a card payment's description is the merchant; a transfer's description is a message, and its counterparty is what repeats.
- **A rule that wouldn't catch the transaction it was made from is refused.** The box exists to categorize this transaction; a rule that misses it is almost always a slip in editing the condition.
- **A rule made from a transaction replaces that transaction's hand choice**, so the user sees the rule they just made decide the transaction they made it from. Other transactions' hand choices still win, as everywhere.
- **Undo doesn't ask.** It can be redone in two clicks, and it says where the transaction went.
- **The privacy box shows the whole explanation, not a summary with a link.** DocuSpend can't open a web page or another program (decision 0006), and the maintainer wants the user told as fully as possible; the text says "v tomto počítači", as the maintainer asked.
- **The privacy box's switch is kept with the look setting** (`settings.json`), since it is a preference, not part of the categorization, and losing it costs only seeing the box once more.
- **"Späť" in the privacy box returns to the "Zaradiť" box** rather than cancelling everything, so a user who wants to think again can switch to a rule.
- **The fingerprint uses the booking date, amount, description and counterparty name and account, and the place among identical ones**, as the intent lists. Nothing that names the file or the statement is used, so a statement downloaded again or overlapping still matches.
- **Hand choices are kept with the rules, in a new version of the saved shape**, as the issue suggested, so they are saved, failed and recovered exactly as rules are (#13).
- **A hand choice no statement matches is kept.** DocuSpend can't know whether the statement will be opened again.

## Open questions

None.
