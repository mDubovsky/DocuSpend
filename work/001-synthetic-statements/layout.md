# VÚB monthly statement layout

Measured from one real statement (September 2026, 4 pages) with PdfPig. This note records **structure and coordinates only**; no value from the real statement appears here. The samples reproduce this layout but carry a fictional bank's identity wherever the real statement shows the bank's ([decision 0002](../../docs/decisions/0002-fictional-bank-identity-in-samples.md)). Coordinates are in PDF points: `x` from the left edge, `y` from the top edge to the top of the text's bounding box (so `y` varies by about ±1.5 pt with glyph height; treat values as approximate).

## Page and fonts

- A4 portrait, 595.3 × 841.9 pt, PDF 1.4. Document info has `Producer` and `CreationDate` only.
- Every page has one image (logo area) and some ruling lines (vector paths); the last page has no lines.
- Fonts (all embedded subsets):
  - **Letter Gothic 12 Pitch, Roman and Bold**: almost all text. Monospaced: at 10 pt each character advances **5.0 pt**, so columns sit on a 5 pt character grid.
  - **Arial 7 pt**: the bank identity line at the top.
  - **A pictogram font, 12 pt**: single glyphs (`1`, `2`, `E`, `X`, …) in the fee-type column.
- Sizes used: 12 pt (title, account-block labels), 11 to 12 pt bold (address block), 10 pt (table and most text), 8 pt (header box, balance summary), 7 pt (bank line), 5 pt (margin code).
- Text extracts cleanly, including Slovak diacritics.

## Every page: header

| Element | Position | Font |
|---|---|---|
| Header box labels: "Por. číslo:", "Strana:", "Zo dňa:", "Pobočka:", "Distribúcia:" | x 340, y 27 to 67, ~10 pt apart | Roman 8 |
| Header box values: statement number "M/YYYY", page "n/N", issue date "DD.MM.YYYY" | right column, x ≈ 525 to 557 | Roman 8 |
| Branch code, distribution code ("IB") | x ≈ 540 to 557, y 55 to 66 | Bold 10 / Roman 8 |
| Bank identity line (name, address, registry, ID number, web), two lines | x 45, y 60 and 68 | Arial 7 |
| Title "VÝPIS Z ÚČTU" | x 45.7, y 83 | Roman 12 |
| Account block labels "Typ:", "Názov:", "Číslo:", "BIC:", "Mena:", "Pobočka:" | x 45.4, y 99 to 169, ~14 pt apart | Roman 12 |
| Account block values (product name, holder, IBAN in groups of 4, BIC, currency, branch address) | x 113.4 | Bold 10 |
| Margin code: a 60-character document code printed vertically, one character per line | x ≈ 35, running down from y ≈ 600 | Roman 5 |

## Page 1 only

- **Address block** (holder name, street, postcode and town): x 340.2, y 177 to 201, Bold 11 to 12.
- **Balance summary**, labels Bold 8 at x 45.4, values Roman 8 right-aligned at x ≈ 216, rows ~18 pt apart starting y 188:
  "Vlastné použiteľné prostriedky v EUR k DD.MM.YYYY" (opening), "Vklady spolu", "Výbery spolu", "Kreditné úroky", "Daň", "Debetné úroky", "Poplatky spolu", then "Vlast. použit. prostr. k DD.MM.YYYY v EUR" (closing, Bold).
- Transaction table header at y ≈ 350; first transaction at y ≈ 376.

## Pages 2+

- Header and account block as above; no address block, no summary.
- Table header at y ≈ 187; first transaction at y ≈ 213.

## Transaction table

Two header lines (Bold for the first column word of each, Roman otherwise):

| Column | Header line 1 | Header line 2 | x |
|---|---|---|---|
| Dates | "Zaúčt." / "Valuta" | "Realiz." | 45.4 / 80.8 |
| Account / description | "Číslo účtu" | "Popis transakcie" | 121.9 |
| Symbols / reference | "VS" | "Referencia" | 334.5 |
| Constant symbol | "KS" | | 391.2 |
| Specific symbol | "ŠS" | | 419.5 |
| Fee type | | "Typ poplatku" | 447.9 |
| Fee | | "Poplatok" | 526.9 |
| Amount | "Suma" | | 546.9, values right-aligned to x ≈ 566 |

### Rows

Each transaction is a group of lines:

1. **Main line (Bold 10):** booking date `DD/MM` at x 45.4; at x 121.9 either the counterparty IBAN (groups of 4) or a type text (`PLATBA KARTOU`, `PLATBA KARTOU A-PAY`) followed by `KARTA:*NNNN` at x 221.9; VS at 334.5, KS at 391.2, ŠS at 419.5 when present; amount with sign right-aligned at 566 (e.g. `-48,15`, `2.300,00`). If the value date differs from the booking date, it is printed on this line at x 80.8 (Roman).
2. **Second line (Roman 10), ~11 pt below:** value date at x 45.6 for card payments; description at x 121.4 (merchant text for cards, message or type for transfers, e.g. `SEPA Okamžitá platba`); reference at x 334.5; fee-type pictogram at x 479; fee amount at x 487 when the row is a fee.
3. **Details block (transfers only), ~13 pt below, lines ~9.5 pt apart**, label Bold 10 at x 45.4, value Roman 10 right after the label:
   `Názov:` counterparty name, `BIC:`, `Referencia platiteľa:` (or "Neuvedené" when absent), `Účel platby:`, `Kód kategórie účelu platby:` (optional), `Mena/suma/kurz:` (optional, `CUR/amount/rate`).

Gap between transactions ≈ 15 to 16 pt (≈ 25 pt after a details block).

### Formats

- Amounts: dot thousands separator, comma decimals, leading `-` for debits, none for credits.
- Dates in the table: `DD/MM`. Dates elsewhere: `DD.MM.YYYY`.
- IBANs printed in groups of 4 separated by single spaces. A foreign IBAN whose length isn't a multiple of 4 ends with a shorter group (a German one, 22 characters, ends with a group of 2); found by the statement reader on the real statement and covered by sample 03.
- Card shown as `KARTA:*` plus the last 4 digits.

### Page breaks

- Bottom of the table area is about y 790.
- **No transaction is split across pages** on this statement. Note this is an observation, not a proven rule: at each page break there was too little room for even the next transaction's first two lines, so the statement never had to choose between splitting and moving a transaction. The samples assume VÚB keeps each transaction together and moves it whole to the next page.

## Assumptions (not seen on the real statement)

Used by the samples and to be checked against future real statements:

- **Keep-together page breaks**, as above.
- **Long details values wrap** onto continuation lines aligned under the value, breaking at spaces.
- **A transfer's second line repeats its purpose, cut to 41 characters** (the column holds 42; one is kept as space before the reference column) when the purpose is longer; the details block prints it in full.
- **An empty month** shows the table header with no rows, followed directly by the fee breakdown.
- **The fee breakdown moves to the next page** when it doesn't fit below the last transaction.

## After the last transaction

On the page holding the last transaction:

- **Fee breakdown** "ROZPIS POPLATKOV (OD DD.MM.YYYY DO DD.MM.YYYY)" with columns "POČET", "CENA", "SUMA V EUR": one row per fee category with count and amount, a legend explaining each fee-type pictogram, the monthly account fee, a fee reversal line, "Ostatné poplatky spolu", and "SUMÁR ZAPLATENÝCH POPLATKOV".

## Last page

- Header and account block, then a loyalty-program section ("Vyhodnotenie Odmeny za vernosť", period, conditions table with "Áno"/"Nie" values) framed by lines of asterisks.
- Deposit protection notice paragraph.
- Contact footer at the bottom (y ≈ 800 to 810).

## Seen on the real statement, beyond what the spec expected

- **Fee rows:** a monthly account fee and its reversal ("Storno-…") appear as transactions on the last day, with the amount in the "Poplatok" column, not "Suma".
- **Foreign IBAN:** one transfer with a non-Slovak IBAN.
- **Mix:** 31 transactions over 3 table pages (9, 16, 6), mostly card payments, with a 4th page for the loyalty section and notices.
