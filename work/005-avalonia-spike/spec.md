# Spec: Try Avalonia for the MVP screens

Intent: [intent.md](intent.md)

## Behavior

The spike is a small desktop application, and a written verdict about it. The application is deliberately rough: one window, no visual design, just enough of each MVP screen to find out how the toolkit behaves. Everything it shows comes from the real statement reader and categorizer; the application adds no reading or categorizing logic of its own.

### Starting and choosing a sample
- The application starts on Windows and shows a statement straight away: `01-typical-month` from `samples/public/`.
- A simple list lets the user switch to another valid sample (`01` to `05`). The samples travel with the build, so it works on a machine that has no copy of the repository.
- The application never opens anything else: no file picker, no reading of `samples/private/`, no other folder.

### The statement view
One window shows, for the chosen statement:

- **A header:** the period ("1. 9. 2026 – 30. 9. 2026"), the opening and closing balance, and the month's total spending.
- **The spending pie:** one slice per spending category with spending above zero, including Nezaradené výdavky. Each slice is labeled with the category's name, and its amount and share are shown on hover or in a legend. Income and the excluded categories aren't in the pie.
- **A list of categories:** every category that has transactions this month, including income, the excluded ones and both Uncategorized buckets, each with its total. This is how a category that isn't in the pie (Nezaradené príjmy, for example) is inspected.
- **The balance line:** the account balance through the month. It starts at the opening balance on the first day of the period, changes on each booking date by that day's transactions, and ends at the closing balance on the last day. Hovering a point shows the date and the balance.
- **The transaction list:** selecting a category, either by clicking its slice or by choosing it in the category list, lists exactly that category's transactions, in booking-date order. Each row shows the booking date, the description or counterparty, the sub-category and the amount. Money coming back into a spending category (a refund, a reversed fee) appears in the list as a positive amount.

### Adding a rule
- A simple form adds one user rule. The user chooses:
  - a **kind of condition**, and its value. Three kinds are in the form, chosen because together they cover free text, a typed number and a fixed choice:
    - text in the merchant or description, with "obsahuje" (contains) or "rovná sa" (equals);
    - an amount range (from, to, or both), typed the Slovak way ("12,50");
    - a transaction type, from a list of Slovak names;
  - a **target**: a category, then one of its sub-categories.
- Saving the rule categorizes the statement again at once. The pie, the category list, the balance header and the transaction list all show the new result without restarting. The new rule wins over every existing rule, as item 003 defined.
- The rules added so far are shown in a plain list (condition and target). They can't be edited, reordered or deleted in the spike.
- If the input is incomplete or invalid (empty text, an amount that isn't a number, "from" above "to", no sub-category chosen), the form says what is wrong, in Slovak, and nothing is saved.

### Rules survive a restart
- Every added rule is saved straight away to a file in the user's own application-data folder on Windows, not next to the program and not in the repository.
- On start, the application loads that file and applies its rules to whichever sample is shown. If there is no file yet, there are simply no user rules.
- The file's format is throwaway (see the intent's decisions). It must only be readable by the same build that wrote it.

### Slovak
- Every visible text is in Slovak: window title, labels, buttons, messages, the names of transaction types, and the built-in category names the categorizer already provides.
- Amounts, dates and typed numbers are formatted and read the Slovak way ("1 349,87 €", "24. 10. 2026", "12,50"), whatever the language and region settings of Windows are, because the interface is always Slovak ([decision 0003](../../docs/decisions/0003-slovak-interface-english-code.md)).
- Formatting comes from the Slovak culture setting in one place, and visible texts are kept in one place, never assembled by hand in individual screens ([decision 0005](../../docs/decisions/0005-avalonia-for-the-user-interface.md), consequences).
- Diacritics (á, ä, č, ď, é, í, ĺ, ľ, ň, ó, ô, ŕ, š, ť, ú, ý, ž) display correctly everywhere, including inside the charts.

### The Windows build
- CI builds the application for Windows on every change to the branch and keeps the result as a downloadable artifact of the run. It isn't a release, and it has no installer and no code signing.
- The build runs on a Windows machine that has no .NET or development tools installed: unpack, start, use.

### Privacy
- The application ships, so it is held to every privacy check like the rest of the code. The networking ban covers its own code, and the dependency check covers everything it brings.
- What the checks flag in Avalonia, the chart library and their drawing engine (networking references in their assemblies, native files) is not suppressed or worked around. Each finding gets an entry in `privacy/allowed-dependencies.json` or `privacy/native-libraries.json` with a reason, proposed by the developer and accepted only by the maintainer. Version ranges are kept narrow, as for every entry.
- If a finding can't honestly be explained as never reached (for example, a component that does phone home), that is a no-go finding, not an entry.
- With the network switched off, the application starts and does everything above. While it runs, it opens no network connections. Both are checked by hand on the maintainer's machine and recorded in the findings.

### How the screens can be tested
At least one automated test, running with `dotnet test` and in CI, shows how screen logic will be tested in the real interface. It covers, without drawing a window:
- that selecting a category gives exactly its transactions;
- that adding a rule changes the categories of the transactions it matches;
- that an amount and a date come out in Slovak formatting.

The findings say whether interface-level testing (drawing the window in a test) is practical with this toolkit.

### The findings
`work/005-avalonia-spike/findings.md` records the verdict, **go** or **no-go**, and the notes the intent asks for:
- what worked and what was awkward, per part of the behavior above;
- where the toolkit fought back: disproportionate effort, workarounds, missing documentation;
- the privacy picture: every assembly and native file the toolkit adds, what networking each references, and the review outcome;
- the Windows build: size, how long it takes to start, anything needed on the target machine;
- what the project structure for the real screens should be: where interface code lives, how it reaches the reader and categorizer, where texts and formatting live, how it is tested;
- whether the spike's code becomes the start of the real interface or is thrown away;
- what the spike learned for the saving-rules issue (the user's folder, reading and writing it from the CI build).

[Decision 0005](../../docs/decisions/0005-avalonia-for-the-user-interface.md) is updated in the same change: status confirmed on a go, reopened on a no-go, with a link to the findings. LiveCharts2's status as "the candidate" there is settled by the findings too.

## Acceptance criteria

Most of these are checked by hand, because the spike is about how the toolkit behaves for a person using it. Each hand check is recorded in the findings as done or not done, with a note. Criteria marked *(automated)* are tests.

- [ ] Given the application starts, then it shows `01-typical-month` with the header, pie, category list and balance line, and every value matches what the categorizer reports for that sample.
- [ ] Given each valid sample `01` to `05`, when the user switches to it, then the view shows that sample.
- [ ] When the user clicks a pie slice, or chooses a category in the category list, then the transaction list shows exactly that category's transactions, in booking-date order. *(automated, for the logic behind it)*
- [ ] The pie has exactly the spending categories with spending above zero, and the balance line starts at the opening balance and ends at the closing balance.
- [ ] Given a text rule, an amount rule and a type rule, each added through the form, then the transactions they match move to the chosen sub-category and the views update without restarting. *(automated, for the logic behind it)*
- [ ] Given invalid input in the form, then it shows a Slovak message and saves nothing.
- [ ] Given a rule added, when the application is closed and started again, then the rule is listed and still applied. The rules file is in the user's application-data folder.
- [ ] Given Windows set to English (United States), then all text is still Slovak and amounts, dates and typed numbers still use Slovak formatting. *(automated, for formatting)*
- [ ] Diacritics display correctly in the window and inside both charts.
- [ ] CI produces a Windows build artifact on the branch, and it runs on a Windows machine without .NET installed.
- [ ] `dotnet build` and `dotnet test` pass, including every privacy check, with the application in place. Every new entry in `privacy/allowed-dependencies.json` or `privacy/native-libraries.json` has a reason and was accepted by the maintainer.
- [ ] With the network switched off, the application works as above, and while it runs it opens no network connection (hand check).
- [ ] `findings.md` exists with a go or no-go and every note listed above, and decision 0005 reflects the verdict.

## Edge cases and errors

- **An empty month (`04`).** The pie is replaced by a Slovak message that there was no spending; the balance line is flat at the opening balance; the category list is empty.
- **A single transaction (`05`).** The pie has one full slice, and the balance line has one step.
- **Many categories (`02`).** Small slices stay readable, or the findings say how badly they don't.
- **More came back than went out.** A spending category whose spending is zero or negative isn't a slice (a pie can't show it), but it is in the category list with its total, and its transactions can be listed.
- **Long texts.** Long descriptions and category names are cut off or wrapped, never overlapping other content.
- **A rule that matches nothing.** It is saved and listed. Nothing changes in the views, and the form says no transaction in this statement matched.
- **A rule that matches transactions in another sample.** Switching samples applies all saved rules to the new sample.
- **The rules file is missing.** The application starts without user rules, with no message.
- **The rules file is unreadable** (damaged, or written by another version). The application starts without user rules and says so in Slovak. It doesn't overwrite the file in that session: adding a rule is refused with a message, so nothing the user had is lost silently.
- **Windows is not in Slovak.** Covered above: the interface and formatting stay Slovak.
- **Display scaling** (125 %, 150 %). Text and charts stay sharp and laid out; the findings note any problem.
- **The window is made small.** Content can scroll or shrink, but the application doesn't crash.

## Out of scope

- Finished screens, visual design, a polished layout, or an application icon.
- Opening a statement the user chooses, and showing errors for unreadable statements.
- Editing, reordering and deleting rules; the other three kinds of condition (counterparty name, counterparty account, variable symbol); creating categories or sub-categories.
- Showing which rule placed a transaction.
- macOS and Linux builds; installers; code signing; publishing a release.
- A real rules file format, its versioning, and reading older files (the saving-rules issue).
- Any networking.
- Comparing chart libraries again.

## Decisions

- **The interface uses Slovak formatting regardless of Windows settings.** The interface is always Slovak, so the formatting follows it; a user on English Windows still sees "1 349,87 €".
- **The balance line follows booking dates.** That's the date the statement's balance changes on, so the line ends exactly at the printed closing balance.
- **Three of the six kinds of condition are in the form.** Text, amount and type cover the three shapes of input the real rule editor needs (free text with a mode, a typed number, a fixed choice). The other three are more of the same and add nothing to the verdict.
- **The samples travel with the build**, so the Windows build can be tried on a clean machine. They are synthetic and already public, so this reveals nothing.

## Open questions

None.
