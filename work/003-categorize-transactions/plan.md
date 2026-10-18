# Plan: Sort transactions into spending categories

Spec: [spec.md](spec.md)

## Approach

**A second class library, `src/DocuSpend.Categorization`, that takes a read `Statement` and a configuration and returns a `CategorizedStatement`.** It is pure logic: no I/O, no network, no user-interface text, no clock, no randomness. It depends only on the model of `DocuSpend.Statements` (`Statement`, `Transaction`, `TransactionType`), so statements from any future reader, for any bank, can be categorized the same way.

### The configuration is one immutable aggregate

`CategorizationConfig` holds everything that decides a category: the category catalog (built-in plus user-created), the default rules and the user rules. All changes go through it and return a new config or a **refusal**, so its invariants can't be broken from outside:

- every rule's target is an existing sub-category;
- every priority number is at least 1; default numbers are unique; user numbers may repeat, and every user rule has a **creation order** number, unique and increasing, assigned by the config when the rule is added (one more than the largest so far), so the tie-break needs no clock and the result stays reproducible; for equal priority numbers the **newest** (highest creation order) wins;
- sibling names are unique, ignoring case and diacritics;
- every category except the Uncategorized buckets has an Ostatné sub-category.

`CategorizationConfig.Default` is the built-in set from the spec. `config.AddCategory(...)`, `AddSubcategory(...)`, `AddUserRule(...)` and `AddUserRuleOnTop(...)` are the operations the later editor and persistence items will use. `AddUserRuleOnTop` takes no priority number: it gives the rule the lowest number any user rule has (1 if there are none), and since the new rule is the newest it wins the tie. It is how a screen can offer "re-categorize" without the user ever seeing a number; the screens, and the "advanced" switch that shows numbers, belong to the UI items. Immutability makes results reproducible, makes "undo" in an editor trivial, and means persistence later only has to store the user-created part (`UserCategories`, `UserRules`), which are plain data.

A refusal carries a `RefusalKind` (empty text, invalid amount range, unknown target, priority below 1, empty name, duplicate name, …) and developer-facing English text. Per [decision 0003](../../docs/decisions/0003-slovak-interface-english-code.md), the Slovak wording is composed by the interface from the kind and details, so no Slovak sentences live in this library. The only Slovak text here is the built-in category names, which are data.

### Stable identifiers

Rules and results refer to categories by **identifier, never by name**, so a built-in name can be corrected later without breaking anyone's saved rules (the consequence recorded in decision 0003). `CategoryId` and `SubcategoryId` are small value types wrapping a string.

- **Built-in:** fixed English keys, e.g. `housing`, `housing.rent`, `housing.other`. The Slovak display name is a property.
- **User-created:** a generated identifier (a GUID), so it can never collide with a built-in key.
- **Default rules:** an identifier derived from the target and the condition, e.g. `default/housing.rent/najom`, so it stays stable when the list is reordered or extended.
- **User rules:** a generated identifier.

Recorded as [decision 0004](../../docs/decisions/0004-stable-category-and-rule-identifiers.md) so the persistence item inherits it.

### The model

- `Category` (id, name, role, sub-categories) and `Subcategory` (id, name); `CategoryRole` is `Spending`, `Income` or `Excluded`. The two Uncategorized buckets are categories with no sub-categories and a flag saying so.
- `Placement` (category id, sub-category id or none) says where a transaction is.
- `Rule` (id, source default or user, priority, creation order, one `Condition`, target sub-category). Rules are tried by priority number and, for equal numbers, from the newest to the oldest. The creation order is what persistence will store next to the rest of a user rule, and an editor that changes a rule's number never touches it. Default rules get their order from the table.
- `Condition` is a closed set of records, one per kind in the spec: `TypeCondition` (a set of types), `TextCondition` (text, `Contains` or `Equals`), `CounterpartyNameCondition` (text, mode), `CounterpartyAccountCondition`, `AmountCondition` (optional minimum and maximum on the size of the amount) and `VariableSymbolCondition`. Each knows how to match a `Transaction`; a field the transaction lacks never matches.

### Text matching

One helper folds text: Unicode decomposition with combining marks removed, lower case, runs of whitespace collapsed to one space. The text being searched is padded with one space at each end before a *contains* test, so patterns with a leading or trailing space ("MOL ", "OBI ") also match at the edges of the text. *Equals* compares the folded texts with surrounding spaces trimmed. The text condition tests the transaction's description and its payment purpose and matches if either does. Account numbers compare with spaces removed and upper-cased. Known limit: letters that don't decompose (for example Polish "ł") are not folded; they don't occur in Slovak or Czech.

### Categorizing

`Categorizer.Categorize(statement, config)` returns a `CategorizedStatement`:

1. For each transaction, in statement order: try the user rules by ascending priority number and, within one number, from the newest rule to the oldest, then the default rules; the first match decides. No match gives the incoming or outgoing Uncategorized bucket (zero counts as incoming).
2. Each result records the transaction, its placement, the deciding rule's id and source (or "no rule matched"), and whether it counts as spending (its category's role is spending).
3. Totals are computed once, in exact `decimal`: balance and count per sub-category and per category; spending (negative of the balance of spending categories), income and excluded totals; and the completeness check, closing minus opening equals income plus excluded minus spending.

The statement is never modified; the function can't fail, because every invariant it relies on was established when the config was built.

### Alternatives considered

- **Putting this into `DocuSpend.Statements`:** rejected; reading a file and interpreting its transactions are separate concerns, and other banks' readers will feed this library.
- **Default rules and categories as an embedded JSON resource:** rejected for now. In code they are checked by the compiler, the list is short, and adding to it is a one-line change with a test. It can move to data if non-developers ever need to edit it.
- **Regular expressions in conditions:** rejected; the spec's modes are contains and equals, and users won't write patterns.
- **A mutable catalog with change events:** rejected; immutability keeps results reproducible.
- **Injecting an identifier generator:** not worth it; the new ids for user rules and categories are the only non-determinism, and tests only need them to exist.

### Layout

```
src/DocuSpend.Categorization/
    Model/         Category, Subcategory, CategoryRole, Placement, identifiers, Refusal
    Rules/         Rule, the conditions, TextMatching
    BuiltIn/       BuiltInCatalog, BuiltInRules (the spec's tables)
    CategorizationConfig.cs, Categorizer.cs
    Results/       CategorizedStatement, summaries
tests/DocuSpend.Categorization.Tests/
```

The library references `DocuSpend.Statements` only. The tests also reference the sample generator, to read the committed samples with the real reader.

## Prerequisites

- Issue #2's reader, merged. ✔
- No new packages: the library uses only the base class library.

## Steps

Test-first throughout; one commit per step group, with the human's approval before each commit.

1. Create `src/DocuSpend.Categorization` and `tests/DocuSpend.Categorization.Tests`, add them to `DocuSpend.slnx`, update the layout note in `AGENTS.md`.
2. Text folding and matching helpers, with tests (case, diacritics, spaces, edges, padding).
3. The six conditions with their matching and validation, tested on constructed transactions: each condition matches what it describes and nothing else; missing fields; amount ranges and thresholds; either text mode.
4. The category model and the built-in catalog (Slovak names, stable identifiers, roles), with tests: Ostatné everywhere except the buckets, unique names, user categories and sub-categories, refusals.
5. Rules and the config: priority numbers, shared numbers with the newest winning, adding a rule on top, refusals, then the built-in default rules from the spec's table with derived ids and consecutive priority numbers. Table-driven tests: every alternative puts its example in the listed sub-category, and no default targets a missing one.
6. The categorizer and the results: decision order (user before default, lower number first), buckets, balances, role totals, completeness, "which rule decided". Tested on constructed transactions, then on the five committed samples read by the real reader.
7. Randomized invariant tests with fixed seeds: for generated transactions and random user rules, every transaction gets exactly one placement, categories equal the sum of their sub-categories, and completeness holds.
8. Docs: decision 0004, glossary (role, balance), `AGENTS.md`, `intent.md` status `done`. Optionally extend `tools/DocuSpend.StatementCheck` with `--categorize`, which prints only counts per category for the real statements. It needs the maintainer's go-ahead like the existing check.

## Risks and unknowns

- **Substring rules are blunt.** A short alternative such as "MOL ", "TETA" or "UBER" can match unrelated text, and the first matching rule wins, so order matters. On an unrelated other-bank export the default set categorized about 71% of transactions, and the rest were mostly transfers to people, banks and local businesses no default can know. Mitigation: the table is tested alternative by alternative, known conflicts are pinned with tests, and wrong defaults are corrected by adding a user rule.
- **Salaries.** The "MZD" rule only matches when the transaction says so; real salaries often don't. A user rule on the employer's account is the intended way, which the later editor must make easy.
- **Spending can be negative** (a refund larger than the purchases in its category). This library reports the signed figure; the charts decide how to show it.
- **Built-in names may need correcting** (the list was typed from a screenshot and a message). Identifiers don't depend on them.
- **The two Uncategorized buckets break the "always a sub-category" rule.** The model makes that explicit (`Placement` with no sub-category), and every consumer has to handle it.
- **Equal numbers resolve by creation order, not by anything the user can see.** The newest wins: that is stable, explainable ("your latest correction wins") and is what lets "re-categorize" add a rule on top without a number. The cost is that a broad rule created later can override an older, more specific one. The editor should show when a rule is hidden by a newer one that can match the same transactions. Alphabetical order was considered and set aside: rules have no names to sort by. Oldest-wins was considered and set aside: putting a correction on top would then need renumbering the user's other rules, and 1 is the highest priority, so there is no number above the top.
- **Rule deletion and category removal** are out of scope, but users of `CategorizationConfig` must not assume targets exist forever; the later editor item will have to decide what happens to rules whose target is removed.

## Verification

Each acceptance criterion in the spec maps to automated tests.

| Spec criterion | How it is checked |
|---|---|
| Every transaction in exactly one sub-category or bucket; nothing lost | Sample test over samples 01 to 05 and 03's awkward cases; randomized invariant test (step 7) |
| Sample 01 examples: NETFLIX, rent, fee pair netting to 0, salary not in spending | Sample test reading sample 01 with the real reader |
| Category balance equals sum of its sub-categories | Sample test and randomized test |
| Ostatné in every category except buckets; never directly in a category with sub-categories | Catalog test; placement test |
| User rule beats default; lower number wins among user rules with different numbers and among defaults | Constructed transactions with competing rules |
| Rules sharing a number: both accepted; the newest decides, whatever order they are given in or later renumbered | Config and categorizer tests that add the rules in different orders and change numbers afterwards |
| A rule added on top beats every existing user rule; gets the lowest existing number, or 1 | Config and categorizer tests, including several rules added on top in a row and rules with different numbers |
| Number below 1, empty text, bad amount range, unknown target refused | Config refusal tests, each with its `RefusalKind` |
| Each of the six conditions, including missing fields | Condition tests on constructed transactions |
| Contains and equals; case, diacritics, spaces ignored | Matching tests |
| Amount matches money in and out | Amount tests |
| Uncategorized by direction; zero amount counts as incoming; "no rule matched" | Categorizer tests |
| Excluded and income never spending; refund reduces spending | Totals tests |
| User-created category: role, Ostatné, duplicate names refused ignoring case and diacritics | Catalog tests |
| Result names the deciding rule | Categorizer tests |
| Same input, same result; statement unchanged | Determinism test comparing two runs; test that the `Statement` object is equal afterward |
| Empty month | Sample 04 |
| Every default rule alternative tested | Table-driven test with the spec's table as expected values; a second test that every default rule's target exists |

Run everything with `dotnet test` from the repo root.
