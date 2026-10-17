# Plan: Read VÚB statement PDFs into transactions

Spec: [spec.md](spec.md)

## Approach

**A class library, `src/DocuSpend.Statements`, that reads a PDF's words with their positions and maps them to a statement.** This is the first project under `src/`, and it is the base the categorization and chart items will use. It has no UI and no network code.

### Public surface (small on purpose)

- **Model** (bank-neutral, so other banks can reuse it): `Statement` (header, totals, transactions), `Transaction`, `TransactionType`. Money is `decimal`, dates are `DateOnly`. Absent fields are `null`.
- **Result:** `ReadResult` holds either a `Statement` or a `Rejection`, never both. `Rejection` has a `RejectionKind` (`EmptyFile`, `NotAPdf`, `NotAVubStatement`, `Unreadable`, `DoesNotReconcile`) and a readable `Message`. Messages are English for now; showing them to a user, and any translation, belong to the UI item. Messages name the problem and its place (page, transaction number, which total) and never quote the file's content.
- **Entry point:** `IStatementReader.Read(byte[] pdf)`, implemented by `VubStatementReader`. The interface exists so another bank's reader can sit beside it later; choosing between readers is a later item and not built here.

### How reading works (internal)

1. **Quick checks.** Zero bytes gives `EmptyFile`. A PdfPig exception on opening gives `NotAPdf`.
2. **Words with positions.** PdfPig's words for each page, horizontal text only (this skips the vertical margin marking and codes), converted to top-based coordinates. Words are grouped into lines by vertical position (a tolerance of about 3 pt, as in the tests of the generator). Real and sample statements use different fonts but the same 5 pt grid, so the reader depends on positions and text, never on font names or sizes.
3. **Recognizing the layout.** A statement is recognized by its structure: the title "VÝPIS Z ÚČTU", the account-block labels, the summary labels and the table header (`Zaúčt.`, `Valuta`, `Realiz.`, `Suma`). Missing structure gives `NotAVubStatement` and says which part was not found. The bank's name, BIC and so on are never consulted.
4. **Header and summary.** Account-block values are read to the right of their labels. The summary's amounts are read by label, using the vertical offset the layout has between a label and its right-aligned value. The period comes from the fee breakdown heading `(OD dd.mm.yyyy DO dd.mm.yyyy)`.
5. **Transactions.** A line-by-line reader with a small state machine over the table of every page:
   - A transaction starts at a line whose first word is a `dd/mm` date in the date column. A second `dd/mm` line within about 14 pt directly beneath it is a card payment's value date, not a new transaction; two real transactions are always at least 26 pt apart.
   - The main line gives type text or account number, symbols and amount by column. The second line gives description, reference, fee type and fee amount. Lines starting with a details label (`Názov:`, `BIC:`, `Referencia platiteľa:`, `Účel platby:`, `Kód kategórie účelu platby:`, `Mena/suma/kurz:`) are the details block; other lines at the left edge below the labels are continuations of a wrapped value and are joined with a single space.
   - The table of a page ends where its transactions end; the fee breakdown, loyalty and notices pages are skipped by recognizing their headings, never read as transactions.
   - Column positions and tolerances are named constants in one file (`VubLayout`), taken from `layout.md`.
   - Dates take the year nearest the statement period.
6. **Reconciliation.** The reader sums the transactions' deposits, withdrawals and fees and compares them with the printed totals, then checks the closing-balance equation. A difference gives `DoesNotReconcile` naming the total.
7. **Any read failure in a recognized statement** (an amount that isn't an amount, a date that isn't a date, a missing column, missing totals) gives `Unreadable` with the page and which part, never a guess.

### Local check on real statements

A second small tool, `tools/DocuSpend.StatementCheck`, runs the reader over files and prints per file only: the name, then `OK (N pages, M transactions)` or `REJECTED (kind)`. It never prints content. It lives next to the generator's tools and is not part of CI. Running it on `samples/private/` needs the human's explicit go-ahead (AGENTS.md rule 4).

### Changes to the sample generator, for testing only

Rejection tests need statements that are inconsistent or damaged in controlled ways, which the generator can't produce yet. Two options on `VubStatementRenderer.Render`, used only by tests:

- `PrintedTotals`: print different totals from those computed from the transactions (reconciliation failures).
- `TextTransform`: change any drawn text, e.g. replace an amount with `-48,1X` (unreadable parts, removed summary).

The committed samples don't change, and their drift tests keep passing.

### Alternatives considered

- **Reading by `pdftotext -layout`:** rejected; it loses positions, is an external tool, and its output interleaves columns (seen on the real statement).
- **A generic table-extraction library:** no mature .NET one handles these multi-line rows with details blocks, and a custom reader gives clear error locations.
- **Using bold to tell a transaction's main line from its second line:** works on both statements but ties the reader to font style; the date-gap rule above doesn't.
- **Reading partial statements:** rejected by the intent.

### Layout

```
src/DocuSpend.Statements/
    Model/                  Statement, Transaction, TransactionType
    Reading/                IStatementReader, VubStatementReader, ReadResult, Rejection
    Reading/Layout/         VubLayout (named positions), word and line helpers
    Reading/Sections/       header, summary, transactions, reconciliation
tests/DocuSpend.Statements.Tests/
tools/DocuSpend.StatementCheck/
```

The new library depends on PdfPig (Apache-2.0, already used). It does not reference the generator; the tests reference both.

## Prerequisites

- Issue #1's sample set, merged. ✔
- For step 8, the maintainer's go-ahead to run the reader on the real statement.

## Steps

Test-first throughout, one commit per step group, with the human's approval before each commit.

1. Create `src/DocuSpend.Statements` and `tests/DocuSpend.Statements.Tests`; add them to `DocuSpend.slnx`; update the layout note in `AGENTS.md` (`src/` now exists).
2. Model, `ReadResult`, `Rejection`, `IStatementReader`. Add the generator's `PrintedTotals` and `TextTransform` options with tests of their own.
3. Rejection of samples 06 to 09: empty, not a PDF, not a VÚB statement (with the missing part named). Skeleton of `VubStatementReader`.
4. Lines and the layout constants. Header, summary and period; first test: sample 04 (empty month) reads to its record.
5. Transactions, one type at a time, each checked against samples 05, 01, 03 and 02: card, wallet card, transfer with details, instant transfer, fee and reversal, wrapped purposes, missing optional fields, value-date rules, year inference (with generated January and December statements).
6. Reconciliation and `Unreadable` with locations, tested through the generator's two options.
7. Whole-sample test: samples 01 to 05 read to exactly their records, all fields, in order.
8. `tools/DocuSpend.StatementCheck`; run it on the real statement; fix the reader (and, when a layout feature turns up that the samples don't show, `layout.md` and a new sample) until it passes. The same on the second statement when it arrives.
9. Docs: glossary entries (reader, rejection), `samples/README.md`, `AGENTS.md` commands, intent status `done`.

## Risks and unknowns

- **Real statements may differ from the samples.** The samples come from one real statement plus assumptions. This is why step 8 is part of the item. If the real statement exposes a feature the generator can't produce, adding it to the generator and the samples counts as part of this item, up to the limit set in the intent.
- **The spec's assumption about fee totals** (fee rows count only in the fee total) may be wrong. The first run on the real statement will show it as a reconciliation failure.
- **Word extraction quirks.** PdfPig merges and splits words by letter spacing; on the real font, a value such as `KARTA:*6694` or a concatenated pair of numbers might not come out as in the samples. Mitigation: the layout constants are measured on the real statement's positions, and the first run on real data comes early, in step 8 after only the basics are done if needed.
- **Pictogram glyphs.** The real statement's fee-type glyphs come from a special font and extracted as plain characters on the one statement seen (`1`, `E`, `X`); other pictograms may extract as unreadable symbols.
- **Reading time.** PdfPig is fast enough for four pages; not measured on large statements.
- **Messages are English** and unvalidated by users; wording will change with the UI.

## Verification

Each acceptance criterion in the spec maps to automated tests unless noted.

| Spec criterion | How it is checked |
|---|---|
| Samples 01 to 05 read to exactly their records | Test reads each PDF, maps the result to the record shape and compares the serialized JSON with the committed record, text for text |
| Samples 06 to 09 rejected with their kinds; readable messages without file content | Test checks kind per sample; asserts the message is non-empty and contains none of the words found in the file |
| Printed total disagrees with transactions | Render a scenario with `PrintedTotals` set; assert `DoesNotReconcile` and that the message names the total |
| Closing balance doesn't follow | Same, with a printed closing balance changed |
| Missing summary totals | Render with `TextTransform` removing the summary amounts; assert `Unreadable` naming the balance summary |
| Unreadable transaction part | Render with `TextTransform` damaging an amount and a date; assert `Unreadable` with page and transaction number |
| Year inference at January and December | Generated scenarios for January 2027 (with a value date in December 2026) and December 2026 (with a booking on 31/12); round trip to their records |
| Value date rules | Samples 01 and 03 (differing and equal value dates), and a card payment |
| Identical transactions both read | Generated scenario with two identical rows |
| Wrapped purpose; "Neuvedené" as absent | Sample 03 |
| Same file read twice gives the same result | Test |
| No network access | Review: the library references only PdfPig; a test asserts that the assembly references no networking assembly beyond what the runtime loads. Manual note, not CI-proof |
| Real statements read with `OK` | Manual: the maintainer runs `StatementCheck`; it prints no content. Not in CI |

Run everything with `dotnet test` from the repo root.
