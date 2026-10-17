---
status: draft
issue: 2
---

# Intent: Read VÚB statement PDFs into transactions

Issue: [#2](https://github.com/mDubovsky/DocuSpend/issues/2)

## Problem
A VÚB monthly statement is a PDF: text positioned on pages, not data. DocuSpend can't categorize spending, draw charts or apply rules until each statement has been turned into structured information: who the statement is for, the period and balances, and every transaction with its details. Nothing in the project can currently do this.

## Desired outcome
Given a monthly statement PDF in the VÚB layout, DocuSpend knows its statement header (account holder, account number, period, opening and closing balance, totals) and every transaction (dates, type, amount, counterparty, description, symbols, references, fees), exactly as the record files in `samples/public/` describe them.

A statement reads completely or not at all:
- If any part can't be read, or the result doesn't reconcile with the statement's own printed totals (opening balance + deposits + withdrawals − fees = closing balance), the file is rejected with a clear reason. There are no partial results.
- Files that aren't readable VÚB statements are rejected the same way, with a clear reason: another bank's statement, a PDF that isn't a statement, a corrupted file, an empty file.

The format is recognized by its structure (title, table headers, layout), not by the bank's name or other identity, because the committed samples deliberately carry a fictional bank's identity ([decision 0002](../../docs/decisions/0002-fictional-bank-identity-in-samples.md)).

## Success criteria
- The valid samples (`01` to `05`) read to exactly their expected records.
- The invalid samples (`06` to `09`) are rejected, each with a clear reason.
- The maintainer's real statements available when the spec is approved read and reconcile when checked locally. The check prints only pass or fail and counts, never values, and can't run in CI because it needs the real files.
- A reader that gets any amount, date or field wrong is caught by a test, not discovered in a chart.
- When the reader fails on a real file, the reason tells a developer what in the file it didn't understand, so the case can become a new sample.

## Non-goals
- Other banks or formats.
- Categorizing transactions, charts, rules.
- Saving statements between sessions.
- Statements for periods other than a month.
- Recovering from damaged files or reading part of a statement.

## Decisions
- A "clear reason" is a human-readable error message. How it is shown to a user (a dialog, a line of text, …) is a user-interface matter and out of scope here.
- A VÚB statement always carries its summary totals. A file without them can't be reconciled and is rejected like any other unreadable file.
- The samples are based on one real statement, so the reader may fail on real files at first. This item is done when it reads the committed samples and the maintainer's real statements available when the spec is approved (currently one, with a second expected). A file that fails after that becomes a new issue and a new sample.

## Open questions
None.
