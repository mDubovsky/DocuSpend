# Plan: Synthetic VÚB statements for testing

Spec: [spec.md](spec.md)

## Approach

**Language and project-wide decision: .NET (C#).** It is the first code in the repo, so this choice also becomes the project's language. Chosen because .NET runtimes are already on the maintainer's machine, it has solid MIT/Apache-licensed PDF libraries, and a good unit-testing story. Recorded in `docs/decisions/0001-use-dotnet.md`.

**A small console tool, `tools/DocuSpend.SampleGenerator`, is the single source of the samples.**

- **One description per sample, in code.** Each sample is defined as a typed scenario (statement header, list of transactions). Samples 1, 3, 4, 5 are written out by hand. Sample 2 (70 transactions) is produced from a fixed random seed with fixed rules, so it is reproducible without 70 hand-written rows.
- **Two outputs per scenario:** the PDF, and the companion record `<name>.expected.json` (field names follow `docs/glossary.md`; absent fields are omitted, never empty or zero). The record is serialized from the same scenario that draws the PDF, so they can't disagree by accident.
- **PDF writing:** PDFsharp draws text. It is pinned to **6.2.4, the last MIT-licensed release**: the next release, 6.5.0, is AGPL-3.0-only and enforces a runtime license check. PDFsharp at explicit coordinates, which gives exact control over the layout features the spec requires (two dates per row, merchant on a separate line, details block under transfers, repeated page headers, page "n/N", keep-together page breaks). Coordinates and fonts come from [layout.md](layout.md). The real statement uses a proprietary monospaced font on a 5 pt character grid; we embed an open-licensed monospaced font with Slovak diacritics (e.g. Liberation Mono or DejaVu Sans Mono) and place text on the same grid. How the glyph width is matched to the grid is settled in step 5.
- **Synthetic marking:** the margin code position carries "SYNTHETIC TEST DATA – NOT A REAL STATEMENT", and the PDF's `Title`/`Subject` say the same. PDF metadata (creation date, producer) is fixed, and after saving, the random parts PDFsharp writes (font subset tags, document ID, XMP UUIDs) are replaced by seed-derived values of the same length, so regenerating gives byte-identical files and clean git diffs.
- **PDF reading in tests:** PdfPig (Apache-2.0) extracts text with positions, so tests check the samples the way a future parser will see them.
- **Invalid files** (samples 6 to 9) come from the same tool: an invented other-bank layout, a one-page invoice, sample 1 cut off partway, and a zero-byte file.
- **Leak check:** the tool has a subcommand `leak-check <path-to-real-pdf>`. It extracts names, account numbers, references, merchants and amounts from the real file in memory, searches every sample and record for them, and prints only counts and file names, never the values. It never writes the extracted values anywhere. It is run by the maintainer locally and is not part of CI.

**Alternatives considered**
- *Editing the real PDF:* rejected, it risks leaving real data in metadata or hidden objects.
- *QuestPDF:* nice layout engine, but its Community license is not OSI-approved and needs checking against GPL-3 before we depend on it; coordinate drawing is enough here.
- *Python (reportlab):* good libraries, but not installed, and a second language would be needed for the app.
- *Hand-edited static files without a generator:* fails the spec's reproducibility requirement.

**Layout**
```
DocuSpend.slnx
tools/DocuSpend.SampleGenerator/       # generator + leak-check
tests/DocuSpend.SampleGenerator.Tests/ # xUnit tests
samples/public/                        # generated output, committed
```
Sample files are named for what they are, e.g. `01-typical-month.pdf` and `01-typical-month.expected.json`.

## Prerequisites

- **A .NET SDK.** This machine has only the .NET 6 and 9 runtimes, not an SDK, so nothing can be built yet. Install the latest LTS SDK (.NET 10 at time of writing; verify) before implementation. The project targets that version.
- Current stable versions of PDFsharp, PdfPig and xUnit are picked and pinned when the projects are created.

## Steps

1. Install the .NET SDK. Create the solution, the tool project and the test project. Extend `.gitignore` with .NET build output (`bin/`, `obj/`).
2. Write `docs/decisions/0001-use-dotnet.md`. Fill in the "Stack, build and test" section of `AGENTS.md` with the build and test commands.
3. **Study the real layout** (needs the maintainer's explicit go-ahead, since `samples/private/` is involved): measure column positions, fonts and sizes, spacing and header content with `pdftotext -bbox`. Record only layout numbers and structure, never values, in a short layout note kept in this folder.
4. Define the scenario model (statement, transaction) and the JSON record writer. Test-first: reconciliation tests (opening + deposits + withdrawals − fees = closing; transactions sum to totals).
5. Build the layout engine on PDFsharp: header, account block, balance summary, transaction rows with the two-dates layout, details block, fee rows, repeated page headers, page numbers, keep-together page breaks, fee breakdown, final notices page, synthetic marking.
6. Write the five valid scenarios (samples 1 to 5) and the seeded generation for sample 2.
7. Generate samples 6 to 9.
8. Add the `generate` command (writes into `samples/public/`) and the `leak-check` command.
9. Generate and commit the samples. Run `leak-check` locally against the real statement and confirm zero matches.

## Risks and unknowns

- **Fidelity rests on one real statement.** Layout features it doesn't show are not reproduced (as the spec states). If a real statement later reveals a new case, a new sample is added in a new item.
- **Font encoding of the real PDF:** resolved in step 3. PdfPig extracts the real statement's text cleanly with diacritics; the damaged characters came from the earlier tool and terminal.
- **Font substitute:** the real font is proprietary. A different monospaced font changes glyph shapes and possibly reported font size; a reader should depend on positions and text, not on font identity.
- **Layout drift in PDFsharp across versions** could change bytes. Mitigated by pinning versions and by comparing extracted text and records, not only bytes.
- **Page counts** depend on the reproduced row heights. Counts (4 pages for 25 transactions, at least 8 for 70) are tuned in step 6 and enforced by tests.
- **Brand names in sample data** are limited to the merchants the default rules will need; any others are fictional.

## Verification

Each acceptance criterion in the spec maps to automated tests unless noted.

| Spec criterion | How it is checked |
|---|---|
| All nine samples exist, clearly named | Test lists `samples/public/` against the expected file names |
| Records of samples 1 to 5 contain all fields | Test deserializes each record and checks required fields and the "absent, not empty" rule |
| Balances, totals, transactions reconcile | Reconciliation tests on every valid record |
| Sample 1: 25 transactions, 4 pages; sample 2: 70, ≥ 8 pages | Tests count record entries and PDF pages (PdfPig) |
| No transaction split across pages; sample 2 has an early page end | Tests check each transaction's lines sit on one page, and that some page in sample 2 ends above the table bottom by more than one row |
| Fee row, reversal and fee breakdown | Record and PdfPig tests on sample 1 and the other valid samples |
| Synthetic marking | PdfPig tests read the margin text and document information of every non-broken sample PDF |
| Sample 3 awkward cases | Tests check the record flags and PdfPig text for diacritics, wrapped text, missing optional fields, differing dates, first and last day |
| Sample 4 empty, sample 5 one transaction | Record and PDF tests |
| Structural features match the real statement | PdfPig-based tests check headers, "n/N" page numbers, two dates per row, number and date formats, details block |
| Samples 6 to 9 rejected as not VÚB statements | Tests assert no VÚB identity markers, or that reading fails, and that each record says "reject" |
| No real value in any sample | Manual: maintainer runs `leak-check` against the real statement; zero matches required. Not in CI |
| Regeneration reproduces identical records | Test generates into a temp folder twice and compares records and extracted text, and compares against the committed files so any drift fails |
| No sample includes kinds outside the intent | Test checks each record's transaction types against an allow-list |

Run everything with `dotnet test` from the repo root.
