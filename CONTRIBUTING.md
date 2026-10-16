# Contributing

DocuSpend is built with an AI-assisted workflow: every change starts as an idea (a GitHub issue), becomes an intent, then a spec, then a plan, then code. See [docs/sdlc.md](docs/sdlc.md).

1. **Open an issue** describing the problem or idea (use the issue templates). Wait for it to be picked up before starting work.
2. **Branch** from an up-to-date `main`, named `NNN-slug` after the issue (e.g. `042-synthetic-statement-generator`). Never commit directly to `main`.
3. **Write the docs first:** intent, spec and plan in `work/NNN-slug/` (see [work/README.md](work/README.md)). Get them agreed before implementing.
4. **Implement with unit tests**, using synthetic data only.
5. **Open a PR** that says `Closes #NNN`. PRs are squash-merged into `main`.

Small fixes (typos, minor doc edits) can go straight to a branch and PR without an issue.

**Privacy:** never commit real bank statements or other personal financial data. Use synthetic samples in `samples/public/`. `samples/private/` is gitignored for your own local files.
