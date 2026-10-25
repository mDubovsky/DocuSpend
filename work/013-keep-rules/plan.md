# Plan: Keep rules and categories between sessions

Spec: [spec.md](spec.md)

## Approach

**No new dependency.** The saved rules are a JSON file written with `System.Text.Json`, which is part of the .NET 10 shared framework DocuSpend already runs on (10.0.13 on the maintainer's machine). It ships no package, so the privacy checks and reviewed lists don't change. The spike's file and its store are removed.

The work has two parts:
- **The saved shape**, in `DocuSpend.Categorization`. It is pure (text in, text out, no files), and it rebuilds a configuration with the same invariant checks the categorizer already uses.
- **The file**, in `DocuSpend.Presentation`: reading, saving safely, moving a damaged file aside, and telling locked from damaged.

Verified on 25 October 2026 in a scratch project on .NET 10.0.13, Windows 11:
- **A file held open by another program** (`FileShare.None`): reading it throws `IOException` (sharing violation, `0x80070020`), and so does moving it. The existing statement-file tests show the same holds on the Linux CI runner.
- **Replacing a file another program has open,** even only for reading and with delete sharing allowed, fails with `UnauthorizedAccessException`. A save at that moment fails and is reported as a failed save (spec: when a change can't be saved).
- **A folder where the file should be,** read as a file, throws `UnauthorizedAccessException`. A file where the folder should be makes `Directory.CreateDirectory` throw `IOException`. The tests use these two cross-platform ways to make "no permission" and "the folder can't be written" without changing access rights.
- **Strict reading:** with `RespectRequiredConstructorParameters`, `RespectNullableAnnotations`, `AllowDuplicateProperties = false` and `UnmappedMemberHandling.Disallow`, the following all throw `JsonException`: a missing required field, a `null` for a non-nullable one, a duplicated or unknown field, a number written as a string, a comment and trailing text. A context built from explicit `JsonSerializerOptions` ignores the options in the generator attribute, so all options are set on the `JsonSerializerOptions` the context is built with.
- **Exact values:** `12.50m` is written as `12.50` and read back as `12.50`, with its scale. `Café „Ž“ 😀 lekáreň` round-trips exactly. With `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, Slovak letters are written as themselves, so a user who opens the file can read it (emoji are still escaped, which is harmless). The default encoder escapes every non-ASCII letter.

### The saved shape, in `DocuSpend.Categorization`
- **`Saving/SavedRules`** has two methods:
  - `Write(CategorizationConfig) → string` writes only the user's part;
  - `Read(string, CategorizationConfig defaults) → SavedRulesReading`, which gives one of: the restored configuration (`Loaded`), `Damaged`, or `Newer`. The reason is kept for tests and developers, never shown.
- **The file**, version 1:
  ```json
  {
    "version": 1,
    "categories": [
      { "id": "user-…", "name": "Deti", "role": "spending",
        "subcategories": [ { "id": "user-….other", "name": "Ostatné" }, { "id": "user-…", "name": "Krúžky" } ] }
    ],
    "subcategories": [ { "category": "housing", "id": "user-…", "name": "Garáž" } ],
    "rules": [
      { "id": "user-…", "priority": 1, "order": 3, "target": "housing.rent",
        "condition": { "kind": "text", "text": "Nájom", "mode": "contains" } }
    ]
  }
  ```
  `categories` holds the user's own categories, each with all its sub-categories. `subcategories` holds the user's sub-categories in built-in categories. Conditions are flat, with a `kind` and only that kind's fields:
  - `type` (`types`);
  - `text` and `counterpartyName` (`text`, `mode`);
  - `counterpartyAccount` (`account`);
  - `amount` (`min`, `max`, either one optional);
  - `variableSymbol` (`symbol`).
- **Stable words, not C# names.** Roles (`spending`, `income`, `excluded`), modes (`contains`, `equals`) and transaction types (`cardPayment`, `walletCardPayment`, `transfer`, `instantTransfer`, `accountFee`, `feeReversal`) are written through fixed tables, never `Enum.ToString()`. Renaming an enum member in the code then can't change saved files. A test fails if an enum value has no word.
- **Reading, in order:**
  1. Read the file as a JSON document. If that fails, or `version` is missing or not a whole number, the file is `Damaged`.
  2. If `version` is above the current version, the file is `Newer`.
  3. Read the current shape strictly (the options above). Older versions get their own reader when there are any, and the next save writes the current shape.
  4. Check the contents:
     - **`Damaged`:** an empty or duplicated identifier; a user category or sub-category whose identifier doesn't start with `user-`; a category without exactly one Ostatné at `<id>.other`; a blank name, or two of the user's own siblings with the same name (ignoring case and diacritics, as the catalog does); a sub-category in an Uncategorized bucket, or in an unknown `user-` category; a rule with priority below 1, a creation order below 1 or a repeated one, a condition that fails its own `Validate()`, or a target `user-` sub-category that doesn't exist.
     - **`Newer`:** a target, or a `subcategories` category, that is a built-in identifier this version doesn't have.
     - **Allowed:** a user sub-category whose name matches a built-in sibling.
  5. Only then build the configuration with the existing internal `CategorizationConfig.Create`, so its invariant checks also run.
- **Built-ins stay out.** `Write` finds the user's part by comparing against the built-in catalog: categories with `IsBuiltIn` false, and sub-categories of built-in categories that the built-in catalog doesn't have. Default rules are never written.
- **The first shape is fixed** by `tests/DocuSpend.Categorization.Tests/Saving/rules-v1.json`, a synthetic file with every kind of condition, a category of each role, sub-categories in built-in and user categories, shared priority numbers and a changed priority. Its test checks what it loads to and how it categorizes the samples, and is never changed to match a new shape.

### The file, in `DocuSpend.Presentation`
- **`RuleStore(string path, TimeProvider clock)`** replaces `SpikeRuleStore`. Its default path is `%APPDATA%\DocuSpend\rules.json`.
  - **`Load(defaults) → RuleStoreLoad`** gives the configuration plus one of `None`, `Damaged(movedTo)`, `CannotOpen` or `Newer`:
    - missing file: `None`, with the defaults;
    - any `IOException` or `UnauthorizedAccessException` while reading: `CannotOpen`, and saving off;
    - `Newer`: saving off;
    - `Damaged`: the file is moved to `rules.damaged-yyyyMMdd-HHmmss.json` in the same folder (local time from `clock`, with `-2`, `-3`, … if that name exists). If the move fails, `CannotOpen` and saving off (spec: edge cases).
  - **`Save(config) → bool`:**
    - writes to `rules.json.<random>.tmp` in the same folder, flushes it to disk (`FileStream.Flush(flushToDisk: true)`) and moves it over `rules.json` (`File.Move(…, overwrite: true)`, a single rename on the same volume);
    - deletes its own temporary file if the move fails, and creates the folder if it is missing;
    - refuses while saving is off (`IsSaving` false);
    - returns false on `IOException` or `UnauthorizedAccessException`.

    A random temporary name means two DocuSpend windows never write the same temporary file. A leftover from a crash is never read, because only `rules.json` is.
- **`MainViewModel`:**
  - takes a `RuleStore` and loads it at start;
  - shows the result as `RulesMessage`: the startup message from the spec's table, or null, closed with `CloseRulesMessageCommand`;
  - sets `RulesNotSaving` when saving is off;
  - builds the rules list from `Config.UserRules` (in creation order), not from the old list of drafts;
  - in `AddRule`, adds the rule on top, applies it, then saves. The confirmation shows as today. When saving is off, the form adds "Pravidlo platí len do zatvorenia DocuSpend." When the save fails, it adds the failed-save message.

  One private method, `ApplyUserChange(config)`, applies and saves every change, so the rule editor and categories screen will save the same way.
- **`Describe`** covers all six kinds, with the spec's new texts for counterparty name, counterparty account and variable symbol.
- **`Texts`:**
  - **new:** the three startup messages (the damaged one with a `{cesta}` placeholder), "Zmeny pravidiel sa teraz neukladajú.", "Pravidlo platí len do zatvorenia DocuSpend.", the failed-save message and the three rule descriptions;
  - **removed:** `RulesFileUnreadable`, `RulesCannotBeSaved` and `RuleNotSaved`;
  - "Pravidlo je uložené, ale v tomto výpise mu nezodpovedá žiadna transakcia." stays as it is.

### The window, in `DocuSpend.Desktop`
- **Under the header,** a second message area for `RulesMessage`, in the existing `message error` style with "Zavrieť", shown on the start screen and over a statement. It stands beside `OpenMessage`, not in its place.
- **In "Moje pravidlá",** the existing error line now shows "Zmeny pravidiel sa teraz neukladajú." while `RulesNotSaving` is true.
- **Startup and the designer:** `App.axaml.cs` and the designer constructor create a `RuleStore` instead of the spike's store. Everything uses only the look's resources, which the existing check enforces.

### Tests, tools and privacy
- **Replacing the spike's store:** `SpikeRuleStore` and `SpikeRuleStoreTests` are removed. The window tests, the presentation tests and `tools/DocuSpend.Screenshots` use `RuleStore` on a temporary folder.
- **Nothing kept:** in `NothingKeptTests`, `RuleStore.cs` replaces `SpikeRuleStore.cs` in the list of the only files allowed to write. Its after-use test also checks dates and amounts from the samples, and that the moved-aside and temporary files contain nothing from a statement either.
- **New screenshots:** the start screen with the damaged-file message, and a statement with the can't-open message and the not-saving line in "Moje pravidlá", in both looks.
- **PRIVACY.md:** what is kept, where, the moved-aside file, the spike's file, and that deleting `%APPDATA%\DocuSpend` removes everything. `PrivacyDocumentTests` already checks parts of PRIVACY.md; it gets a check that the rules file's name is mentioned.

### Alternatives considered
- **`System.Text.Json` polymorphism (`[JsonDerivedType]`) on the condition records:** less code, but it ties the saved file to the C# class names and shapes, so a refactor could break saved rules. Explicit saved records with fixed words keep the file under our control.
- **A database (SQLite, LiteDB):** a new dependency with native files to review (decision 0006), for a few kilobytes of data. Also harder for a user to inspect.
- **Keeping the store in `DocuSpend.Presentation` only, as the spike did:** the shape and its checks need the categorizer's internal `Create` and invariants, and they are tested best without files. The file handling, which is about Windows and not about categories, stays in Presentation.
- **`File.Replace` instead of `File.Move` with overwrite:** both are a single rename on Windows. `File.Replace` needs the target to exist and offers a backup file we don't want, so the first save would need a separate path.
- **Loading the readable rules of a partly damaged file:** ruled out by the spec.
- **Lenient reading (comments, unknown fields):** a hand-edited file with an unknown field would load and then lose that field on the next save. Strict reading moves it aside instead, so nothing is lost.

### Layout
```
src/DocuSpend.Categorization/Saving/    SavedRules, the saved records, the word tables, the JSON context
src/DocuSpend.Presentation/             RuleStore; MainViewModel, Texts changed; SpikeRuleStore removed
src/DocuSpend.Desktop/                  MainWindow.axaml(.cs), App.axaml.cs changed
tools/DocuSpend.Screenshots/            the new states
tests/DocuSpend.Categorization.Tests/Saving/   SavedRules tests, rules-v1.json
tests/DocuSpend.Presentation.Tests/     RuleStoreTests; MainViewModel, NothingKept, Texts tests changed; SpikeRuleStoreTests removed
tests/DocuSpend.Desktop.Tests/          window tests changed
```

## Prerequisites
- Items 003, 005 and 012 merged. ✔
- The maintainer reviews the screenshots and the PRIVACY.md wording, and does the hand checks with the built application and their own statements.

## Steps
Test-first throughout. One commit per step, each after the maintainer's approval. The first commit sets the intent's status to `in-progress`.

1. **The saved shape:** `SavedRules` with its records, word tables and strict reading.
   - Round trip of every kind of condition, categories of each role, sub-categories in built-in and user categories, shared and changed priorities, and exact texts and amounts.
   - Every valid sample categorized the same after a round trip, rule for rule.
   - A rule added after loading wins the tie.
   - Built-ins and default rules never in the file.
   - Every damaged case, and both newer cases.
   - `rules-v1.json` and its test.
2. **The file:** `RuleStore`.
   - Missing file, missing folder, a file locked by the test, a folder in the file's place, and a file in the folder's place.
   - Each damaged case moved aside byte for byte, two damaged files on two loads, and a move aside that fails.
   - A newer file left untouched, and saving off refused.
   - An interrupted save simulated by leftover full and partial temporary files next to an older `rules.json`.
   - A thousand rules saved and loaded under a second.
3. **The view model:** `MainViewModel` on `RuleStore`, the startup messages, the not-saving line, `AddRule` with saving off and with a failed save, `Describe` for all kinds, and the new texts.
   - `SpikeRuleStore` is removed, and its users switch over, so the build stays green.
   - Tests for each startup case, closing the message, the message next to `OpenMessage`, the order of "Moje pravidlá" after a restart, the spike's file ignored, and the texts being Slovak and free of file content.
4. **The window:** the second message area and the not-saving line.
   - Window tests: the message on the start screen and over a statement, closing it, and a rule kept across two windows on one folder.
   - Screenshots of the new states in both looks (**stop: screenshot review**).
5. **Privacy:** `NothingKeptTests` extended, and `PRIVACY.md` updated with its document test (**stop: the maintainer reads the PRIVACY.md wording**).
6. **Hand checks and documentation** (**stop: hand checks**).
   - The maintainer checks a rule kept across a restart with a real statement, and the can't-open message with the file held open by another program, in the CI build.
   - `AGENTS.md` and `README.md` get a line where they describe what the application does. Anything that surprised during the build is noted in this plan.
   - The intent's status becomes `done`.

## Risks and unknowns
- **Another program holds the file during a save** (antivirus, a backup or sync tool). The save fails and is reported, and the next change tries again (spec). If the hand checks show this happens often, a single retry after a short pause can be added without changing the spec.
- **`%APPDATA%` redirected to a network share or synced by OneDrive.** The rename may not be a single step there. A failed save is still reported, and the previous file is replaced only by a complete one. Any network use is Windows' doing, as PRIVACY.md already says.
- **Locks on Linux are advisory.** .NET honours `FileShare.None` between .NET handles on Linux, which the existing statement-file test relies on in CI. If a store test behaves differently there, it runs on Windows only and says why.
- **A crash between flushing and renaming** leaves a temporary file that nothing reads. These are rare and tiny, and DocuSpend doesn't delete files it can't be sure are its own leftovers. If they turn out to pile up, a later change can remove old ones at start.
- **The local clock in the moved-aside name** can repeat or go backwards (time zone changes). The `-2`, `-3` suffix keeps names unique whatever the clock says.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| Every kind of rule, user categories of each role and user sub-categories round-trip with identifiers, conditions, targets, priorities, orders, names and roles | `SavedRules` round-trip tests; `RuleStore` save-and-load test |
| Valid samples `01`–`05` categorized the same before and after, rule for rule | `SavedRules` sample test; view model restart test |
| Shared priority numbers and a changed priority decide the same after loading | `SavedRules` tests; `rules-v1.json` test |
| A rule added after loading wins the tie | `SavedRules` test |
| Diacritics, capitals, inner spaces, `Café „Ž“ 😀` and cents come back exactly | `SavedRules` and `RuleStore` tests |
| In the window: a rule added in one window is listed and applied in a new window on the same folder | View model restart test; window test |
| Built-ins and default rules never saved; a loaded file uses current built-in names and default rules | `SavedRules` tests (file contents; load against a changed built-in name) |
| A rule is saved before the confirmation appears | View model test (the file holds the rule when the message is set) |
| An interrupted save loads the rules before or after, with no message | `RuleStore` tests with leftover full and partial temporary files |
| Folder can't be written: the rule applies, both messages show, and the next save keeps both rules | View model test with a file in the folder's place, then removed |
| No saved rules: no message, built-ins only | `RuleStore` and view model tests |
| `rules-v1.json` loads with the expected rules and categorization | `SavedRules` golden-file test |
| Each damaged case: no user rules, message with the moved path, moved file byte for byte, next rule saved, nothing else changed | `SavedRules` cases; `RuleStore` tests; view model test for the message and the next save |
| Two damaged files kept under different names | `RuleStore` test with a fixed clock |
| A file held open: can't-open message, not-saving line, rule applies with its note, file unchanged | `RuleStore` and view model tests with the test holding the file; hand check (maintainer) with another program |
| A newer file, or an unknown built-in target: as above with the newer message | `SavedRules` and view model tests |
| The startup message on the start screen and over a statement, closable, beside the open message | View model tests; window test |
| No message in English; none contains file content other than the moved path | `TextsTests` (Slovak check); view model test with a damaged file containing sample text |
| `spike-rules.json` ignored and unchanged | View model test |
| Nothing written contains statement data | `NothingKeptTests` (extended) |
| PRIVACY.md describes what is kept, the moved-aside file, the spike's file and how to remove everything | `PrivacyDocumentTests`; review at step 5 |
| Privacy checks pass, no new entries | `dotnet test` in CI; no change to `privacy/` |
| Hand check: a rule kept across a restart with a real statement | Hand check (maintainer), step 6 |
| Hand check: the can't-open message with the file held by another program | Hand check (maintainer), step 6 |
| Screenshots approved | Review at step 4 |
| Many rules (edge case): a thousand load and save under a second | `RuleStore` timing test |
