---
status: draft
issue: 5
---

# Intent: Try Avalonia for the MVP screens

Issue: [#5](https://github.com/mDubovsky/DocuSpend/issues/5)

## Problem
The user interface is the largest remaining piece of the MVP and the most expensive one to change later. [Decision 0005](../../docs/decisions/0005-avalonia-for-the-user-interface.md) chose Avalonia for it, but only on paper: from documentation and a throwaway chart comparison. Nobody has yet seen how it handles what the MVP needs beyond charts: clicking into a category, lists of transactions, forms for rules, Slovak text and formatting, saving to a file on the user's computer, and a Windows build made by CI. Nor do we know what it brings into the application, which matters because DocuSpend's promise is that nothing that can see a statement can use the network ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)). Building every screen on an untried toolkit is a risky bet.

## Desired outcome
A small, deliberately rough Avalonia application exists that runs the real statement reader and categorizer on a synthetic sample statement and shows:

- **The two MVP charts:** the spending pie and the balance line for the month. Clicking a category in the pie lists that category's transactions.
- **A simple rule form:** the user adds a rule (one condition, one target sub-category), the statement is categorized again, and the charts and list update.
- **Slovak:** every visible text in Slovak, with amounts and dates formatted the Slovak way ("1 349,87 €", "24. 10. 2026"), coming from the culture setting rather than hand-built strings, and diacritics displayed correctly.
- **Rules that survive a restart:** the user's rules are saved to a file in the user's own folder and loaded the next time the application starts.
- **A Windows build made by CI:** the pipeline produces a Windows build that starts on a machine without development tools.
- **The privacy checks still pass:** the application is held to the same checks as everything else that ships. Whatever Avalonia, the chart library and their drawing engine bring that the checks flag (networking references, native files) is listed, explained and reviewed by the maintainer before it is accepted. Nothing is suppressed or worked around.

The result is a **go or no-go** for Avalonia, written down with notes:

- what worked and what was awkward;
- where the toolkit fought back: parts that took disproportionate effort, workarounds, missing documentation;
- what the toolkit adds to the privacy picture: which assemblies and native files it brings, what networking they reference, and whether that is acceptable;
- how big the Windows build is and how it starts;
- what the project structure for the real screens should be (where the interface code lives, how it reaches the reader and categorizer, how texts and formatting are kept in one place, how it is tested);
- whether the spike's code becomes the start of the real interface or is thrown away.

Decision 0005 is updated with the outcome: confirmed on a go, reopened on a no-go.

## Success criteria
- On a synthetic sample statement, the application shows both charts with the real reader's and categorizer's results, and clicking a category lists exactly that category's transactions.
- Adding a rule through the form changes the category of the transactions it matches, and the charts and list reflect it without restarting.
- All visible text is Slovak, and amounts and dates are formatted the Slovak way, by the culture setting.
- A rule added, the application closed and started again: the rule is still there and still applied. The rules file is in the user's own folder, not next to the program.
- CI produces a Windows build of the application, and it runs on a clean Windows machine.
- All privacy checks pass with the application in place. Every new entry in `privacy/allowed-dependencies.json` or `privacy/native-libraries.json` has a reason and was reviewed by the maintainer.
- The application never reads `samples/private/` and is shown working only with synthetic statements.
- A written go/no-go with the notes above exists in the item's folder, and decision 0005 reflects it.

## Non-goals
- Finished screens, visual design, or a polished layout.
- The complete rule editor (listing, editing, deleting, reordering rules), and creating categories.
- Opening a statement chosen by the user, and error messages for unreadable files, beyond what the spike needs to show its sample.
- macOS and Linux builds.
- Installers, code signing, and publishing a release.
- Any networking, including the later online rules feature.
- Comparing chart libraries again; the earlier experiment settled that chart looks don't decide this.
- Automated interface tests beyond what the spike needs to judge how the real screens can be tested. The spike may be rough, but any change it makes outside its own code (for example to the reader or categorizer) follows the usual rule: tested from the start.

## Decisions
- **On a no-go, the next candidate gets its own issue**, evaluated the same way. This item ends with the written no-go and the reopened decision 0005.
- **No time box.** The project is worked on whenever there is time, so elapsed time says nothing about the toolkit. A no-go comes from what doesn't work or is disproportionately awkward, not from how long it took.
- **The spike's rule saving is throwaway.** The rules file is the one piece of user data DocuSpend keeps over time, so its format (versioning, reading older files, room for sharing rules later) is designed in the separate issue for saving rules ([item 003](../003-categorize-transactions/intent.md) left it there). The spike saves rules in the simplest way that works; what it learns (where the user's folder is, whether reading and writing it works in the CI build) goes into the notes for that issue. The saving code stays inside the spike.

## Open questions
None.
