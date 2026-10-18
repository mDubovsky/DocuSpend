# AGENTS.md

DocuSpend is an open-source tool that turns bank statements (PDF, spreadsheet, …) into charts of when and how you spent money.

## Rules

1. **Never commit without asking the human for approval first.**
2. **Never push. Pushing is for humans only.**
3. **Never work or commit on `main`.** `main` is for merging PRs only. Work on a branch named after the item (`NNN-slug`, where NNN is the GitHub issue number), created from an up-to-date `main`.
4. Never commit real financial data. Do not read anything in `samples/private/` unless the human explicitly asks, and never reproduce its contents in committed files, tests, or docs. Use synthetic fixtures only.
5. Test samples may copy a bank's statement layout but never its identity (name, legal details, BIC, bank code, logo); see [decision 0002](docs/decisions/0002-fictional-bank-identity-in-samples.md).
6. Before working on an item, read its `intent.md`, `spec.md` and `plan.md` in `work/<NNN-slug>/`.
7. Unit tests are required from the start. Every behavior change ships with tests, and tests use synthetic statements from `samples/public/`, never real data.
8. Do not make technology choices outside a `plan.md`. Do not start implementing before the spec and plan are approved.
9. Everything the user sees is in Slovak; code, docs, issues and commit messages are in English; see [decision 0003](docs/decisions/0003-slovak-interface-english-code.md).

## Where things are

- `docs/product.md`: what the product is and why (no technical detail)
- `docs/mvp.md`: what the first version includes and excludes
- `docs/glossary.md`: shared terms; use them consistently
- `docs/decisions/`: short records of non-obvious decisions
- `docs/sdlc.md`: the intent → spec → plan workflow
- `work/`: one folder per backlog item, named after its GitHub issue; templates in `work/_templates/`
- `.claude/skills/`: project skills (`new-issue`, `new-intent`, `write-spec`, `write-plan`)
- `.github/`: issue and PR templates
- `samples/public/`: committed synthetic statements for tests; `samples/private/` is gitignored

## Stack, build and test

.NET (C#), see [docs/decisions/0001-use-dotnet.md](docs/decisions/0001-use-dotnet.md). The SDK version is pinned in `global.json`. UI and charting technology are still undecided.

Run from the repo root:

- Build: `dotnet build`
- Test: `dotnet test`
- Regenerate test samples: `dotnet run --project tools/DocuSpend.SampleGenerator -- generate` (see [samples/README.md](samples/README.md); never edit samples by hand)
- Check the reader on real statements (maintainer, locally, only with the human's go-ahead): `dotnet run --project tools/DocuSpend.StatementCheck` (reads `samples/private/`; prints counts or a rejection, never content)

Layout: `src/` for application code (`DocuSpend.Statements`: reading statements into transactions), `tools/` for developer tooling, `tests/` for test projects.
