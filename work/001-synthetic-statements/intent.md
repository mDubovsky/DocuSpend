---
status: done
issue: 1
---

# Intent: Synthetic VÚB statements for testing

Issue: [#1](https://github.com/mDubovsky/DocuSpend/issues/1)

## Problem
DocuSpend must read VÚB monthly PDF statements, and its behavior must be covered by automated tests. Real statements contain personal financial data and can't be committed, so contributors and agents have nothing safe and realistic to test against. Everything downstream (reading statements, categorizing transactions, charts) is blocked on this.

## Desired outcome
The repository contains a committed set of fake VÚB-style monthly statement PDFs that look and behave like real ones but hold only invented data. Each comes with a record of exactly what it contains (transactions, balances, dates), so tests can check results against known correct answers instead of eyeballing them.

The set covers:
- a normal month with the transaction kinds seen on a real statement: card payments (including wallet payments), ordinary transfers, instant transfers, incoming and outgoing money, a monthly account fee and its reversal, spanning multiple pages, plus the non-transaction sections a real statement has (fee breakdown, notices)
- an empty or near-empty month
- files that are not valid VÚB statements (another bank's statement, a PDF that isn't a statement, a corrupted or empty file), so the MVP's "clear error" behavior can be tested

## Success criteria
- Someone who has never seen a real statement can test statement-reading behavior using only the committed samples.
- For every valid sample, the record of its contents is complete and unambiguous enough to serve as the expected result of a test.
- A sample statement is recognizable as a VÚB statement in structure, so that a reader that works on it is likely to work on a real one. This is checked against the one real statement we have; layouts it doesn't show are assumptions.
- No value from any real statement (names, account numbers, merchants, amounts, dates, references) appears in the samples or their records.
- Every sample is visibly marked as synthetic, so it can't be passed off as a real bank document.
- The samples can be reproduced or extended later without needing a real statement.

## Non-goals
- The statement reader (parser) itself.
- Other banks or statement formats.
- Transaction kinds the one real statement doesn't show (foreign-currency payments, ATM withdrawals, standing orders, direct debits, fees other than the monthly account fee). They can be added once verified against real examples.
- Visually pixel-perfect reproduction of VÚB's design.

## Open questions
- How faithful must the layout be? What matters is the structure a reader depends on (page structure, column positions, multi-line transactions, details printed under a row), not branding.
- How many normal-month samples are enough to avoid tests that overfit to one statement?
- Are the samples fixed files, or can they also be regenerated on demand? The success criteria lean toward reproducible, but the choice is for the spec and plan.
