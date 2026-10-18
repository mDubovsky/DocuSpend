# 0003: The interface is in Slovak; code and docs are in English

Status: accepted

## Context
DocuSpend starts with Slovak bank statements and Slovak users. Its first users read Slovak, the statements are in Slovak, and the spending categories they know from their bank's own app are in Slovak. At the same time the project is open source, so contributors may not read Slovak, and agents and tools work best with one working language in the repository.

Some text already exists in English that a user will eventually see: the statement reader's rejection messages ([work/002-statement-reader](../../work/002-statement-reader/spec.md)).

## Decision
- **Everything the user sees is in Slovak:** screens, labels, messages, and the names of the built-in categories and sub-categories.
- **Everything else is in English:** source code and identifiers, documentation, issues, commit messages, tests, and the developer-facing output of tools.
- Slovak texts the product shows are kept as texts of the product, not mixed into documentation.

## Consequences
- **Built-in category names are Slovak data** (Bývanie, Stravovanie, …) and may be corrected or extended later. Anything that stores a choice (a user's rule pointing at a category) must not depend on the display name, so renaming a built-in category doesn't break users' rules. How is settled in the plan of [#3](../../work/003-categorize-transactions/intent.md).
- **The reader's English rejection messages are developer-facing for now.** Before a rejection is shown to a user it must be Slovak. There are two ways: the reader returns the kind of rejection and the place (page, transaction number, which total) and the interface composes the Slovak text, or the reader produces Slovak messages itself. This is not decided here; it belongs to the first item that shows a rejection to a user.
- **Other languages are not planned.** If they are ever added, user-visible texts will need to be collected in one place, which is a reason to keep them out of the logic from the start.
- Documentation that explains Slovak concepts keeps the Slovak term, with an English explanation (for example "VS / KS / SS" in the glossary).
