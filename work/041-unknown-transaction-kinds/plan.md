# Plan: Open statements with unfamiliar transaction kinds

Spec: [spec.md](spec.md)

## Approach

No new dependency, tool or shipped file: everything below changes existing projects only, so the privacy lists stay as they are. Verified on 2 November 2026 by reading the code on `main` at `5f16902` (after #25 was merged).

Choices to know about first:

- **The saved rules file goes to version 2, always.** Every save from now on writes `"version": 2`, whether or not it holds a cash-withdrawal rule. The previous release already reads any version above 1 as "saved by a newer DocuSpend" and leaves the file alone (`SavedRules.Read`, checked in the code), so that is all it takes. The cost: once this version has saved, the previous release shows the newer-version message even for a file with no cash-withdrawal rule. The project has no published release yet, so that cost is theoretical.
- **One new valid sample, `10-new-types-month`,** is committed alongside 01 to 05, with a G-PAY payment, cash withdrawals and two rows of invented unknown types. The invalid samples keep their numbers, 06 to 09.

### Statements (`DocuSpend.Statements`)

- `TransactionType` gets `CashWithdrawal` and `Unknown`, appended after the existing values.
- `Transaction` gets `PrintedType` (`string?`), set only for `Unknown`.
- `TransactionSection.ToTransaction` decides the type in this order:
  1. "PLATBA KARTOU", "PLATBA KARTOU A-PAY", "PLATBA KARTOU G-PAY" (the last two are wallet), "VÝBER HOTOVOSTI" (cash withdrawal): read as card rows, card required;
  2. an account number: transfer or instant transfer, as today;
  3. no type text, no amount, a fee: fee row, as today;
  4. type text and an amount in the amount column: `Unknown`, with the type text as `PrintedType`;
  5. anything else: rejected. The message for a row with type text but no amount is "the amount is missing or not an amount", so "its kind isn't one this reader knows" goes away.
- **Unknown rows.** For `Unknown`, `typeText` is the text between the account column and the VS column, minus a word at the card column that is `KARTA:*` plus four digits; that word, when present, gives `Card`. The value date is read from the second line's date column when a date is printed there, otherwise from the value-date column, otherwise it is the booking date. Symbols, reference and details are read as for transfers. A value printed in the second line's date column that isn't a date rejects the row, like a broken value date elsewhere.
- Reconciliation is unchanged; unknown types count like any other transaction.

### Sample generator (`tools/DocuSpend.SampleGenerator`)

- Its `TransactionType` gets the same two values, and `Transaction` gets `PrintedType` and `Wallet` (`"A-PAY"` or `"G-PAY"`; the expected record doesn't carry it, so A-PAY stays the default and the existing samples render byte-identically).
- `Tx` gets `Card(..., wallet: WalletKind.GPay)`, `CashWithdrawal(booked, daysEarlier, amount, atm, card)` and `Unknown(booked, amount, printedType, description, card: null | "nnnn", vs: …)`.
- The renderer prints "PLATBA KARTOU G-PAY" and "VÝBER HOTOVOSTI" as card rows, and an unknown row with its printed type in the type column, laid out as a card row when it has a card and as a transfer row without an account number when it doesn't.
- The expected record gets an optional `printedType`. It is written only when set, so the existing records don't change.
- `TemplateVocabulary`'s probe gets a G-PAY row and a cash withdrawal, so their labels count as layout text and not as content in the leak check.
- **Sample 10** (`10-new-types-month`, fictional holder and bank like the others, a month no other sample uses): some A-PAY and G-PAY payments, a card payment, two cash withdrawals (one at an ATM whose name contains "TESCO", which a default rule would otherwise place under groceries), an unknown type laid out like a card payment ("PLATBA HODINKAMI", description "PEKAREN U MOSTA …"), an unknown type laid out like a transfer without an account ("ZRÁŽKA ZO ZÁLOHY", with VS and a details block), a salary transfer and a fee. All values are invented.

### Categorization (`DocuSpend.Categorization`)

- `TypeCondition.Validate` refuses `Unknown` with a new `RefusalKind.UnknownType`. `Matches` never matches `Unknown`, because no valid condition holds it.
- `TextCondition.Matches` also tests `PrintedType`. It is null for every other type, so known types are unaffected.
- `Categorizer` skips default rules for a cash withdrawal: `rules.FirstOrDefault(r => !(r.Source == RuleSource.Default && t.Type == TransactionType.CashWithdrawal) && r.Condition.Matches(t))`. This is one line in the categorizer rather than a condition on every default rule, which keeps the default rules table and its tests as they are.
- **Saving.** `Words.Types` gets `(CashWithdrawal, "cashWithdrawal")`. `SavedRules.CurrentVersion` becomes 2, and version 2's shape is version 1's. `Read` accepts versions 1 and 2 through the same reader. In `Load`, a type word this version doesn't know now gives `Newer` instead of `Damaged`. `Words.Type(Unknown)` is never called, since `Validate` keeps `Unknown` out of saved rules. `rules-v1.json` is untouched, and a `rules-v2.json` is committed next to it, with a cash-withdrawal rule, as the fixed example of the new shape.

### Presentation (`DocuSpend.Presentation`)

- `Texts.TypeName`: `CashWithdrawal` → "Výber hotovosti", `Unknown` → "Neznámy typ" (only to keep the switch total; never offered). `Texts.UnknownTypes(int)` → "1 transakcia / 3 transakcie / 5 transakcií neznámeho typu", following the plural rule `RuleMatched` already uses. `Texts.UnknownTypesHelp` holds the tooltip from the spec. `RefusalKind.UnknownType` gets a Slovak reason too, since the texts test requires one for every kind.
- `RuleFormViewModel` offers every type except `Unknown`.
- `StatementViewModel` gets `UnknownTypes` (`string?`, null when the count is 0). `TransactionRow.Description` becomes "<printed type> · <description>" for `Unknown` and "Výber hotovosti · <description>" for a cash withdrawal, in one helper with the rest unchanged.

### Desktop (`DocuSpend.Desktop`)

- The key figures become a two-column `Grid`: the existing `WrapPanel` on the left, and on the right a `StackPanel` (`x:Name="UnknownTypes"`, bottom-aligned, visible only when `Statement.UnknownTypes` is set) holding a muted `TextBlock` and the same `help` button the rules card uses, with `ToolTip.Tip` bound to the help text. Only look resources are used, so the look test still passes.

### Tools

- **StatementCheck:** `OK (N pages, M transactions, K of unknown type)`, with the count always printed. The `--verify` mode covers sample 10 like the others.
- **Screenshots:** a new screen, `22-unknown-types`, opens sample 10 and selects Nezaradené výdavky, so the note, a cash withdrawal and an unknown-type row show together.

### Alternatives considered

- **Write version 2 only when a cash-withdrawal rule is present.** It is kinder to someone switching back to an older version, but it means two shapes written, chosen by content. There is no release to switch back to, so it isn't worth it.
- **Let the saved file carry unknown type words through untouched.** That would need a rule that can't be applied but must be kept, and a newer version is the more honest answer.
- **A separate `PrintedType` for every transaction**, with text rules searching it everywhere. Rejected by the spec: it changes what existing text rules catch.
- **A "not for cash" check inside every default text rule.** It would touch all the rows of the default rules table and their tests, for the same effect as one line in the categorizer.
- **Guessing card payments from a card number.** Ruled out by the intent.

### Layout

No new projects or folders. New files:

```
samples/public/10-new-types-month.pdf
samples/public/10-new-types-month.expected.json
tests/DocuSpend.Categorization.Tests/Saving/rules-v2.json
tests/DocuSpend.Categorization.Tests/Saving/SecondShapeTests.cs
work/041-unknown-transaction-kinds/screenshots/22-unknown-types-{light,dark}.png   (after review)
```

## Prerequisites

- #25 is merged (done: this branch sits on `5f16902`).
- The maintainer reviews the screenshots (step 7) and does the hand check on the October statement (step 8). Running StatementCheck on `samples/private/` needs their go-ahead each time.

## Steps

1. **Reader: new types.** Tests first in `TransactionTests`, on statements the generator renders in the test (G-PAY, cash withdrawal, unknown card-like, unknown transfer-like, unknown with no amount, unknown with a bad booking date, unknown that doesn't reconcile, "PLATBA KARTOU X-PAY" stays unknown). Model and generator changes needed to render them, then reader changes. The existing samples still read as their records say.
2. **Sample 10.** Add the scenario, record field and leak-check probe; regenerate the samples (`SampleGenerator -- generate`). Only the sample-10 files are new, and the others stay byte-identical (`CommittedSamplesTests`). Reader test: sample 10 reads as its record. Add sample 10 to the test sample lists that enumerate valid samples (categorization, fit window).
3. **Categorization.** Tests first: type rule with cash withdrawal matches; `Unknown` refused in a type condition; text rule on a printed type matches an unknown and not a known type; a cash withdrawal at a "TESCO" ATM goes to Nezaradené výdavky, while a user text or type rule catches it; nothing is lost on sample 10.
4. **Saving: version 2.** Tests first: `rules-v1.json` loads as before; `rules-v2.json` loads with its cash-withdrawal rule; `Write` produces version 2; a version-3 file and a file whose type rule names "somethingNew" are both `Newer`; a cash-withdrawal rule survives a save and load. Update the `CurrentVersion` doc and #13's plan note.
5. **Presentation.** Tests first: type names, plural texts for 1, 3, 5 and 0 (none), the help text, form choices without `Unknown`, transaction row descriptions, `UnknownTypes` on sample 10 and its absence on sample 01.
6. **Desktop and StatementCheck.** Window tests: the note is visible with sample 10 and hidden with 01, and its help mark has the tooltip; the window still fits at 1280 × 940 (`FitWindowTests` with sample 10). StatementCheck prints the count (`SampleVerifierTests` or a new test on sample 10's output) and no content.
7. **Screenshots.** Add `22-unknown-types`. Generate, check that the screens touched by the key figures (01, 02, 03 …) still match `work/011-modern-look/screenshots/` apart from intended changes. **Review stop: the maintainer reviews the screenshots.** After approval, commit them under `work/041-unknown-transaction-kinds/screenshots/`.
7a. **Details labels the reader doesn't know** (added after the first hand check; spec: Details with a label the reader doesn't know). Tests first: a transfer with "Pôvodný odosielateľ:" and a wrapping "Konečný príjemca:" reads as without them; a non-label line still rejects. `TransactionSection.Blocks` accepts a line at the date column that matches a label pattern (one to four words of letters and dots, the last ending in ":") as a detail under that label; `Detail(...)` lookups only ask for known labels, so the value is dropped. The generator gets a render-only `OtherDetails` list on a transaction, the leak-check probe prints both labels, and sample 10's salary transfer gets them (its record doesn't change).
8. **Hand check.** **Stop: the maintainer**, with their go-ahead, runs `dotnet run --project tools/DocuSpend.StatementCheck` on `samples/private/` (the October statement reads OK with 0 of unknown type), then opens it in DocuSpend: G-PAY payments are "Platba mobilom" and the cash withdrawal is in Nezaradené výdavky with "Výber hotovosti · …".
9. **Finish** with `finish-item`: glossary (transaction type: cash withdrawal, unknown type, printed type), AGENTS.md (saved shape at version 2, `rules-v2.json`), the reader spec's type list in #2 pointed at this item, the intent to `done`.

Each step is one commit, made once all tests pass.

## Risks and unknowns

- **How other unknown rows are laid out.** Only the cash withdrawal has been seen, and it is card-like. A future unknown row may print in a way that neither layout covers (for example the amount in the fee column). It is then rejected as today, with a clearer message, and becomes a new sample. Fallback: none needed now.
- **A printed type that sits partly in the card column** (long text running past x = 221.9). Reading the type as "between the account column and the VS column, minus a card word" covers it. Test: an unknown type long enough to cross the card column.
- **Text rules on printed types and diacritics.** Text matching already ignores case and diacritics; the printed type goes through the same test. Nothing new.
- **Screenshots shift.** The key figures row gets a second column; at 1280 wide nothing should move when the note is absent. If the existing screenshots change, that is investigated before review rather than accepted.
- **Sample regeneration changes old samples.** Guarded by `CommittedSamplesTests`; the G-PAY switch defaults to A-PAY so old rows render as before.

## Verification

| Acceptance criterion | Check |
|---|---|
| G-PAY row read as wallet card payment with card, value date, description, reference | `TransactionTests` (step 1); sample 10 record (step 2) |
| VÝBER HOTOVOSTI read as cash withdrawal with card, value date, description, reference | `TransactionTests`; sample 10 record |
| Unknown card-like row opens with unknown type, printed type, card, value date, description, amount | `TransactionTests`; sample 10 record |
| Unknown transfer-like row: symbols and details read | `TransactionTests`; sample 10 record |
| Unknown row without amount rejected, naming page and number | `TransactionTests` / `RejectionTests` (step 1) |
| Unknown row with damaged booking date rejected | `TransactionTests` (step 1) |
| Unknown type in a statement that doesn't add up rejected as not reconciling | `ReconciliationTests` (step 1) |
| Known types carry no printed type; samples read as recorded; existing reader tests pass | sample tests over 01 to 05 and 10; existing tests unchanged |
| Details lines with unknown labels, one wrapping, are accepted and dropped | `TransactionTests`; sample 10 (step 7a) |
| A left-edge line that is neither a date nor a label still rejects | `ReconciliationTests` (step 7a) |
| Type rules: wallet catches G-PAY, cash catches cash, none catches unknown | `ConditionTests` (step 3) |
| Text rule on printed type catches unknown, not a known type printing the same word | `ConditionTests` (step 3) |
| Cash withdrawal with "TESCO" goes to Nezaradené výdavky; user text or type rule catches it | `CategorizerTests` (step 3) |
| Unknown not caught goes to the right Nezaradené bucket; nothing lost | `SampleCategorizationTests` on sample 10 (step 3) |
| Rule form offers "Výber hotovosti"; list reads "Typ: Výber hotovosti" | `RuleFormViewModelTests`, `MainViewModelTests` (step 5) |
| Transaction list descriptions for unknown and cash; other rows unchanged | `StatementViewModelTests` (step 5) |
| Note texts for 1, 3, 5; help mark and tooltip; no note with none | `TextsTests`, `StatementViewModelTests` (step 5); `MainWindowTests` (step 6) |
| Screenshots with the note, both looks, reviewed | Hand check, maintainer (step 7) |
| Cash-withdrawal rule saved and loaded after restart | `SavedRulesTests`, `RuleStoreTests` (step 4) |
| First-shape file still loads the same | `FirstShapeTests`, unchanged (step 4) |
| A file with a cash-withdrawal rule is one the previous release treats as newer | `SecondShapeTests`: `Write` gives version 2, and the previous release's check (any version above 1 is newer, already tested in `SavedRulesTests`) covers the rest (step 4) |
| A saved type rule naming an unknown type is newer and the file untouched | `SavedRulesTests` (reading), `RuleStoreTests` (file untouched) (step 4) |
| StatementCheck prints the count and no text | `SampleVerifierTests` / StatementCheck test on sample 10 (step 6) |
| No networking, telemetry or unreviewed dependency; samples synthetic with fictional bank | Privacy tests in `dotnet test`; leak check and fictional-identity tests over sample 10 (step 2) |
| October statement opens, G-PAY as mobile, cash in Nezaradené výdavky, 0 of unknown type | Hand check, maintainer (step 8) |
