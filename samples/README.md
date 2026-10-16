# Samples

- `public/`: synthetic sample statements used by unit tests and development. Committed. They contain no real personal data: every name, account number, merchant, reference and amount is invented. They copy VÚB's statement **layout** but carry an invented bank's **identity** ("Vzorová banka, a.s."), never VÚB's ([decision 0002](../docs/decisions/0002-fictional-bank-identity-in-samples.md)). Every PDF is marked "SYNTHETIC TEST DATA – NOT A REAL STATEMENT" in the margin, the footer and its metadata.
- `private/`: real statements for your own local experiments. Gitignored. Never commit them, and never use them in tests.

## What's in `public/`

Each sample is a `.pdf` with a companion `.expected.json` record of what a correct reader must get out of it.

| Sample | What it is |
|---|---|
| `01-typical-month` | A normal month in the VÚB layout: 25 transactions on 4 pages, every kind seen on a real statement |
| `02-busy-month` | 70 transactions on 8 pages, including pages that end early |
| `03-awkward-month` | Slovak diacritics, full-width and wrapped text, a bare transfer, differing dates, both ends of the month |
| `04-empty-month` | No transactions |
| `05-single-transaction-month` | One transaction |
| `06-other-bank-statement` | Another (invented) bank's statement: must be rejected |
| `07-not-a-statement` | An invoice: must be rejected |
| `08-corrupted-statement` | A VÚB statement cut off partway: must be rejected |
| `09-empty-file` | Zero bytes: must be rejected |

Records for valid samples (`"expectation": "parse"`) list the statement header, totals and every transaction. Fields a statement doesn't show are left out, not empty. Records for invalid samples say `"expectation": "reject"` and why.

The layout is based on one real statement; see [work/001-synthetic-statements/layout.md](../work/001-synthetic-statements/layout.md), including the assumptions about things it didn't show.

## Regenerating

The samples come from `tools/DocuSpend.SampleGenerator`. Don't edit them by hand: change the scenarios and regenerate.

```
dotnet run --project tools/DocuSpend.SampleGenerator -- generate
```

Tests fail if the committed files differ from what the generator produces, so every change to a sample is deliberate.

## Checking for leaks (maintainer, locally)

Before committing changed samples, check them against your real statements:

```
dotnet run --project tools/DocuSpend.SampleGenerator -- leak-check samples/private/<statement>.pdf
```

It prints counts per file, never values, and exits with an error if any amount, reference or account number from a real statement appears in a sample. "Weak" matches are shared words such as first names or towns; review them if the count looks unexpected. This can't run in CI because it needs the real file.
