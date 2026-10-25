---
status: planned
issue: 13
---

# Intent: Keep rules and categories between sessions

Issue: [#13](https://github.com/mDubovsky/DocuSpend/issues/13)

## Problem
A user can correct how their spending is categorized by adding rules, and the categorization can hold categories and sub-categories of their own. When DocuSpend closes, all of it is gone, apart from a throwaway file the spike used to try the idea out ([#5](../005-avalonia-spike/findings.md), "For the issue on saving rules"). That file keeps only some kinds of rules, and not their priority numbers or identifiers, so even what it brings back is not exactly what the user had. A user who fixes their categories has to do it again every time they open a statement.

This is the last thing the MVP's "done when" asks for that DocuSpend can't do yet: "a user can create a rule, see the charts update, close the software, return, and find the rule still applied" ([mvp.md](../../docs/mvp.md)). Rules are the user's own work and the only thing DocuSpend keeps for itself, so losing them, or bringing them back slightly wrong, breaks trust quickly.

## Desired outcome
- **Kept as soon as it changes:** user rules and user-created categories and sub-categories are saved on the user's computer, in DocuSpend's own folder, every time they change. There is no "save" button and nothing to remember; closing DocuSpend, or DocuSpend ending unexpectedly, loses nothing that was already on screen.
- **Back exactly as it was:** when DocuSpend starts, it brings back every user rule and user category. Any statement is categorized exactly as it was before closing: each rule keeps its condition, its target, its priority number, its place among rules with the same number, and its identifier ([decision 0004](../../docs/decisions/0004-stable-category-and-rule-identifiers.md)).
- **Defaults come from DocuSpend, not from the saved data:** the built-in categories and default rules are not saved. A newer version of DocuSpend can improve them, and the user's own rules still point at the same built-in categories and still win over default rules.
- **Problems are told, never hidden:** if the saved rules can't be read, DocuSpend says so in Slovak, in terms the user understands, and does not silently start with nothing. Nothing the user had is lost or overwritten. If a change can't be saved, the user is told that it will not be there next time.
- **Later versions keep the rules:** the saved data can change shape in later versions of DocuSpend, and a user's rules saved by an earlier version still load. The saved rules are a complete, self-contained description of the user's categorization, so exporting and importing rule packs later ([mvp.md](../../docs/mvp.md), future directions) needs no change to what is saved.
- **Nothing from a statement:** what is saved contains no transactions, amounts, names or anything else read from a statement, and nothing about which statements were opened. The only exception is text or amounts the user typed into a rule themselves, as [PRIVACY.md](../../PRIVACY.md) already says.

## Success criteria
- A user adds rules of every kind of condition, and a user category with sub-categories; after closing and reopening DocuSpend, all of them are back, and every committed valid sample (`01` to `05`) is categorized exactly as before closing, transaction by transaction.
- Rules sharing a priority number, and rules whose number was changed, still decide in the same order after a restart.
- Saved data written by DocuSpend before a change of its shape still loads and gives the same categorization (checked with a committed, synthetic saved file of the earlier shape).
- Saved data that is damaged, or can't be opened, is reported to the user with a clear Slovak message; DocuSpend still starts, and the file is left as it was, or kept aside where the user can find it, never overwritten.
- A change that can't be saved (for example, the folder can't be written) is reported, and DocuSpend keeps working.
- If DocuSpend ends while saving, the previously saved rules or the new ones are there next time, never half of either.
- Starting DocuSpend for the first time, with nothing saved, shows no message and works as today.
- Nothing saved contains anything read from a statement (checked by opening statements, adding rules and inspecting everything DocuSpend wrote).
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)).
- The maintainer adds a rule while viewing a real statement, restarts DocuSpend, reopens the statement and sees the rule still applied, as a hand check.

## Non-goals
- The rule editor: listing, editing and deleting rules, changing their priority, and creating or renaming categories on screen ([mvp.md](../../docs/mvp.md), rule editor; [#21](https://github.com/mDubovsky/DocuSpend/issues/21) for where the rules live on screen). This item saves whatever the categorization holds, whichever screen changed it.
- Exporting, importing or sharing rules ([mvp.md](../../docs/mvp.md), future directions).
- Synchronizing rules between computers, or backups beyond keeping a damaged file.
- Saving statements or anything read from them (out of scope for the MVP).
- The light or dark look setting, which is already saved separately and stays as it is.
- Hiding the saved rules from someone who has access to the user's Windows account. They are in the user's own folder, protected like their other files.

## Decisions
- **Saved as soon as something changes**, as the issue asks. A user should never lose work by forgetting a step or by DocuSpend closing unexpectedly.
- **The spike's file format is replaced, not grown.** It doesn't store priorities or identifiers, so it can't bring back the categorization exactly ([#5 findings](../005-avalonia-spike/findings.md)).
- **Default rules and built-in categories are never saved.** They belong to the version of DocuSpend the user runs, so improvements reach the user; saved data refers to them only by their identifiers (decision 0004).
- **A file that can't be read is never silently replaced**, as the issue asks and the spike showed works well.
- **A damaged file is kept aside, and saving goes on.** DocuSpend keeps the damaged file under another name in the same folder, tells the user where, and starts with no user rules, saving normally from then on. Refusing to save instead would stop DocuSpend from ever saving again until the user deleted the file by hand.
- **A file that can't be opened right now is left untouched.** When the file is in use or can't be read for lack of permission, DocuSpend starts without user rules, saves nothing for the rest of the session and says so. The file may be fine next time.
- **A file saved by a newer DocuSpend is left untouched too**, treated like a file that can't be opened right now, and the user is told to use the newer version. Overwriting it would lose rules the older version doesn't understand.
- **The spike's file is ignored and left where it is.** Its rules are not brought over: only the maintainer used it, and its rules are easy to add again. DocuSpend doesn't delete files from the user's folder; [PRIVACY.md](../../PRIVACY.md) says how to remove it.
- **Two DocuSpend windows at once: the last one to save wins**, as long as the file is never damaged. Preventing it, or merging changes, is out of proportion for a single-user desktop tool.
- **Deleting a rule stays out of this item.** Once rules are kept, a mistaken rule stays until the rule editor exists, but adding a rule on top already corrects any mistake. The MVP's rule editor is [#24](https://github.com/mDubovsky/DocuSpend/issues/24).

## Open questions
None.
