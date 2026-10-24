# Rehearsal: the privacy checks on GitHub

Plan: [plan.md](plan.md), step 7. Each guard is shown failing on a deliberate violation in the real pipeline, and the release path is tried once. Recorded on 2026-10-24; runs are on [the Actions page](https://github.com/mDubovsky/DocuSpend/actions).

## The item branch

The first two runs failed on Linux in `build-and-test (linux)` and `privacy-no-real-data`: the committed sample PDFs differed from the generator's output there, first in how streams were compressed, then in line endings PDFsharp writes in a font's character map. Both are platform differences, not content; the comparison now allows for them (see the plan's risks). The third run passed.

| Run | Result |
|---|---|
| CI on `004-privacy-guard-rails` at `c6880f6` ([run 39947207587](https://github.com/mDubovsky/DocuSpend/actions/runs/39947207587)) | Passed: all seven jobs, including `privacy-no-network-run` (sandbox created, positive control connected, canary could not connect, samples verified with the network removed) |
| Code scanning on `004-privacy-guard-rails` ([run 39947207665](https://github.com/mDubovsky/DocuSpend/actions/runs/39947207665)) | `code-scanning` skipped, as it must be while the repository is private |

## Deliberate violations

Each branch is `004-privacy-guard-rails` plus one commit that breaks one rule. Other checks failing as a consequence is expected: a build error fails every job that builds, and `build-and-test` runs all tests.

| Branch | Violation | Must fail | Result |
|---|---|---|---|
| `004-rehearsal-networking-call` ([run 40104614849](https://github.com/mDubovsky/DocuSpend/actions/runs/40104614849)) | `new HttpClient()` in `DocuSpend.Statements` | `privacy-no-networking-code` | Failed: `Rehearsal.cs(6,42): error RS0030: The symbol 'System.Net.Http' is banned in this project: privacy-no-networking-code: HTTP is not allowed in DocuSpend code (decision 0006, …)`. Every job that builds failed with it; `dependency-vulnerabilities` (restore only) passed. |
| `004-rehearsal-denied-package` ([run 40104615071](https://github.com/mDubovsky/DocuSpend/actions/runs/40104615071)) | `Sentry` in `DocuSpend.Categorization` | `privacy-dependencies` | Failed: `DocuSpend.Categorization depends on Sentry 5.0.0, which privacy/denied-packages.txt denies ('Sentry*')`, and `ships Sentry 5.0.0.0 (Sentry.dll) uses networking: System.Net.Http…`; both again for `DocuSpend.StatementCheck`, which gets it through the library. Also `build-and-test`; every other check passed. |
| `004-rehearsal-real-data` ([run 40104614948](https://github.com/mDubovsky/DocuSpend/actions/runs/40104614948)) | a made-up `docs/statement.pdf` | `privacy-no-real-data` | Failed: `privacy-no-real-data: docs/statement.pdf looks like a statement and is outside samples/public. …` Also `build-and-test`; every other check passed. |
| `004-rehearsal-vulnerable-package` ([run 40104614951](https://github.com/mDubovsky/DocuSpend/actions/runs/40104614951)) | `System.Text.Json` 8.0.0 (known high severity) | `dependency-vulnerabilities` | Failed at restore: `error NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability` (twice, one per advisory), next to `NU1510` (the package is part of .NET 10 and needn't be referenced). The audit makes the build fail too, so every job failed. |
| `004-rehearsal-no-sandbox` ([run 40104614809](https://github.com/mDubovsky/DocuSpend/actions/runs/40104614809)) | the canary's connection runs outside the sandbox | `privacy-no-network-run` | Failed: `privacy-no-network-run: the canary connected from inside the sandbox, so the sandbox doesn't remove the network and this run proves nothing.` Only this job failed. |

## Release

| Run | Result |
|---|---|
| Release, private repository, from tag `v0.0.0-rehearsal.1` at `c6880f6` ([run 39955895925](https://github.com/mDubovsky/DocuSpend/actions/runs/39955895925)) | Passed. Build and all tests ran first; "Signed record of origin" was skipped (private). One artifact with four archives (`DocuSpend.StatementCheck` and `DocuSpend.SampleGenerator`, `win-x64` and `linux-x64`) and a `.cdx.json` list of contents for each. The downloaded `DocuSpend.StatementCheck-win-x64` ran `--verify samples/public` locally: all 9 samples agree; its list of contents names `PdfPig 0.1.16`. |
| Release, public repository: `gh attestation verify <archive> --repo mDubovsky/DocuSpend` (command from PRIVACY.md) accepts the archive | Open until the repository is public; see the settings checklist in CONTRIBUTING.md. |

The Release workflow can be started by hand only once `release.yml` is on `main`; until then a `v*` tag runs it, as here.

## Checked locally, before pushing

- Every command the workflows run, except `unshare` and `curl`, which need the Linux runner: the Release build, the tests with each check's filter, `StatementCheck --verify samples/public`, the strict NuGet audit, `dotnet package list --vulnerable`, and `dotnet publish` with the CycloneDX list for both tools and both platforms.
- The workflows pass actionlint 1.7.12.
- Each guard failed on a deliberate violation in the working tree: a networking call (RS0030), the ban switched off (PRIVACY001), a dropped ban-list overload, a misspelled entry, a bare `#pragma warning disable`, an unlisted `PrivacyException`, a removed or narrowed review, a denied package, a staged PDF outside `samples/public`, and a vulnerable package (NU1903).

## Afterwards

Delete the rehearsal branches and the `v0.0.0-rehearsal.1` tag on GitHub; they are never merged.
