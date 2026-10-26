# Spec: Edit, delete and reorder rules

Intent: [intent.md](intent.md)

## Behavior

The editor lives in the "Moje pravidlá" card and the "Nové pravidlo" form, where they are today: shown while a statement is open ([#12](../012-open-statement/spec.md)). Moving them to a screen of their own is [#21](https://github.com/mDubovsky/DocuSpend/issues/21).

### The rules card
- The card's heading is a pair of pills, **"Moje pravidlá"** and **"Vstavané pravidlá"**, in the look of the light and dark switch. "Moje pravidlá" is chosen when DocuSpend starts; choosing the other switches the card to the built-in rules, and back.
- "Zmeny pravidiel sa teraz neukladajú." (#13) shows under the pills in either view.

### "Moje pravidlá"
- The list shows the user rules **in the order they are tried**: by priority number, 1 first, and rules with the same number newest first. The rule higher up wins. (Until now the list was in creation order.)
- Each row shows the rule's condition and its target, as today ("Text obsahuje „lidl“", "Potraviny › Supermarkety"), and two buttons: **"Upraviť"** and **"Odstrániť"**. **Priority numbers are not shown in the list**; they show only in the form while a rule is edited.
- A rule added through the form goes on top, as today (it gets the lowest number any user rule has, 1 if there are none, and wins the tie by being newest), so it appears first in the list.
- With no user rules, the list says "Zatiaľ žiadne pravidlá.", as today.

### The form offers every kind of condition
The MVP's user rules can match on six kinds of condition ([mvp.md](../../docs/mvp.md), [#3 spec](../003-categorize-transactions/spec.md)); the form has offered only three. It now offers all six, for adding and editing alike:

| Kind (as the form names it) | Fields | Shown in the list as |
|---|---|---|
| "Text v popise" | "obsahuje" or "rovná sa", and the text | "Text obsahuje „…“" / "Text sa rovná „…“" |
| "Protistrana" | "obsahuje" or "rovná sa", and the name | "Protistrana obsahuje „…“" / "Protistrana sa rovná „…“" |
| "Účet protistrany" | the account number | "Účet protistrany: …" |
| "Suma" | "Od", "Do" or both | "Suma od … do …" / "Suma od …" / "Suma do …" |
| "Typ transakcie" | one or several types, each with its own tick box | "Typ: Platba kartou, Prevod" |
| "Variabilný symbol" | the symbol | "Variabilný symbol: …" |

Each is checked as the categorization already checks it, with its existing Slovak reason: no type ticked ("Vyberte typ transakcie."), an empty name or text ("Zadajte text, ktorý obsahuje aspoň jedno písmeno alebo číslicu."), an empty account ("Zadajte číslo účtu."), an empty symbol ("Zadajte variabilný symbol."). Spaces around a text, account or symbol are removed before it is checked, as for text today.

### Editing a rule
- "Upraviť" opens the rule in the form. The form's heading becomes **"Upraviť pravidlo"**, its fields show the rule's condition (its kind and that kind's fields, as above) and target, and a field **"Priorita"** appears, holding the rule's current number. Next to it is a **help mark** ("?"); hovering over it, or reaching it with the keyboard, shows this tooltip:
  > Pravidlá sa uplatňujú od priority 1. Transakciu zaradí prvé pravidlo, ktorému zodpovedá. Vaše pravidlá môžu mať prioritu od 1 do 255; vstavané pravidlá majú prioritu 256, takže vaše pravidlá majú vždy prednosť. Ak majú dve pravidlá rovnakú prioritu, rozhoduje novšie.

  The add button is replaced by **"Uložiť zmeny"** and **"Zrušiť"**. The row of the rule being edited is marked in the list.
- The user can change anything in it: the kind of condition, its values, the target and the priority.
- **"Uložiť zmeny"** checks the input exactly as adding a rule does, with the same Slovak reasons, and in addition the priority: it must be a whole number from 1 to 255, or the form says "Priorita musí byť celé číslo od 1 do 255." If anything is refused, the rule is left as it was and the form stays open.
- If the input is valid, the rule is changed, saved and the open statement categorized again. The rule keeps its identity and its creation order, so among rules with the same number it stays exactly as new or old as it was. The list moves it to its new place. The form goes back to "Nové pravidlo", empty, and says what the changed rule now does, with the same texts as after adding: "Pravidlo zaradilo 3 transakcie." or "Pravidlo je uložené, ale v tomto výpise mu nezodpovedá žiadna transakcia."
- **"Zrušiť"** goes back to "Nové pravidlo", empty, and changes nothing.
- Saving without changing anything is allowed and changes nothing but the message.

### Deleting a rule
- "Odstrániť" doesn't delete yet. A **pop-up box** opens in the middle of the window, which is dimmed behind it and waits until the box is answered. The box asks **"Odstrániť toto pravidlo?"**, shows the rule's condition and target, and has two buttons, **"Odstrániť"** and **"Ponechať"**. No button is chosen when it opens; Tab moves between the two.
- "Ponechať" closes the box and keeps the rule. So does Escape.
- "Odstrániť" deletes the rule, saves, and categorizes the open statement again. Transactions it decided go to whatever rule now matches them, or to Nezaradené. "Moje pravidlá" says **"Pravidlo je odstránené."** until the next change.
- If the rule being deleted is open in the form, the form goes back to "Nové pravidlo", empty.

### When a change can't be saved
As for a new rule ([#13 spec](../013-keep-rules/spec.md), "When a change can't be saved"), an edit or deletion still applies until DocuSpend closes, and the user is told it won't be kept:

| | Saving is off this session | Saving failed |
|---|---|---|
| Edit (in the form, under the confirmation) | "Zmena platí len do zatvorenia DocuSpend." | "Zmenu sa nepodarilo uložiť, preto po zatvorení DocuSpend neostane. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova." |
| Deletion (in "Moje pravidlá", under "Pravidlo je odstránené.") | "Pravidlo sa po zatvorení DocuSpend vráti." | "Odstránenie sa nepodarilo uložiť, preto sa pravidlo po zatvorení DocuSpend vráti. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova." |

After an edit that wasn't saved and matched nothing, the form says "V tomto výpise pravidlu nezodpovedá žiadna transakcia." instead, as for a new rule.

### "Vstavané pravidlá"
- The card's second view lists every default rule in the order they are tried: its condition and target, with no number and no buttons. Nothing about a default rule can be changed. The list's order shows which built-in rule wins; every user rule wins over all of them.

### What stays the same
- What is saved keeps its shape ([#13](../013-keep-rules/spec.md)); a file saved before this item loads unchanged, and a file saved after it is still of the first shape.
- Adding a rule works as before, including going on top without a number, now with every kind of condition.

## Acceptance criteria

Every kind of condition:
- [ ] Given each of the six kinds, with each of their options ("obsahuje" and "rovná sa"; from, to and both; one and several types), when the user adds a rule of it through the form, then the rule is the one the categorization would make from those values, the list describes it as in the table above, and every valid sample (`01` to `05`) is categorized as with that rule added directly.
- [ ] Given each kind with its required value missing (no type ticked, empty or space-only name, text, account or symbol), then the form shows the reason above and nothing is added.
- [ ] Given a rule of each kind saved and loaded, then "Upraviť" shows its kind and values exactly, and saving it unchanged leaves the rule as it was.

Editing:
- [ ] Given a user rule, when the user presses "Upraviť", then the form is headed "Upraviť pravidlo" and shows the rule's kind, mode, values, target and current priority, with "Uložiť zmeny" and "Zrušiť".
- [ ] Given a text rule, when the user changes its text, its mode, and its target, and saves, then the open statement is categorized exactly as if the rule had been added that way with the same number and creation order, and the form shows how many transactions it now catches.
- [ ] Given a text rule, when the user changes it to each of the other five kinds and saves, then the same holds.
- [ ] Given an edit with an empty text, an amount that isn't a number, an upside-down amount range, no type or no sub-category, then the form shows the same reason as when adding, and the rule, the statement and the saved file are unchanged.
- [ ] Given a priority of "0", "-1", "1,5", "abc", "" or "256", then the form says "Priorita musí byť celé číslo od 1 do 255." and nothing changes; "1" and "255" are accepted.
- [ ] Given an edited rule, then it keeps its identifier and creation order: of two rules with the same number, the newer one still wins after either is edited or renumbered.
- [ ] Given "Zrušiť", then nothing changes and the form is "Nové pravidlo", empty.
- [ ] Given an edited rule, when DocuSpend is restarted, then the rule is back as edited.

Priority and order:
- [ ] Given user rules with numbers 3, 1 and 1 (the second created before the third), then the list shows the third, the second, then the first, with no numbers.
- [ ] Given a rule renumbered from 3 to 1, then it moves in the list to its place among the rules numbered 1 by creation order, and the categorization follows the new order.
- [ ] Given a rule added through the form, then it is first in the list.
- [ ] The help mark by "Priorita" shows the tooltip text above, on hover and on keyboard focus.

Deleting:
- [ ] Given "Odstrániť", then the rule is still there and a pop-up box asks "Odstrániť toto pravidlo?" about that rule, with neither button chosen.
- [ ] Given that question, when the user presses "Ponechať" or Escape, then the box closes and the rule stays.
- [ ] Given that question, when the user presses "Odstrániť", then the rule is gone from the list, the statement is categorized without it, "Pravidlo je odstránené." shows, and after a restart the rule is not back.
- [ ] Given the rule open in the form is deleted, then the form is "Nové pravidlo", empty.
- [ ] Given the last user rule is deleted, then the list says "Zatiaľ žiadne pravidlá." and the next rule added gets number 1.

Saving:
- [ ] Every edit, renumbering and deletion is saved before its confirmation appears.
- [ ] Given a folder that can't be written, when the user edits a rule, and separately deletes one, then the change applies and the matching "Saving failed" text shows; when writing works again, the next change saves everything on screen.
- [ ] Given saving is off for the session, then an edit shows "Zmena platí len do zatvorenia DocuSpend." and a deletion "Pravidlo sa po zatvorení DocuSpend vráti.", and the saved file is unchanged when DocuSpend closes.
- [ ] A file saved after edits and deletions has the same shape as before this item, and the committed first-shape file still loads.

Built-in rules:
- [ ] "Moje pravidlá" is shown at start; switching to "Vstavané pravidlá" lists every default rule in the order tried, with its condition and target, no number and no "Upraviť" or "Odstrániť"; switching back shows "Moje pravidlá" again.
- [ ] No edit, renumbering or deletion of user rules changes the default rules.

Privacy and hand checks:
- [ ] All privacy checks pass, with no new entries in the reviewed lists.
- [ ] No new text is in English.
- [ ] Hand check (maintainer): with a real statement open, edit a rule's condition and target, renumber one, delete one, restart DocuSpend, reopen the statement, and find the rules and the categorization as left.
- [ ] The maintainer has approved screenshots, in both looks, of "Moje pravidlá" with several rules, a row asking to delete, the form in edit mode with the tooltip shown, and "Vstavané pravidlá" chosen.

## Edge cases and errors

- **A saved rule with a priority above 255.** Files saved before this item allowed any number from 1, though the screen never made one. Such a rule loads and works as before, and the form shows its number; it still wins over every built-in rule. Saving it in the form requires a number from 1 to 255.
- **Many rules.** With 200 user rules, the list scrolls within the card, and an edit or deletion shows its result without a noticeable pause.
- **Long texts and names.** A long condition or target is cut with "…" in the row; the full text is in the form when edited.
- **Editing while a new rule is half typed.** "Upraviť" replaces what was typed in the form. "Zrušiť" then leaves the form empty.
- **"Upraviť" on another rule while editing.** The form switches to that rule; unsaved changes to the first are dropped.
- **A priority with spaces around it** (" 2 ") is accepted as 2.
- **Opening another statement while editing.** The form stays as it is; saving then categorizes the newly opened statement.
- **Two rules made identical by an edit.** Allowed, as it is when adding; the newer one wins.

## Out of scope

- Creating, renaming or deleting categories and sub-categories ([#25](https://github.com/mDubovsky/DocuSpend/issues/25)).
- Changing, switching off or deleting default rules, and showing their priority numbers.
- Showing priority numbers in the lists.
- Moving the rules to their own screen ([#21](https://github.com/mDubovsky/DocuSpend/issues/21)); showing which rule decided a transaction, and categorizing one transaction from the list ([#22](https://github.com/mDubovsky/DocuSpend/issues/22)).
- Moving rules up and down or dragging them; choosing a number when adding a rule.
- Undo, and a history of changes.
- Searching or filtering the rules list.

## Decisions

- **The list shows rules in the order they are tried**, not in creation order as before. With the numbers hidden, "higher up wins" is how the list is read, and a new rule on top appears first.
- **Editing reuses the "Nové pravidlo" form** rather than editing in the row. The fields are the same, so the checks and the texts are the same, and one place to edit keeps a narrow card readable.
- **The deletion question is a pop-up box over the dimmed window**, as the maintainer chose after the screenshot review, instead of a question inside the row, which squeezed the rule's text. It is drawn in the window, not as a separate window, so it follows the light and dark look, and nothing in it is chosen at start, as the maintainer asked; it names the rule, since the row is hidden behind it.
- **A user rule's priority is from 1 to 255**, as the maintainer chose: far more than anyone needs, and it leaves 256 to stand for the built-in rules. The existing refusal text changes to name the bound. Adding on top never goes past it, since it reuses the lowest number in use.
- **Priority numbers show only in the form, while a rule is edited**, as the maintainer chose after the screenshot review. The list's order already says which rule wins. The tooltip still names 256 for the built-in rules: their own numbers are separate from the user's and never compete with them, and one number just below every user rule says what is true, that any user rule wins.
- **The form offers all six kinds of condition**, as the maintainer chose. The MVP already lists them for user rules, and editing needs the form to show any rule a saved file can hold. A type rule takes several types, since the categorization allows it and a built-in rule already uses it.
- **The rules card switches between two views with pills in its heading**, as the maintainer chose after the screenshot review, instead of a fold-out section below the user's rules. The help mark is only by "Priorita", the one place a number shows.
- **A deletion's confirmation stays until the next change**, like the form's messages after adding.

## Open questions

None.
