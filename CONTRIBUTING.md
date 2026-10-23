# Contributing

DocuSpend is built with an AI-assisted workflow: every change starts as an idea (a GitHub issue), becomes an intent, then a spec, then a plan, then code. See [docs/sdlc.md](docs/sdlc.md).

1. **Open an issue** describing the problem or idea (use the issue templates). Wait for it to be picked up before starting work.
2. **Branch** from an up-to-date `main`, named `NNN-slug` after the issue (e.g. `042-synthetic-statement-generator`). Never commit directly to `main`.
3. **Write the docs first:** intent, spec and plan in `work/NNN-slug/` (see [work/README.md](work/README.md)). Get them agreed before implementing.
4. **Implement with unit tests**, using synthetic data only.
5. **Open a PR** that says `Closes #NNN`. PRs are squash-merged into `main`.

Small fixes (typos, minor doc edits) can go straight to a branch and PR without an issue.

**Privacy:** never commit real bank statements or other personal financial data. Use synthetic samples in `samples/public/`. `samples/private/` is gitignored for your own local files.

## Repository settings

The workflows in `.github/workflows/` run the checks; a few protections are repository settings instead. Some of them are free only for public repositories, so while the repository is private they are prepared but off: the `code-scanning` job and the dependency-review step are skipped, and release builds come without a signed record of origin.

**Works while private:** `build-and-test`, `privacy-no-networking-code`, `privacy-dependencies`, `privacy-no-real-data`, `privacy-no-network-run`, `dependency-vulnerabilities` (NuGet's audit), release archives with their list of contents, and Dependabot's update proposals (`.github/dependabot.yml`).

**When the repository goes public**, switch these on in Settings:

1. **Actions → General → Workflow permissions:** "Read repository contents and packages permissions" (each job asks for more only where it needs it).
2. **Advanced Security** (Code security): turn on **Dependency graph**, **Dependabot alerts**, **Dependabot security updates**, **Secret scanning** and **Push protection**. Secret scanning is the `secret-scanning` check: it is a setting, not a workflow, and push protection blocks a push that contains a secret.
3. **Code scanning:** keep the advanced setup (`codeql.yml`); don't turn on the default setup, which would replace it. Under **Protection rules**, set **Check runs failure threshold** to "High or higher" for security alerts and "Errors" for other alerts, so a new high or critical finding fails the pull request and lower ones are only reported.
4. Run the **Release** workflow by hand once (Actions → Release → Run workflow) and verify the record with the command in `PRIVACY.md`.

**Making checks required for merging** is not done while there is a single contributor. When it is, add a branch ruleset for `main` (Settings → Rules → Rulesets) with "Require status checks to pass" and select the checks by name: `build-and-test (windows)`, `build-and-test (linux)`, `privacy-no-networking-code`, `privacy-dependencies`, `privacy-no-real-data`, `privacy-no-network-run`, `dependency-vulnerabilities` and, once public, `code-scanning`. The names are an interface; renaming a job is a documented change, and a test (`WorkflowTests`) checks that each name exists.
