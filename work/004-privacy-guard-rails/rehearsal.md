# Rehearsal: the privacy checks on GitHub

Plan: [plan.md](plan.md), step 7. Each guard is shown failing on a deliberate violation in the real pipeline, and the release path is tried once. When every row below is recorded, the item is done.

## The item branch

Push `004-privacy-guard-rails`. Every job of the CI workflow must pass: `build-and-test (windows)`, `build-and-test (linux)`, `privacy-no-networking-code`, `privacy-dependencies`, `privacy-no-real-data`, `privacy-no-network-run`, `dependency-vulnerabilities`. While the repository is private, the Code scanning workflow's `code-scanning` job shows as skipped.

| Run | Result |
|---|---|
| CI on `004-privacy-guard-rails` | pending |
| Code scanning on `004-privacy-guard-rails` (skipped while private) | pending |

## Deliberate violations

Each branch is `004-privacy-guard-rails` plus one commit that breaks one rule. It must fail under the check named, with a message naming the rule. Other checks may fail as a consequence (a networking call also breaks `build-and-test`). These branches are never merged; delete them on GitHub after recording.

| Branch | Violation | Must fail | Result |
|---|---|---|---|
| `004-rehearsal-networking-call` | `new HttpClient()` in `DocuSpend.Statements` | `privacy-no-networking-code` (RS0030) | pending |
| `004-rehearsal-denied-package` | `Sentry` in `DocuSpend.Categorization` | `privacy-dependencies` | pending |
| `004-rehearsal-real-data` | a made-up `docs/statement.pdf` | `privacy-no-real-data` | pending |
| `004-rehearsal-vulnerable-package` | `System.Text.Json` 8.0.0 (known high severity) | `dependency-vulnerabilities` | pending |
| `004-rehearsal-no-sandbox` | the canary's connection runs outside the sandbox | `privacy-no-network-run` | pending |

## Release

Run the Release workflow by hand on `004-privacy-guard-rails` (Actions → Release → Run workflow). It must produce one artifact with four archives (`DocuSpend.StatementCheck` and `DocuSpend.SampleGenerator`, for `win-x64` and `linux-x64`) and a `.cdx.json` list of contents for each. While the repository is private, the signed-record step is skipped.

| Run | Result |
|---|---|
| Release, private repository: archives and lists of contents | pending |
| Release, public repository: `gh attestation verify <archive> --repo mDubovsky/DocuSpend` (command from PRIVACY.md) accepts the archive | pending, once public |

## Checked locally, before pushing

- Every command the workflows run, except `unshare` and `curl`, which need the Linux runner: the Release build, the tests with each check's filter, `StatementCheck --verify samples/public`, the strict NuGet audit, `dotnet package list --vulnerable`, and `dotnet publish` with the CycloneDX list for both tools and both platforms.
- The workflows pass actionlint 1.7.12.
- Each guard failed on a deliberate violation in the working tree: a networking call (RS0030), the ban switched off (PRIVACY001), a dropped ban-list overload, a misspelled entry, a bare `#pragma warning disable`, an unlisted `PrivacyException`, a removed or narrowed review, a denied package, a staged PDF outside `samples/public`, and a vulnerable package (NU1903).
