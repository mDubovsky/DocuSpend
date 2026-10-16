# AGENTS.md

DocuSpend is an open-source tool that turns bank statements (PDF, spreadsheet, …) into charts of when and how you spent money.

## Rules

1. **Never commit without asking the human for approval first.**
2. **Never push. Pushing is for humans only.**
3. Never commit real financial data. Do not read anything in `samples/private/` unless the human explicitly asks, and never reproduce its contents in committed files, tests, or docs. Use synthetic fixtures only.
4. Before working on an item, read its `intent.md`, `spec.md` and `plan.md` in `work/<NNN-slug>/`.
5. Unit tests are required from the start. Every behavior change ships with tests, and tests use synthetic statements from `samples/public/`, never real data.
6. Do not make technology choices outside a `plan.md`. Do not start implementing before the spec and plan are approved.

## Where things are

- `docs/product.md`: what the product is and why (no technical detail)
- `docs/mvp.md`: what the first version includes and excludes
- `docs/glossary.md`: shared terms; use them consistently
- `docs/decisions/`: short records of non-obvious decisions
- `docs/sdlc.md`: the intent → spec → plan workflow
- `work/`: one folder per backlog item; templates in `work/_templates/`
- `.claude/skills/`: project skills (`new-intent`, `write-spec`, `write-plan`)
- `samples/public/`: committed synthetic statements for tests; `samples/private/` is gitignored

## Stack, build and test

TBD. Decided in work items.
