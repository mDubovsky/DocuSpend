---
status: in-progress
issue: 12
---

# Intent: Open a statement the user chooses

Issue: [#12](https://github.com/mDubovsky/DocuSpend/issues/12)

## Problem
DocuSpend can read and categorize a VÚB statement, but a user has no way to give it one. The window only shows the synthetic samples it ships with, picked from a list left over from the spike ([#5](../005-avalonia-spike/findings.md), "Keep or throw away"). A user with a real statement on their computer can't use DocuSpend at all yet. This is the MVP's input ([mvp.md](../../docs/mvp.md)): without it there is a demo, not a product.

It is also where the MVP's "clear failures" promise meets the user. The reader already rejects a file it can't read, with a reason written for developers ([#2](../002-statement-reader/intent.md)). A user needs to be told, in their own terms, what is wrong with the file they chose.

## Desired outcome
- **Choosing a statement:** the user picks a PDF on their computer, either through the familiar Windows "open file" window or by dropping the file onto DocuSpend's window. DocuSpend reads and categorizes it and shows it with the charts and lists, exactly as it shows a sample today.
- **Before any statement:** when DocuSpend starts, it says plainly what to do: open or drop a VÚB monthly statement.
- **Another statement:** the user can open a different statement at any time; it replaces the one on screen.
- **Clear failures:** a file that isn't a readable VÚB statement gives a message in Slovak that says what is wrong in terms the user understands, and what they can do about it. There is a message for each kind of problem:
  - the file is empty;
  - the file isn't a PDF, or is damaged or protected;
  - the file is a PDF but not a VÚB monthly statement (for example another bank's statement, or an invoice);
  - the file looks like a VÚB statement but part of it can't be read;
  - the statement doesn't add up to its own printed totals;
  - the file can't be opened at all (it has been moved, is in use, or the user may not read it).

  A failed file never produces a chart, not even a partial one. If a statement was on screen before, it stays there, and the message makes clear that the new file was not opened.
- **Nothing kept:** the statement is read in memory only. DocuSpend doesn't copy it, keep any of its content, or remember its name or where it was, and none of this changes after DocuSpend is closed.

## Success criteria
- A user can open each valid sample (`01` to `05`) from disk, by the open window and by dropping it, and sees its charts and lists, the same as for that sample today.
- Each invalid sample (`06` to `09`) gives its own clear message, matching the kind of problem, and no chart.
- A statement that doesn't add up, and a file that can't be opened from disk, each give their own message. (No committed sample covers these; the spec decides how they are tested.)
- After a failed file, the statement that was on screen before is still on screen and still works.
- Dropping several files at once, or something that isn't a file, gives a clear message and changes nothing.
- No message repeats the file's content, and no message is in English.
- After opening statements and closing DocuSpend, nothing on the computer that DocuSpend wrote mentions those statements: not their content, names or folders.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)).
- The maintainer opens their real statements in the application and sees them, as a hand check.

## Non-goals
- Several statements at once, or combining months.
- Other banks and formats ([mvp.md](../../docs/mvp.md), out of scope).
- Remembering recent files, or reopening the last statement on start.
- Saving parsed statements between sessions (out of scope for the MVP).
- Opening a statement from Windows Explorer ("Open with", double-clicking a PDF) or from the command line.
- Saving rules and categories between sessions ([#13](https://github.com/mDubovsky/DocuSpend/issues/13)).
- How the screens are arranged around the statement, beyond the start screen and the place for messages.
- Recovering part of a damaged statement ([#2](../002-statement-reader/intent.md) rules this out).

## Decisions
- **The messages say what is wrong and what to do**, never why the code failed. The reader's own reasons stay for developers and for the statement check tool.
- **A statement that can't be read changes nothing.** The previous statement stays on screen, as the issue asks.
- **One example statement still ships.** The synthetic sample `01-typical-month` is a "try it on an example" choice on the start screen, clearly labelled as invented data; the sample list goes. A new user can see what DocuSpend does before trusting it with a real statement, and the screenshots and window tests keep a statement to show without a file dialog.
- **Windows may remember the last folder of its open window, and that is accepted.** Windows keeps it in its own settings for every program that uses the standard window; preventing it would mean giving up the familiar window. DocuSpend itself stores nothing, and [PRIVACY.md](../../PRIVACY.md) says so.
- **A file that isn't a PDF (a spreadsheet, a photo) gets the same message as any other non-PDF**, judged by its content, not its name or extension. One rule is simpler to trust than two, and a renamed file is judged by what it is.

## Open questions
None.
