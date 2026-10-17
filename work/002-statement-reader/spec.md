# Spec: Read VÚB statement PDFs into transactions

Intent: [intent.md](intent.md)

## Behavior

The **reader** takes one file and gives back exactly one of two things:

- a **statement**: its header, its summary totals and all of its transactions, or
- a **rejection**: a reason, in human-readable text, that the file could not be read as a VÚB monthly statement.

It never gives both, and never a partial statement. A person can use it only through what it returns; how it is shown to them (dialog, line of text, …) is a user-interface matter and out of scope.

Reading needs only the file. It uses no network, so the statement never leaves the machine.

### What a statement contains

The same information as the record files in `samples/public/*.expected.json`, one for one:

- **Header:** account holder, account number (IBAN), BIC, currency, period start and end, opening balance, closing balance, total deposits, total withdrawals, total fees.
- **Transactions, in the order the statement prints them.** For each: booking date, value date, type, amount (signed, exact to the cent), currency and, when the statement shows them: card (last 4 digits), description, counterparty name, counterparty account number, counterparty BIC, variable, constant and specific symbols, payer reference, payment purpose, purpose category code, reference, fee type and fee amount.
- A field the statement does not show is **absent**, not empty and not zero. "Neuvedené" ("not stated") printed as a payer reference means absent.

### How the printed layout maps to those fields

These rules are what a reader has to know that the PDF doesn't say outright. The layout itself is described in [work/001-synthetic-statements/layout.md](../001-synthetic-statements/layout.md).

- **Dates.** The table prints dates as day/month without a year. A date takes the year that places it nearest the statement period, so a January statement can contain a value date in December of the previous year and a December statement a booking date of 31/12.
- **Type.** It is not a printed column; it comes from how the row reads:
  - card payment: the row says "PLATBA KARTOU" and a card
  - wallet card payment: the row says "PLATBA KARTOU A-PAY"
  - instant transfer: the description line is "SEPA Okamžitá platba"
  - transfer: any other row led by an account number
  - account fee or fee reversal: the amount is printed in the fee column instead of the amount column; a negative fee is the account fee, a positive one is its reversal
- **Fee rows.** The fee amount is both the transaction's amount and its fee amount.
- **Description.** The text printed on the transaction's second line, exactly as printed. For a transfer with a long purpose, that line shows only the beginning, so the description is shorter than the payment purpose.
- **Payment purpose.** The full purpose from the details block, with a value that wraps onto continuation lines joined by a single space.
- **Text.** Slovak diacritics are kept exactly as printed. Words are separated by single spaces.
- **Amounts.** `1.234,56` means 1234.56; a leading `-` means money out.
- **Ignored content.** Page headers repeated on each page, the table header on each page, the fee breakdown after the last transaction, the loyalty summary and notices, the margin marking, and the footer are not part of the statement and are never read as transactions.

The reader recognizes a statement by its **structure** (title, account block, summary labels, table header and column positions), not by the bank's name, BIC or other identity ([decision 0002](../../docs/decisions/0002-fictional-bank-identity-in-samples.md)), and not by fonts, since the real statement and the samples use different ones.

### A statement reads completely or not at all

After reading, the reader checks that what it read agrees with what the statement says about itself:

- the transactions' deposits, withdrawals and fees each equal the statement's printed totals, and
- opening balance + total deposits + total withdrawals − total fees equals the closing balance.

If either check fails, the statement is rejected. So are statements missing their summary totals, which a VÚB statement always prints.

### Rejections

A rejection says what was wrong in words a person can read. It names the problem and where it is, and it never repeats the file's content (names, account numbers, amounts), because messages end up in logs and bug reports.

| The file is… | The reason says… |
|---|---|
| empty (zero bytes) | the file is empty |
| not a readable PDF (damaged, cut off, not a PDF at all) | the file isn't a readable PDF and may be damaged or incomplete |
| a readable PDF that isn't a VÚB-layout statement (another bank's, an invoice, anything else) | this doesn't look like a VÚB monthly statement, and which expected part was not found |
| a VÚB-layout statement with a part that can't be understood | which part (a header field, the balance summary, the n-th transaction on page p, …) and what was expected there (an amount, a date, …) |
| a VÚB-layout statement that reads but doesn't reconcile | which total disagrees (deposits, withdrawals, fees or closing balance) |

The exact wording is decided in the plan. What this spec fixes is that each case above has its own distinguishable reason that a test can tell apart from the others.

### Checking real statements (maintainer, locally)

The maintainer can run the reader over their real statements in `samples/private/`. For each file it prints only: the file name, then either `OK` with counts (pages, transactions) or `REJECTED` with the reason's kind. It prints no names, amounts or other file content. This check cannot run in CI because it needs real files.

## Assumptions

Taken from the sample generator, which is based on one real statement. The local check on real statements confirms or corrects them, and each correction becomes a new sample:

- Fee rows are counted in the fee total only, not in deposits or withdrawals, so a fee and its reversal net to zero in the fee total.
- Transactions are never split across pages.
- Value dates are never later than booking dates.

## Acceptance criteria

- [ ] Samples `01` to `05` read to exactly their expected records: every header field, every total, and every field of every transaction, in order. Amounts are exact to the cent; absent fields are absent.
- [ ] Samples `06` and `07` are rejected as "not a VÚB-layout statement"; `08` as "not a readable PDF"; `09` as "empty". Each rejection's reason is non-empty, readable text, distinguishable from the other kinds, and contains no text taken from the file.
- [ ] A statement whose printed total disagrees with its transactions is rejected, and the reason says which total disagrees.
- [ ] A statement whose closing balance doesn't follow from its opening balance and totals is rejected, and the reason says so.
- [ ] A statement missing its summary totals is rejected, and the reason names the balance summary.
- [ ] A statement with a transaction whose amount, date or other required part can't be read is rejected, and the reason names the page and which transaction.
- [ ] A statement for January with a value date in December reads with the correct years, and so does a statement for December with a booking date of 31/12.
- [ ] Two identical transactions on the same statement are both read.
- [ ] A transfer whose purpose wraps onto several lines reads with its full purpose, and a transfer with a payer reference of "Neuvedené" reads with that field absent.
- [ ] Reading the same file twice gives identical results.
- [ ] Reading uses no network access.
- [ ] The maintainer's real statements available when this spec is approved read with `OK` in the local check, which prints no file content. (Run locally; not in CI.)

## Edge cases and errors

- A password-protected or otherwise unopenable PDF is treated as not a readable PDF.
- A statement with no transactions (empty month) reads as a statement with an empty list, not as a rejection.
- A file that reads fine but reconciles only by coincidence of two reading mistakes cancelling out can't be detected; that's why every field of the samples is compared, not just the totals.

## Out of scope

- How rejections are shown to a person, or any other user interface.
- Other banks, other formats (spreadsheets, CSV), other periods than monthly.
- Categorizing transactions, charts, rules.
- Saving statements between sessions.
- Reading part of a damaged statement.
- Writing statements, or changing the sample generator beyond what testing needs.

## Open questions

None.
