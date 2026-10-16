# Spec: Synthetic VÚB statements for testing

Intent: [intent.md](intent.md)

## Behavior

The repository provides a set of sample files in `samples/public/`. Each valid sample is a PDF statement plus a companion record of its contents. The "user" of this item is a developer or an automated test that needs known input and a known correct answer.

### The sample set

| # | Sample | Purpose |
|---|---|---|
| 1 | Typical month | Similar in kind and size to the one real statement: exactly 25 transactions over 4 pages, a mix of all kinds seen, including the monthly account fee and its reversal |
| 2 | Busy month | Exactly 70 transactions over at least 8 pages: many page breaks, including a page that ends early because the next transaction with its details block doesn't fit |
| 3 | Awkward month | Slovak diacritics in names and merchants, long or wrapped merchant and description text, transfers with every optional field missing, a transaction with a different booking and value date, and the first and last day of the month |
| 4 | Empty month | No transactions; opening balance equals closing balance |
| 5 | Single-transaction month | Exactly one transaction |
| 6 | Other bank's statement | A statement with a different layout and bank identity |
| 7 | Non-statement PDF | A valid PDF that isn't a bank statement (e.g. an invoice) |
| 8 | Corrupted PDF | A truncated or damaged PDF |
| 9 | Empty file | A zero-byte file with a `.pdf` name |

**Why three normal months:** one is not enough because tests would overfit to it. A second, much bigger one exercises paging, which is where layouts break. A third deliberately packs in the awkward cases that don't occur often enough to show up in a "typical" month. More would add volume without adding new situations; further samples are added when a real statement reveals a new situation.

### The record of contents

Every valid sample (1 to 5) has a companion record, kept next to the PDF and readable by both people and test code. It states, exactly as it should be read from the PDF:

- **Statement:** account holder name, account number (IBAN), BIC, currency, period start and end, opening balance, closing balance, total deposits, total withdrawals, total fees.
- **Each transaction, in statement order:** booking date, value date, transaction type, signed amount, currency, and, where the PDF shows them: card (last 4 digits), merchant or description, counterparty name, counterparty account number, counterparty BIC, variable/constant/specific symbols, payer reference, payment purpose, purpose category code, reference, fee type, fee amount.
- Fields the PDF does not show are recorded as absent, not as empty or zero.

The records for invalid files (6 to 9) state only that the file must be rejected as not a readable VÚB statement.

### Consistency and realism

- For every valid sample, opening balance plus deposits plus withdrawals minus fees equals closing balance, and the transactions sum to the stated totals. The running balance used by any chart never contradicts the record.
- Number and date conventions match the real statement's: comma decimals, dot thousands separators, signed amounts, dates printed as day/month without a year, a statement header with page numbers ("n/N").
- Structural features that a reader depends on are present as on the real statement (see [layout.md](layout.md)): two dates per transaction, the merchant or description on a line apart from the date, the details block printed under transfers, repeated headers on each page, and transactions never split across pages.
- Non-transaction content is present as on the real statement, so a reader learns to skip it: the balance summary on page 1, the fee breakdown after the last transaction, and a final page with a program summary and notices.
- Layout features not seen on the real statement (foreign-currency payments, ATM, standing orders, direct debits, fees other than the monthly account fee) are absent from the set.

### Marked as synthetic

Every sample PDF is visibly marked "SYNTHETIC TEST DATA – NOT A REAL STATEMENT" in the left margin, where the real statement prints its vertical document code, in a footer line on every page, and in the PDF's document information. The marking stays outside the areas a reader parses.

### Invented data

- All names, account numbers, merchants, references, addresses and amounts are invented.
- The bank's identity is invented too: samples copy VÚB's layout but carry a fictional bank's name, legal details, BIC, bank code and product names (see [decision 0002](../../docs/decisions/0002-fictional-bank-identity-in-samples.md)). A reader recognizes the format by its structure.
- Account numbers look structurally valid but must not belong to a real person known to us, including anyone on the real statement.
- Merchant names use fictional names, not brand names. The exception is the default categorization rules, which need recognizable names to match (e.g. a streaming subscription); those may use the brand name as plain text.

### Reproducibility

The valid samples and their records can be regenerated from a single description, producing the same records each time. Adding a new sample, or changing an existing one, does not need a real statement.

## Acceptance criteria

- [ ] `samples/public/` contains all nine samples listed above, each clearly named for what it is.
- [ ] Samples 1 to 5 each have a companion record containing all the fields described above.
- [ ] For samples 1 to 5, the record's balances, totals and transactions reconcile exactly.
- [ ] Sample 1 has exactly 25 transactions across 4 pages; sample 2 has exactly 70 across at least 8 pages.
- [ ] No transaction in any sample is split across pages, and sample 2 has a page that ends early because the next transaction didn't fit.
- [ ] Sample 1 contains the monthly account fee and its reversal, and every valid sample with transactions has the fee breakdown after its last transaction.
- [ ] Every sample PDF (valid and invalid, except the zero-byte file and the corrupted PDF) carries the synthetic marking in the margin and in its document information.
- [ ] Sample 3 contains Slovak diacritics, wrapped text, an all-optional-fields-missing transfer, a differing booking and value date, and transactions on the first and last day of the period.
- [ ] Sample 4 has no transactions and equal opening and closing balances; sample 5 has exactly one.
- [ ] Extracting text from a valid sample shows the same structural features as the real statement: layout, formats, headers, page numbers.
- [ ] Samples 6 to 9 are rejected by a PDF library or a basic check as not VÚB statements, and each has a record saying so.
- [ ] A comparison against the real statement, run locally by the maintainer, finds none of its names, account numbers, references, merchants or amounts in any sample or record. (This check can't run in shared CI because it needs the real file.)
- [ ] The samples can be regenerated from their source description, producing identical records.
- [ ] No sample includes a transaction kind outside those listed in the intent.

## Edge cases and errors

- A sample's record disagrees with its PDF: the test that detects this fails, and the record is the thing to fix.
- Regeneration changes a record unexpectedly: treated as a test failure, so changes to the samples are always deliberate.

## Out of scope

- The statement reader (parser) and any tests of it beyond checking the samples themselves.
- Other banks and formats; foreign-currency payments, ATM, standing orders, direct debits, fees other than the monthly account fee.
- Pixel-accurate reproduction of VÚB's visual design.
- Verifying the samples by eye against a rendered image of the real statement; the structure is checked through extracted text.

## Open questions

None.
