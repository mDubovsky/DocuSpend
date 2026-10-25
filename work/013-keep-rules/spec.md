# Spec: Keep rules and categories between sessions

Intent: [intent.md](intent.md)

## Behavior

The screens stay as they are. What changes is that the user's rules and categories are still there after DocuSpend is closed and started again, and that problems with keeping them are reported.

### What is kept
- **The user's own part of the categorization**, all of it, whichever screen changed it (today the rule form; later the rule editor):
  - every user rule: its identifier, its condition, its target sub-category, its priority number and its creation order. All six kinds of condition are kept: transaction type (one or several types), text, counterparty name, counterparty account, amount (from, to or both) and variable symbol, with their "contains" or "equals" choice where they have one;
  - every user category: its identifier, name and role (spending, income or excluded), with its sub-categories, including its own Ostatné;
  - every user sub-category added to a built-in category: its identifier and name.
- **Exactly as entered.** Texts are kept letter for letter, including case, diacritics and spaces, and amounts to the cent: a rule typed as "Lekáreň" with an amount up to "12,50" comes back as exactly that.
- **Not kept:** the built-in categories and default rules (they come from the DocuSpend that runs), the look setting (kept separately, as today), and anything about statements: which were opened, their names or folders, or anything read from them.
- **Where:** in DocuSpend's own folder in the user's application data (`%APPDATA%\DocuSpend`), the folder [PRIVACY.md](../../PRIVACY.md) already names. DocuSpend creates the folder when it first saves.

### Saving
- DocuSpend saves after every change to the user's part: a rule added (today), and later any rule edited, deleted or moved, or a category or sub-category added. There is no save button.
- The change is saved before DocuSpend confirms it. The rule form's confirmation ("Pravidlo zaradilo 3 transakcie." or "Pravidlo je uložené, ale v tomto výpise mu nezodpovedá žiadna transakcia.") appears only for a rule that was saved.
- A save either completes or leaves the previous saved rules as they were. If DocuSpend or Windows ends during a save, the next start finds either the rules from before the change or the rules after it, never a mixture or a damaged file.

### Starting
- **Nothing saved yet** (first start, or the user removed the folder): DocuSpend starts with the built-in categories and default rules only, and says nothing.
- **Saved rules that read:** every user rule and category is back before the first statement is opened. "Moje pravidlá" lists the user rules in the order they were created, as before closing. Every statement is categorized exactly as it would have been before closing: each transaction in the same sub-category, decided by the same rule.
- **"Moje pravidlá" describes every kind of rule.** Today it can describe only the kinds the form makes. A loaded rule of another kind is described as "Protistrana obsahuje „…“" or "Protistrana sa rovná „…“" (counterparty name), "Účet protistrany: …" (counterparty account) or "Variabilný symbol: …" (variable symbol).
- **Rules saved by an earlier version of DocuSpend** load the same way and give the same categorization. The next save writes them in the current shape.

### When the saved rules can't be used
In each case DocuSpend still starts and works, with the built-in categories and default rules. A message appears under the header, in the look's error style, on the start screen and over a statement alike, until the user closes it ("Zavrieť"). It doesn't replace the message about a statement that wasn't opened; both can show.

| Situation | What DocuSpend does | Message |
|---|---|---|
| **Damaged:** the saved rules can't be understood (garbled, cut short, empty, a rule whose condition is incomplete, a rule pointing at a user sub-category that isn't there) | Moves the file aside, in the same folder, under a name with the date and time, and starts without user rules. Saving works as usual from then on. | "Uložené pravidlá boli poškodené, preto DocuSpend začína bez nich. Poškodený súbor je odložený tu: {cesta}" |
| **Can't be opened right now:** the file is in use by another program, or the user's account may not read it | Leaves the file untouched, starts without user rules, and saves nothing until DocuSpend is closed. | "Uložené pravidlá sa teraz nedajú otvoriť, možno ich používa iný program. DocuSpend začína bez nich a kým ho nezavriete, zmeny pravidiel neuloží. Súbor zostáva nedotknutý." |
| **Saved by a newer DocuSpend:** the file says it is of a later shape than this version knows, or a rule points at a built-in sub-category this version doesn't have | Leaves the file untouched, starts without user rules, and saves nothing until DocuSpend is closed. | "Pravidlá uložila novšia verzia DocuSpend a táto verzia ich nevie použiť. Aby ste o ne neprišli, DocuSpend začína bez nich a zmeny pravidiel neuloží. Otvorte novšiu verziu DocuSpend." |

`{cesta}` is the full path of the moved file, so the user can find it. No message contains anything from the file itself.

- **While saving is off** (the last two rows), "Moje pravidlá" says "Zmeny pravidiel sa teraz neukladajú." for the rest of the session. The user can still add rules; they apply until DocuSpend closes. The form confirms such a rule as usual and adds "Pravidlo platí len do zatvorenia DocuSpend."

### When a change can't be saved
- If a save fails (the folder can't be created or written, the disk is full), the change still applies until DocuSpend closes, and the rule form says, under its usual confirmation: "Pravidlo sa nepodarilo uložiť, preto po zatvorení DocuSpend zmizne. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova."
- The next save, if it works, keeps everything on screen, including the change that failed before.

### The spike's file
DocuSpend no longer reads the spike's rules file (`spike-rules.json`) and doesn't bring its rules over. It doesn't delete or change it either. [PRIVACY.md](../../PRIVACY.md) says it may be there from a development build and can be deleted.

### PRIVACY.md
Says what is kept and where (as in "What is kept"), that a damaged file is kept aside in the same folder, and how to remove everything: delete the folder `%APPDATA%\DocuSpend`.

## Acceptance criteria

Kept and restored:
- [ ] Given a user rule of each of the six kinds of condition (type with one and with several types; text "contains" and "equals"; counterparty name "contains" and "equals"; counterparty account; amount with from, to and both; variable symbol), a user category of each role with a user sub-category, and a user sub-category in a built-in category, when DocuSpend saves and a new DocuSpend loads what was saved, then every rule has the same identifier, condition, target, priority number and creation order, and every category and sub-category the same identifier, name and role.
- [ ] Given those rules, then every valid sample (`01` to `05`) is categorized the same before and after: every transaction in the same sub-category, decided by the same rule.
- [ ] Given user rules sharing a priority number, and a rule whose priority number was changed after it was created, when they are saved and loaded, then the same rule decides for every transaction as before.
- [ ] Given a rule added after loading, then it gets a creation order after every loaded rule, and wins a tie with them.
- [ ] Texts with diacritics, capitals, inner spaces and characters outside Slovak (`Café „Ž“ 😀`), and amounts with cents, come back exactly as entered.
- [ ] In the window: given a rule added through the form, when the window is closed and a new window is started on the same folder, then "Moje pravidlá" lists the same rules in the same order, and the opened sample shows the same categories as before.
- [ ] A built-in category or default rule is never in what is saved: changing a built-in category's name or a default rule in DocuSpend changes nothing in a saved file, and a loaded file still uses the current built-in names and default rules.

Saving:
- [ ] Given a rule added through the form, then it is saved before the confirmation appears.
- [ ] Given a save interrupted at any point (simulated), then the next start loads either the rules before the change or after it, with no message.
- [ ] Given a folder that can't be written, when the user adds a rule, then the rule applies to the statement, the form shows the confirmation and "Pravidlo sa nepodarilo uložiť, …", and when writing works again, the next rule added saves both.

Starting and problems:
- [ ] Given no saved rules, then DocuSpend starts with no message and with only built-in categories and default rules.
- [ ] Given a committed saved file of the first shape (synthetic, covering every kind of condition, user categories and shared priority numbers), then it loads with the expected rules and categorization. The test stays as the shape changes, so every later version must still load it.
- [ ] Given a damaged file (each: garbled, cut short, empty, an incomplete condition, a target user sub-category that doesn't exist, a user sub-category in an Uncategorized bucket), then DocuSpend starts without user rules and shows the damaged-file message with the moved file's path; the moved file is byte for byte the original; the next rule added is saved; nothing else in the folder changed.
- [ ] Given two damaged files found on two starts, then both moved files are kept, under different names.
- [ ] Given a file that can't be opened (held open by another program), then DocuSpend starts without user rules, shows the can't-open message and "Zmeny pravidiel sa teraz neukladajú.", a rule added applies with "Pravidlo platí len do zatvorenia DocuSpend.", and the file is unchanged when DocuSpend closes.
- [ ] Given a file of a later shape, or with a rule pointing at a built-in sub-category that doesn't exist, then the same as the row above, with the newer-version message.
- [ ] The startup message shows on the start screen and over a statement, can be closed, and shows next to a message about a statement that wasn't opened.
- [ ] No message is in English, and none contains anything from the saved file other than the moved file's path.

The spike's file and privacy:
- [ ] Given a `spike-rules.json` with rules, then DocuSpend starts with no user rules from it and no message, and the file is unchanged.
- [ ] After opening every valid sample, adding rules through the form and closing DocuSpend, nothing DocuSpend wrote contains a transaction's description, counterparty, account, amount or date, or a statement's file name or folder, other than what the test typed into a rule.
- [ ] PRIVACY.md describes what is kept and where, the moved-aside file, the spike's file, and how to remove everything.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.

By hand:
- [ ] Hand check (maintainer): in the built application, add a rule while viewing a real statement, close DocuSpend, start it again, open the same statement, and see the rule listed and applied.
- [ ] Hand check (maintainer): with the saved file opened exclusively by another program, DocuSpend starts with the can't-open message.
- [ ] The maintainer has approved screenshots of the startup messages and of "Moje pravidlá" while saving is off, in both looks.

## Edge cases and errors

- **Many rules.** A thousand user rules load and save without a noticeable pause (under a second each).
- **A leftover from an interrupted save.** It is never mistaken for the saved rules, and doesn't stop the next save.
- **The folder `%APPDATA%\DocuSpend` exists but the file doesn't.** Same as nothing saved.
- **A file that is valid but has no rules or categories.** Loads as no user rules, with no message.
- **A user sub-category with the same name as a built-in sub-category added later in the same category.** Both stay and both work; a matching name is not damage. Telling them apart on screen is left to the rule editor.
- **A file that is damaged in only one rule.** The whole file is treated as damaged and moved aside; no rule from it is used. Using the rest would bring back a categorization that isn't the one the user had.
- **The damaged file can't be moved aside** (the folder can't be written). DocuSpend treats it like a file that can't be opened right now: untouched, nothing saved this session, and the can't-open message.
- **Two DocuSpend windows at once.** Each saves its own changes; the last save wins, and the file is never damaged. Neither sees the other's changes until it is started again (intent: decisions).
- **The user edits the file by hand.** If it still reads, it is used; if not, it is damaged.
- **The user deletes the folder while DocuSpend runs.** The next save creates it again.
- **No statement opened.** Saved rules still load at start, and a startup message still shows on the start screen.

## Out of scope

- The rule editor: listing, editing and deleting rules, changing priorities ([#24](https://github.com/mDubovsky/DocuSpend/issues/24)), and creating categories on screen ([#25](https://github.com/mDubovsky/DocuSpend/issues/25)) (intent: non-goals). This item keeps whatever those will change, and the screen-level tests cover only what the form can do today.
- Showing priority numbers on screen.
- Exporting, importing, sharing or synchronizing rules; backups beyond the moved-aside damaged file.
- Restoring a moved-aside or newer-version file from within DocuSpend.
- Saving statements or anything read from them.
- Changing how the look setting is kept.

## Decisions

- **Rules added while saving is off, or after a failed save, still apply until DocuSpend closes.** Until now the form refused a rule it couldn't save. Applying it, and saying it won't be kept, lets the user keep working; refusing would make one locked file block all categorization for the session.
- **A file damaged in one place is damaged as a whole.** Like a statement ([#2](../002-statement-reader/intent.md)), a partial result would silently differ from what the user had. The moved-aside file keeps everything.
- **A rule pointing at an unknown built-in sub-category means a newer DocuSpend.** Built-in identifiers are never retired ([decision 0004](../../docs/decisions/0004-stable-category-and-rule-identifiers.md)), so an unknown one can only come from a later version. Moving the file aside would cost the user their rules the moment they switch back.
- **Moved-aside files carry the date and time in their name**, so a second damaged file never replaces the first.
- **The startup message is under the header, not in "Moje pravidlá".** The rules list isn't shown on the start screen ([#12](../012-open-statement/spec.md)), and the user should hear about their rules when DocuSpend starts, not only after opening a statement.
- **The first shape is fixed by a committed saved file and its test.** There is no earlier shape yet; the file is how "a file from an earlier version still loads" is checked from now on.
- **The six kinds of condition, user categories and priority changes are tested below the screen.** The form can make only text, amount and single-type rules today, but everything the categorization can hold must survive, so the rule editor needs no change to saving.

## Open questions

None.
