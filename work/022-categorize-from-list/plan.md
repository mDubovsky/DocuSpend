# Plan: Categorize a transaction from the list

Spec: [spec.md](spec.md)

## Approach

No new dependency. Everything is .NET 10's own library (SDK 10.0.100, `global.json`) and the packages the projects already use. The privacy lists don't change.

### Choices to know about first
- **The fingerprint is a keyed hash: HMAC-SHA256 with a random key of 32 bytes made on this computer the first time a hand choice is made, and kept in `rules.json` beside the hand choices.** With a plain hash (no key), the same transaction would give the same fingerprint for every user, so anyone holding a `rules.json` could test guesses ("did they pay 9,99 € to NETFLIX on 3 March?") against a table computed once for everyone. With a key of this installation's own, a guess can only be tested by someone who has this very file. The key doesn't stop someone who has the file and the statement (or exact knowledge of a transaction) from checking it, which the privacy box already says. See "Alternatives" for a key protected by Windows.
- **The spec's sentence on what the fingerprint can reveal gets one clarification.** "Kto by však mal ten istý výpis a prístup k tomuto počítaču" is true while the file stays on this computer; a copy of `rules.json` taken elsewhere carries its key along. The privacy box and PRIVACY.md will say instead: "Kto by však poznal presné údaje transakcie, napríklad z toho istého výpisu, a mal by váš súbor rules.json, vedel by overiť, či ste túto transakciu zaradili ručne." The spec is changed in the same step as the text (step 4).
- **The saved shape goes to version 3** (`SavedRules.CurrentVersion`). An earlier DocuSpend reading it takes it for a newer one and leaves it untouched, as #13 designed. `rules-v1.json` and `rules-v2.json` stay as they are and must still load.
- **No new sample PDFs.** None of the committed samples has identical transactions or overlapping periods (checked on 2026-11-03 against every `*.expected.json`: no two transactions share date, amount, description and counterparty, and no two periods overlap). Those cases are tested with statements built in the tests, as `TestTransactions` already does; the window tests show such a statement directly. The samples are used for everything else.

### Verified before planning (2026-11-03)
- `System.Security.Cryptography.HMACSHA256.HashData(byte[] key, byte[] data)` and `RandomNumberGenerator.GetBytes(int)` exist in .NET 10 and live in the framework assembly `System.Security.Cryptography`, not a package; a throwaway project in the scratchpad ran 100 000 fingerprints in 56 ms, so fingerprinting every transaction of a statement on each categorization costs nothing noticeable.
- Neither type is in `privacy/BannedSymbols.txt` (it bans `System.Net.*`, `System.Diagnostics.Process` and the like), and framework assemblies are outside the dependency check, so the privacy checks need no entry.
- `SavedRulesJson.Strict` treats constructor parameters with a default value as optional (`RespectRequiredConstructorParameters` only requires those without one), so version 3's new fields can be added to `SavedFile` with defaults, and the reader checks that a version 1 or 2 file doesn't have them.
- The deletion box (`DeleteDialog` in `MainWindow.axaml`) is the pattern for both new boxes: a scrim, a `card dialog`, cycling Tab, and Escape bound to a command.

### Categorization (`DocuSpend.Categorization`)
- **`Rules/Fingerprint.cs`:** `Fingerprint` (a record wrapping 64 lower-case hex characters), `FingerprintKey` (32 bytes; `FingerprintKey.Create()` draws them from `RandomNumberGenerator`; kept as base64), and `Fingerprints.Of(Statement, FingerprintKey)`, which returns one fingerprint per transaction, in order. The hashed text is, field by field, each as a length-prefixed UTF-8 string so no two different tuples can run together: the booking date (`yyyy-MM-dd`), the amount (invariant, two decimals), the description, the counterparty name and the counterparty account (a missing field is written differently from an empty one), and the place among earlier transactions of the statement with the same five, starting at 1. A version tag (`docuspend-fingerprint-1`) leads, so the recipe can change one day without old fingerprints matching new ones by accident.
- **`CategorizationConfig`** gains `HandChoices` (fingerprint → sub-category, in the order made) and `FingerprintKey?` (null until the first hand choice). New methods, immutable like the rest: `SetHandChoice(Fingerprint, SubcategoryId)` (refuses an unknown target or an Uncategorized bucket, replaces an existing choice), `RemoveHandChoice(Fingerprint)`, and `WithFingerprintKey(FingerprintKey)` (only when none is set). `Create` checks every hand choice's target as it checks rules'.
- **`Categorizer`:** when there are hand choices, it fingerprints the statement first; a transaction whose fingerprint has a hand choice is placed there, before any rule is tried. `CategorizedTransaction` gains `ByHand` (true for those; `Rule` is then null) and `Fingerprint?` (set whenever a key exists, so the screen can undo or replace without recomputing). It stays a pure function: the key comes from the configuration, never from randomness here.

### Saving (`Saving/SavedFile.cs`, `SavedRules.cs`)
- `SavedFile` gets `string? FingerprintKey = null` and `List<SavedHandChoice>? HandChoices = null`, with `SavedHandChoice(string Fingerprint, string Target)`. Version 3 writes both (an empty list and no key when there are none).
- Reading: version 1 or 2 with either field is damaged. Version 3: a key that isn't 32 bytes of base64, a fingerprint that isn't 64 hex characters, a repeated fingerprint, or hand choices without a key are damaged; a target that is a missing user sub-category is damaged, a missing built-in one is newer, exactly as for rules.
- A new committed `tests/DocuSpend.Categorization.Tests/Saving/rules-v3.json` (synthetic: one rule of each kind, a user category, two hand choices) with its test, fixed from then on like the other two. `AGENTS.md` names it in the last step.

### Settings (`Look/SettingsStore.cs`)
- The settings file keeps `version: 1` and gains an optional `hideFingerprintNote` (true when "Túto správu už nezobrazovať" was ticked). The store loads and saves a small `Settings` record (look and the note), so saving one never drops the other. An older DocuSpend ignores the field; if it then saves the look, the field is lost and the box shows once more, which the spec accepts.

### Screen logic (`DocuSpend.Presentation`)
- **`RuleFormViewModel.Prefill(Transaction)`** fills every kind's fields from the transaction and picks the first kind (the spec's table), so the box and "Pravidlá" share one form and one set of checks. Switching kind afterwards keeps each kind's prefilled value, since all kinds' fields are filled at once.
- **`CategorizeViewModel`** (the "Zaradiť" box): the transaction and its row texts, its own `RuleFormViewModel` for target and condition, the choice `JustThis` / `Rule`, the error, and the privacy box state (`ShowsPrivacy`, `HideNoteNextTime`).
- **`StatementViewModel`:** `SelectedTransaction` (cleared when the list changes), `TransactionRow` gains `ByHand`, `Fingerprint` and its index in the statement, and a `TransactionsMessage` / `TransactionsError` pair under the card's heading, which survives a category emptying.
- **`MainViewModel`:** commands `Categorize(row)` (opens the box), `ConfirmCategorize`, `ConfirmPrivacy`, `BackFromPrivacy`, `CancelCategorize` (Escape too) and `UndoHandChoice(row)`. A hand choice creates the key if there is none, sets the choice and goes through `ApplyUserChange`, as every saved change does. A rule is checked with `condition.Matches(transaction)` first, then added with `AddUserRuleOnTop`, and the transaction's own hand choice is removed in the same change, so one save covers both. The messages and the "can't be saved" texts follow the spec's table; new texts go in `Texts.cs`.

### The window (`DocuSpend.Desktop/MainWindow.axaml`)
- The transactions `ListBox` binds `SelectedItem`; its row template has a fifth column holding the "ručne" tag and, when the row is selected or the tag is pointed at, "Zaradiť" and "× Zrušiť zaradenie" (bound to `$parent[ListBoxItem].IsSelected` and the tag's `IsPointerOver`). The description column gives up its width.
- Two boxes after the deletion box's pattern: `CategorizeDialog` (the form's condition controls are the "Pravidlá" form's, factored into a `UserControl` both use, so they can't drift) and `PrivacyDialog` above it. Escape closes the top box. Styles only from `Look.axaml`; the "ručne" tag reuses the look's `Border.tag` style, as "vlastná" does.

### Alternatives considered
- **A plain SHA-256 with no key.** Simpler, but every user's fingerprint of the same transaction would be the same, so one table of guesses would test anyone's file. A key costs a few lines.
- **A key protected by Windows (DPAPI, `ProtectedData`), so a copied file is useless elsewhere.** It needs the `System.Security.Cryptography.ProtectedData` package (a new dependency to review), and a reset Windows profile would make every hand choice silently unrecognisable. The gain over a key in the file is small for a file in the user's own folder; it can come later without changing what the user sees.
- **Keeping the hand choices in a file of their own.** They would need their own saving, failure messages and damaged-file handling, all of which `rules.json` already has; the issue suggested the same file.
- **Storing the transaction's reference number (`reference`) instead of its texts.** It may identify a transaction uniquely, but nothing says it stays the same between downloads or formats, and the intent names the texts.
- **A new sample PDF with identical and overlapping transactions.** The generator could make one, but the cases are about categorization, not reading, and statements built in code test them directly.

### Layout
```
src/DocuSpend.Categorization/Rules/Fingerprint.cs              new: Fingerprint, FingerprintKey, Fingerprints
src/DocuSpend.Categorization/CategorizationConfig.cs           hand choices and the key
src/DocuSpend.Categorization/Categorizer.cs                    hand choices first
src/DocuSpend.Categorization/Results/CategorizedStatement.cs   ByHand, Fingerprint
src/DocuSpend.Categorization/Saving/SavedFile.cs, SavedRules.cs  version 3
src/DocuSpend.Presentation/CategorizeViewModel.cs              new: the "Zaradiť" box and the privacy box
src/DocuSpend.Presentation/RuleFormViewModel.cs                Prefill
src/DocuSpend.Presentation/StatementViewModel.cs, MainViewModel.cs, Texts.cs, Look/SettingsStore.cs
src/DocuSpend.Desktop/MainWindow.axaml(.cs), ConditionFields.axaml(.cs)  new user control shared by both forms
tests/DocuSpend.Categorization.Tests/HandChoiceTests.cs, FingerprintTests.cs, Saving/rules-v3.json, Saving/ThirdShapeTests.cs
tests/DocuSpend.Presentation.Tests/CategorizeTests.cs, PrefillTests.cs (and NothingKeptTests, SettingsStoreTests extended)
tests/DocuSpend.Desktop.Tests/CategorizeWindowTests.cs
tools/DocuSpend.Screenshots/Screens.cs                         new scenes
PRIVACY.md, docs/mvp.md, docs/glossary.md, AGENTS.md
```

## Prerequisites
- #25 and #41 are merged into `main` (they are: `5f16902`, `80ce0f4`).
- The maintainer reviews the screenshots (step 6) and does the hand check on a real statement (step 8).

## Steps
1. **Fingerprints and hand choices in the categorization**, test first: `FingerprintTests` (same transaction gives the same fingerprint, any field changed gives another, the place among identical ones, a missing field differs from an empty one, another key gives another fingerprint) and `HandChoiceTests` (a hand choice beats user and default rules and a newer rule; replace; remove; refused targets; the overlapping and re-downloaded statements built in code; the randomized invariant test still holds with hand choices). Sets the intent to `in-progress`.
2. **Saved shape version 3**, test first: round trip with hand choices and key; every damaged and newer case above; `rules-v1.json` and `rules-v2.json` load with no hand choices; `rules-v3.json` committed with its test.
3. **The privacy note setting**: `SettingsStore` keeps look and note together; tests for each surviving a save of the other, and for an old settings file.
4. **Screen logic**, test first: `PrefillTests` (the table, the first kind for each transaction type, empty fields), `CategorizeTests` (every acceptance criterion below the window: both choices, the privacy box and its tick, the refused condition, the rule replacing the transaction's hand choice, undo, the messages and the "can't be saved" texts, selection clearing). Changes the spec's privacy sentence as above, in the same commit.
5. **The window**: the row's selection, buttons and tag; both boxes; the shared condition control. `CategorizeWindowTests` drives them headless, including Escape, the tag's pointer-over, and the privacy box fitting the default window in both looks. The look test passes.
6. **Screenshots** of the spec's list, in both looks, added to `tools/DocuSpend.Screenshots`. **Stop: the maintainer reviews them.** Changes from the review go into the spec and are built before going on.
7. **What is kept, said in full**: PRIVACY.md and mvp.md as the spec says; `NothingKeptTests` extended to make hand choices on every valid sample and check that nothing written contains a transaction's date, amount, description, counterparty or account, or the file's name or folder; a test that the privacy box's text and PRIVACY.md's agree on the facts (the folder, the file, "v tomto počítači").
8. **Hand check. Stop: the maintainer** categorizes transactions of a real statement both ways, undoes one, restarts, reopens and finds everything as left, then opens `rules.json` and sees no detail of the transactions.
9. **Finish** with the `finish-item` skill: glossary ("hand choice", "fingerprint"), AGENTS.md (`rules-v3.json`, the shared condition control), README if it lists features, the intent set to `done`.

## Risks and unknowns
- **A re-downloaded statement prints a transaction's text slightly differently** (spaces, line breaks). The reader already normalises spaces; if the hand check shows hand choices lost on a re-download, the fingerprint can fold spaces and case before hashing (a new recipe version, `-2`), at the cost of the spec's "differs in any detail".
- **The shared condition control** may be awkward to bind in two places. Fallback: the box gets its own copy of the fields, and a test compares the two forms' controls so they can't drift.
- **The fifth column squeezes the description** at the default width. The screenshot review decides; the fallback is buttons over the sub-category column while the row is selected.
- **Pointer-over is hard to drive headless.** Avalonia's headless platform can send pointer moves (`MouseMove`); if the tag's hover can't be tested that way, it is covered by the screenshot with the tag pointed at, and the test checks the selected row instead.
- **Found in the build: the box's text field ran past its edge.** The condition fields were laid out for the wide rules card. The text field now sits in a grid column that stretches (minimum 160), so it fits the box; on "Pravidlá" it is a little wider than before.
- **Found in the build: the shared condition control hid fields from window tests.** Names inside a `UserControl` are in its own scope, so the rules screen's tests look up the fields inside `RuleConditionFields` when the window doesn't have them. What they check is unchanged.
- **Found in the build: two tests pinned version 2 of the saved shape.** They now follow `SavedRules.CurrentVersion`; the fixed `rules-v1.json` and `rules-v2.json` are untouched and still load.
- **Found in the build: the screenshot tool hid a scene's exception** behind a font error at shutdown. The scene had picked a category with a single row; it now uses "Nákupy online".
- **Found in the screenshot review:** the shortened description on a selected hand-chosen row was accepted; the buttons stay in their own column.
- **Found in the hand check:** nothing. Both choices, undo, a restart and `rules.json` behaved as the spec says on a real statement.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| Selecting a row shows "Zaradiť"; another category clears it | `CategorizeWindowTests`, `CategorizeTests` |
| The box shows with the transaction; "Zrušiť" and Escape change nothing | `CategorizeWindowTests` |
| Starting sub-category; no "Nezaradené" offered | `CategorizeTests` |
| "Len túto transakciu" first, no condition; "Pravidlom…" shows it | `CategorizeTests`, `CategorizeWindowTests` |
| First kind by transaction type | `PrefillTests` |
| Each kind's starting values, after switching too; empty fields refused | `PrefillTests`, `CategorizeTests` |
| No sub-category: "Vyberte podkategóriu." | `CategorizeTests` |
| Just this one moves only it, also not its identical twin; message | `CategorizeTests`, `HandChoiceTests` |
| Beats user, built-in and later rules | `HandChoiceTests` |
| A second hand choice replaces the first | `HandChoiceTests`, `CategorizeTests` |
| Message stays when the category empties | `CategorizeTests` |
| Charts, totals, categories and list follow at once | `CategorizeTests` (statement view model after the change) |
| A rule from the transaction: added on top, count, first in "Moje pravidlá" | `CategorizeTests` |
| A condition that misses the transaction is refused | `CategorizeTests` |
| An invalid condition gives the reasons of "Pravidlá" | `CategorizeTests` |
| The rule replaces this transaction's hand choice, not others' | `CategorizeTests`, `HandChoiceTests` |
| The privacy box's text, unticked | `CategorizeTests` (text), `CategorizeWindowTests` (shown) |
| "Späť" / Escape back to the box | `CategorizeTests`, `CategorizeWindowTests` |
| The tick hides the box, also after restart; without it, it shows again | `CategorizeTests`, `SettingsStoreTests` |
| No privacy box for a rule | `CategorizeTests` |
| Privacy box fits the default window in both looks | `CategorizeWindowTests` |
| "ručne" tag, tooltip, the button on pointing and selecting; none for rules | `CategorizeWindowTests`; screenshot (pointed at) |
| Undo: no question, back to the rules' place, message, kept after restart | `CategorizeTests` |
| Hand choice back after restart | `CategorizeTests` (new view model on the same folder) |
| Overlapping statements | `HandChoiceTests` |
| Identical transactions: the chosen one stays chosen | `FingerprintTests`, `HandChoiceTests` |
| A changed description isn't recognised | `FingerprintTests`, `HandChoiceTests` |
| Nothing written holds a transaction's details or the file's name | `NothingKeptTests` |
| Version 3 keeps everything; v1 and v2 load; `rules-v3.json` fixed | `SavedRulesTests`, `FirstShapeTests`, `ThirdShapeTests` |
| A hand choice with a missing target: damaged or newer | `SavedRulesTests` |
| Saved before the message shows | `CategorizeTests` (file checked when the message is set) |
| Can't write / saving off: the table's texts | `CategorizeTests` |
| PRIVACY.md and mvp.md; box and PRIVACY.md agree | step 7 test; reviewed in the PR |
| Shared look only | the existing look test |
| Privacy checks, no new entries | `DocuSpend.Privacy.Tests` |
| Slovak texts | `TextsTests` |
| Screenshots approved | step 6, maintainer |
| Hand check on a real statement | step 8, maintainer |
