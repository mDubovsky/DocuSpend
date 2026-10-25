# Spec: Open a statement the user chooses

Intent: [intent.md](intent.md)

## Behavior

The sample list in the header goes. In its place the user opens a statement of their own, or tries DocuSpend on one example. Everything a statement shows today (key figures, both charts, the category and transaction lists, the rule form and the rule list) stays as it is and works the same for an opened statement.

### Starting
- DocuSpend starts on the **start screen**: the header with the look switch, and one card below it in place of the statement. The card says:
  - a heading: "Otvorte výpis z účtu";
  - a line: "Vyberte mesačný výpis z účtu VÚB vo formáte PDF, alebo ho sem potiahnite.";
  - a line: "Výpis sa číta len vo vašom počítači. DocuSpend ho nikam neposiela ani neukladá.";
  - the primary button **"Otvoriť výpis…"**;
  - a secondary button **"Vyskúšať na ukážke"**.
- The rule form, the rule list and the charts are not shown until a statement is open: without a statement, a new rule has nothing to show its effect on.
- The saved rules are loaded at start as today, and apply to every statement opened.

### Opening a statement
- **By the open window:** "Otvoriť výpis…", on the start screen or in the header once a statement is shown, opens the standard Windows open window. It shows PDF files ("Výpisy vo formáte PDF") by default, and offers all files too. Ctrl+O does the same.
- **By dropping:** the user drags one file onto any part of DocuSpend's window, on the start screen or over a statement. While a file is dragged over the window, the window shows "Pustite výpis sem". Dropping it opens it.
- **By the example:** "Vyskúšať na ukážke" opens the synthetic sample `01-typical-month`, which travels with DocuSpend.
- **Reading:** while the file is read and categorized, the window stays responsive and shows "Výpis sa číta…"; opening another file waits until it is done. A monthly statement takes well under a second, so usually this is barely seen.
- **Showing it:** a statement that reads is shown in place of the start screen, or in place of the statement shown before. Nothing is selected in it; the rule form keeps what the user typed. The header shows the statement's period, as today, and the "Otvoriť výpis…" button.
- **The example is labelled.** While the example is shown, the header says "Ukážka s vymyslenými údajmi" next to the period, so nobody takes it for their own account.
- **Cancelling** the open window changes nothing and shows nothing.

### When a file can't be opened
- Nothing is shown from a file that can't be read: no figures, no chart, not even part of one. What was on screen before (the start screen or the previous statement) stays, and works.
- A message appears under the header, in the look's error style, with a close button ("Zavrieť"). It starts with "Výpis sa neotvoril." and then says what is wrong and what to do. When a statement stays on screen, it ends with "Zobrazený zostáva predchádzajúci výpis."
- The message stays until the user closes it or opens something else. A statement that opens successfully removes it.
- The messages, after "Výpis sa neotvoril.":

  | Problem | Message |
  |---|---|
  | The file is empty | "Súbor je prázdny. Stiahnite výpis z internet bankingu znova." |
  | The file isn't a PDF, or is damaged or protected by a password | "Súbor nie je PDF, alebo je poškodený či chránený heslom. Stiahnite výpis z internet bankingu znova, bez hesla." |
  | A PDF, but not a VÚB monthly statement (another bank, an invoice, …) | "Tento súbor nie je mesačný výpis z účtu VÚB. DocuSpend zatiaľ vie čítať len tie." |
  | Looks like a VÚB statement, but part of it can't be read | "Časť výpisu sa nedá prečítať. Výpis má možno podobu, ktorú DocuSpend ešte nepozná." |
  | Read, but doesn't add up to its own printed totals | "Pohyby vo výpise nesedia s jeho súčtami, preto by grafy neboli správne." |
  | The file can't be opened from disk (moved, deleted, in use, no permission) | "Súbor sa nedá otvoriť. Možno bol presunutý alebo ho práve používa iný program." |
  | The file is far larger than any monthly statement (over 50 MB) | "Súbor je príliš veľký na mesačný výpis." |

- Dropping something that can't be opened at all gives a message on its own, without "Výpis sa neotvoril.", and changes nothing else:
  - several files at once: "Pretiahnite naraz len jeden výpis.";
  - a folder, or something that isn't a file (text, a link from a browser): "Sem môžete pretiahnuť len súbor s výpisom."
- No message contains anything from the file: no name, folder, account, amount or text from inside it. No message is in English. The reader's own reasons, written for developers, are never shown.

### Nothing is kept
- The statement is read into memory and nowhere else. DocuSpend writes no copy, no cache, no log and no list of recent files.
- What DocuSpend writes on the user's computer is unchanged by this item: the look setting and the rules. Neither mentions a statement, a file name or a folder.
- DocuSpend doesn't add the statement to Windows' list of recent files.
- Windows may remember, for its own open window, the folder last used in it. That is Windows' setting, not DocuSpend's (intent: decisions), and [PRIVACY.md](../../PRIVACY.md) says so.

### PRIVACY.md
- It says what DocuSpend does with the file the user opens: reads it in memory and shows it, keeps nothing, and that Windows' open window may remember the last folder.
- Its "Where things stand" note, which says the application isn't built yet, is brought up to date.

### Screenshots
- The screenshot command also shows the start screen and the start screen with a message (sample `06`), in both looks; the example statement with its label; and a statement with a message above it.

## Acceptance criteria

Automated, unless marked as a hand check.

- [ ] Given DocuSpend starts, then the start screen is shown with its texts and both buttons, and no statement, chart or rule form.
- [ ] Given the start screen, when the user chooses "Vyskúšať na ukážke", then `01-typical-month` is shown, with "Ukážka s vymyslenými údajmi" in the header.
- [ ] Given any screen, when each valid sample (`01` to `05`) is opened from disk, then it is shown with the same figures, categories and transactions as the reader and categorizer give for it, and without the example label.
- [ ] Given the start screen, and given a statement on screen, when each invalid sample (`06` to `09`) is opened, then the message for its kind of problem is shown (`06` and `07`: not a VÚB statement; `08`: not a PDF or damaged; `09`: empty), and what was on screen before is unchanged and still works.
- [ ] Given a statement that doesn't add up, and given a part of a statement that can't be read, when each is opened, then its own message is shown. (No committed sample covers them; the tests use generated variants of a sample, never real data.)
- [ ] Given a file that has been deleted, or that another program holds locked, when it is opened, then the "can't be opened" message is shown.
- [ ] Given a file over 50 MB, when it is opened, then the "too large" message is shown and the file isn't read.
- [ ] Given a statement on screen with a message, when another valid statement is opened, then it is shown and the message is gone; when the user closes the message, it is gone and the statement stays.
- [ ] When a file is dropped onto the window, it is opened exactly as through the open window; when several files are dropped, or a folder, or text, then their message is shown and nothing else changes.
- [ ] Given a statement is open, then the saved user rules apply to it, and adding a rule works on it as today.
- [ ] Every message is in Slovak and contains nothing from the file: checked for every message against the samples' names, account numbers and file names.
- [ ] Given statements opened and DocuSpend closed, then nothing DocuSpend wrote mentions those statements: not their content, file names or folders.
- [ ] Hand check (maintainer): the open window shows PDF files by default, offers all files, opens a chosen statement, and cancelling it changes nothing; Ctrl+O opens it.
- [ ] Hand check (maintainer): dropping a statement from Windows Explorer opens it, and "Pustite výpis sem" shows while it is dragged.
- [ ] Hand check (maintainer): after opening a statement, it is not in Windows' recent files (Quick access "Recent", the taskbar's jump list).
- [ ] Hand check (maintainer): the maintainer's real statements open and show, in the built application.
- [ ] The maintainer has approved the screenshots of the new and changed screens, in both looks.
- [ ] PRIVACY.md says what DocuSpend does with an opened file, mentions Windows' folder memory, and is no longer out of date.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.

## Edge cases and errors

- **A file renamed to `.pdf`, or a PDF without the extension.** Judged by its content: a non-PDF gets the not-a-PDF message, a statement without the extension opens.
- **A statement in a folder with diacritics, a long path, a network drive or a synced cloud folder.** Opens like any other. Fetching it from the network is Windows' doing, not DocuSpend's (PRIVACY.md already says so).
- **The same statement opened twice.** It is read again and shown again; nothing is selected.
- **A file that changes or disappears after it was opened.** Nothing happens: DocuSpend works from what it read.
- **A drop or Ctrl+O while a file is being read.** Ignored until the reading is done.
- **A drop onto the rule form's text box.** Opens the file like a drop anywhere else; the text box gets nothing.
- **The empty month (`04`) and the one-transaction month (`05`).** Shown as today, with their empty states.
- **The example missing from DocuSpend's folder** (a damaged installation). "Vyskúšať na ukážke" isn't shown; opening a statement still works.
- **The saved rules can't be read.** The existing message about the rules file is shown as today, under the statement's rules; opening statements works.

## Out of scope

- Several statements at once, combining months, other banks (intent: non-goals; [mvp.md](../../docs/mvp.md)).
- Remembering recent files, reopening the last statement, saving parsed statements.
- Opening from Windows Explorer ("Open with", double-click) or the command line.
- Closing a statement to go back to the start screen.
- Saving rules and categories properly ([#13](https://github.com/mDubovsky/DocuSpend/issues/13)); the spike's rules store stays as it is.
- Rearranging the statement screen beyond the header and the message.

## Decisions

- **The start screen shows no rule form.** A rule's effect ("matched N transactions") needs a statement, and an empty form would be the first thing a new user tries to understand.
- **One primary button per screen.** The look ([#11](../011-modern-look/spec.md)) made "Pridať pravidlo" the only filled button. The start screen has no rule form, so "Otvoriť výpis…" is its primary button; in the statement view's header it is a normal button and "Pridať pravidlo" stays the primary one.
- **"Doesn't add up" is a message of its own**, not folded into "can't be read". It is the case where DocuSpend could show numbers but refuses because they would be wrong, and saying so is the MVP's "never a silently wrong chart".
- **Files over 50 MB are refused before reading.** A monthly statement is well under 1 MB; a file a hundred times larger is not one, and reading it could make DocuSpend slow for nothing.
- **No file name in messages or on screen.** The statement's period identifies what is shown, and leaving the name out keeps every message free of anything from the user's computer.
- **The message stays until closed or replaced.** A message that vanishes on its own can be missed; one that stays over a working statement would be in the way, so it can be closed.
- **The open window offers all files, not only PDFs.** Files are judged by content (intent: decisions), so a statement saved without its extension can still be opened.

## Open questions

None.
