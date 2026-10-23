# Plan: Prove in CI that the app stays private

Spec: [spec.md](spec.md)

## Approach

Three layers, each cheap to run and each tested by a deliberate violation:

1. **The compiler refuses networking in our own code**, using a ban list and an analyzer.
2. **A dependency inspector** reads the metadata of everything that ships and fails on networking that wasn't reviewed.
3. **A GitHub Actions pipeline** runs all of it on every change under stable check names, adds the network-off run, the scanners and release provenance, and stays inactive where a feature needs a public repository.

The privacy rules live in one folder, `privacy/`, so a contributor, reviewer or visitor finds them in one place.

### Verified before planning
A throwaway experiment on .NET 10 with `Microsoft.CodeAnalysis.BannedApiAnalyzers` (5.6.0) showed that a ban list stops, at build time: a whole namespace (`N:System.Net.Http`), a type (`T:System.Net.WebClient`), `Process`, `DllImportAttribute`, and `Type.GetType(string)`; and that it leaves harmless `System.Net.IPAddress` alone. Two things to build in: the list must be an additional file named **exactly `BannedSymbols.txt`** (any other name silently disables the analyzer, so a test guards it), and violations are warnings by default (the repository already treats warnings as errors).

### 1. The ban on networking in our own code
- `privacy/BannedSymbols.txt` is the single list of what counts as networking (namespaces and types for HTTP, sockets, web sockets, mail, DNS, ping, secure streams; `Process`; `DllImportAttribute`, `LibraryImportAttribute`, `NativeLibrary`; and loading types or assemblies by name, which would be a way around the rest). Each entry carries the message that is shown: the rule, decision 0006, and how to request an exception.
- `Directory.Build.props` adds the analyzer to every project, tests included, and points it at that file, unless the project sets `PrivacyException` (below). With warnings as errors, a violation fails the build.
- **Exceptions** are a project that sets `<PrivacyException>true</PrivacyException>` and has a matching entry in `privacy/exceptions.json` (project, reason, decision). A test requires the two to agree exactly, and requires that such a project cannot reach the statement or categorization projects, directly or through other projects. The file is empty today.
- **Ways around the ban are tested for:** a source scan fails on any `.cs` suppression of the analyzer (`#pragma warning disable`, `SuppressMessage`), on `.editorconfig` or project settings that lower or remove it, and on a missing or misnamed ban file.

### 2. The dependency inspector (`privacy-dependencies`)
A new test project, `tests/DocuSpend.Privacy.Tests`, holds all privacy tests that run with `dotnet test`. Its inspector reads assemblies with `System.Reflection.Metadata`, so nothing is loaded or run and no extra package is needed.
- **What it scans:** the build output of every project that ships (everything under `src/`, and the two command-line tools). A dependency's own dependencies are in that output, so indirect ones are covered.
- **What it looks for, in every managed assembly:** type references into the networking namespaces and types of the ban list (the `System.Net` entries; reflection and `Process` are only banned in our own code, because toolkits legitimately use reflection), and native imports of networking libraries and functions (`ws2_32`, `wininet`, `winhttp`, `libcurl`, `socket`, `connect`, `getaddrinfo`, …, listed in `privacy/native-networking.txt`). Looking at type references rather than assembly references avoids false alarms from a mere reference and also catches `netstandard` libraries.
- **Findings** name the assembly, the type or import, and the path through the dependency graph from our assembly to it. A finding passes only if `privacy/allowed-dependencies.json` has an entry for that assembly and version range with a reason and a reviewer. The list is empty today; the user-interface item will add its first entries.
- **Denied packages:** `privacy/denied-packages.txt` lists telemetry, analytics and crash-reporting packages by name pattern (for example Application Insights, Sentry, App Center, Segment, Mixpanel, Amplitude, OpenTelemetry exporters). The inspector reads each project's `obj/project.assets.json`, so indirect packages are covered.
- **Native libraries** (not managed code, so not inspectable) are found by file, listed, and checked against `privacy/native-libraries.json`; a new one fails until reviewed. None exist today.
- The old `NoNetworkTests` in the statements tests (which loads assemblies by name, now banned) is replaced by this.

### 3. The real-data check (`privacy-no-real-data`)
- **A pure policy function** takes a list of tracked paths and applies the rule: no PDF, spreadsheet, CSV or bank-export formats (xls, xlsx, ods, csv, ofx, qif, mt940, camt) except under `samples/public`, nothing under `samples/private`. It is unit-tested on made-up lists.
- **The tracked paths** come from the repository's index, read with LibGit2Sharp, a library that reads git data without starting a process (the ban forbids `Process`, so running `git` from a test is out). It is used only in the test project, which doesn't ship; a note in `privacy/README.md` says why.
- The exact-set-of-samples and synthetic-marking tests that already exist stay, and run under the same check name.

### 4. The network-off run (`privacy-no-network-run`)
- **A `--verify` mode for the existing `StatementCheck` tool.** Given a directory of samples, it reads each PDF and compares the outcome with the sample's own `.expected.json`: files expected to parse must read, categorize with totals that add up, and have the expected transaction count; files expected to be rejected must be rejected. It exits with success only if all agree. It needs no new package.
- **The job** runs on a Linux runner: restore and build with the network, then run the tool inside a network namespace with no network (`unshare --net`), with .NET's own telemetry switched off. The canary is **shell, not C#**, because a C# canary would itself be banned: a positive control first (a connection works outside the sandbox, so the canary can't pass for an unrelated reason), then the negative one (the same connection inside the sandbox must fail). If the runner can't create the sandbox, the job fails.

### 5. The pipeline
GitHub Actions, as [decision 0005](../../docs/decisions/0005-avalonia-for-the-user-interface.md) assumes. Third-party actions are pinned to full commit identifiers, each job gets the least permissions it needs, and nothing needs a secret.
- `ci.yml`, on every push and pull request: `build-and-test` on Linux and Windows (Windows is the main platform; Linux is where the sandbox works); `privacy-no-networking-code` (a Release build with the analyzer, so a violation shows under its own name); `privacy-dependencies`, `privacy-no-real-data` (each runs the privacy tests with its own trait filter, so each is a separate status); `privacy-no-network-run`; `dependency-vulnerabilities` (restore with the NuGet audit at moderate severity and above, including indirect packages, and fail on any finding).
- `codeql.yml` (code scanning, C#, on every change and weekly) and a dependency-review step are guarded by `github.event.repository.private == false`, so while the repository is private they show as skipped rather than failing. Secret scanning and push protection are repository settings, not workflows; the contributor documentation lists exactly which to switch on when the repository goes public.
- `.github/dependabot.yml` proposes updates for NuGet packages and for the actions, which then pass through the same checks. Dependabot works in a private repository.
- `release.yml`, on a version tag and on demand: publishes the two tools for Windows and Linux, zips them, creates a list of contents (a CycloneDX software bill of materials) and, when the repository is public, a build-provenance record with GitHub's attestation action. While private it stops after the archive and the list. Run on demand, it only keeps the files as workflow artifacts; publishing real releases waits for the application.
- **Checks that can't run fail**: no step uses `continue-on-error`, and a test fails when a required input (the ban file, a list, the project assets) is missing.

### 6. Documentation
`PRIVACY.md` (English; a Slovak version before the first release, per the spec), a privacy section in `CONTRIBUTING.md` (what each check does, how to request an exception, how to run them locally, how to mark them required), a link from `README.md`, a `privacy/README.md`, and updated rules in `AGENTS.md`. The verification commands in `PRIVACY.md` are tried as written.

### Alternatives considered
- **A custom Roslyn analyzer instead of the package:** more code to maintain for nothing the package can't already do; reconsidered only if a real gap appears.
- **`dotnet list package` or the assets file alone for dependencies:** these show packages, not what the code in them does, so the metadata scan is needed anyway; the assets file is still used for denied packages.
- **Running `git` from a test:** needs `Process`, which the ban forbids for tests too; an exception for tests would weaken "every project".
- **A container with `--network none` for the sandbox:** works, but `unshare` is simpler on the runners.
- **Making the privacy tests an MSBuild step instead of tests:** they would then run on every build, slowing it for no gain; as tests they run with `dotnet test` and in CI.

### Layout
```
privacy/                  BannedSymbols.txt, native-networking.txt, denied-packages.txt,
                          allowed-dependencies.json, native-libraries.json, exceptions.json, README.md
tests/DocuSpend.Privacy.Tests/   inspector, policy checks, guard tests
.github/workflows/        ci.yml, codeql.yml, release.yml;  .github/dependabot.yml
PRIVACY.md                at the root
```

## Prerequisites
- Issue #3 merged (the flow the network-off run exercises). ✔
- The repository on GitHub with Actions available (it is private today; the free allowance is enough for this pipeline).
- The user pushes branches and merges; pipeline behavior can only be observed after a push.

## Steps
Test-first throughout; one commit per step group, each after the human's approval.
1. `privacy/` folder with the ban list, the analyzer wiring in `Directory.Build.props`, and the exceptions mechanism. Confirm the current repository builds clean. Tests: the ban catches each kind of violation (checked by compiling small snippets with the real analyzer and ban list inside a test), the file-name guard, suppression scan, exceptions agreement and limits.
2. `DocuSpend.Privacy.Tests`: the metadata inspector, allowlist, denied packages, native-library listing, built and tested on small assemblies compiled in the test (one using networking, one clean, one importing a networking library), then run on the real build outputs. Replace the old no-network test.
3. The real-data policy function and its tests, then the LibGit2Sharp reader and the check on the real repository.
4. `StatementCheck --verify` and its tests.
5. The workflows, `dependabot.yml`, and the contributor-facing instructions for repository settings. A local dry run of each command the workflows use.
6. Documentation: `PRIVACY.md`, `CONTRIBUTING.md`, `README.md`, `privacy/README.md`, `AGENTS.md`.
7. Rehearsals, after the user pushes: a branch with a deliberate networking call, one with a denied package, one with a PDF outside `samples/public`, and one with the canary's sandbox removed. Each must fail under the right check name. A manual release run must produce the archives and the list (and the attestation once public, verified with the command from `PRIVACY.md`). Recorded in `work/004-privacy-guard-rails/rehearsal.md`; marks the item done.

## Risks and unknowns
- **Toolkits will reference networking types.** The user-interface toolkit very likely will. Mitigated by the allowlist, which exists so a reviewer sees it; the first entries come with the UI item, and an allowlisted assembly is still reported in the check output.
- **The analyzer sees source, not behavior.** Loading by string, native code and the operating system aren't covered; `Type.GetType(string)`, `Assembly.Load*` and native-library loading are banned to close the obvious doors, and `PRIVACY.md` states the rest as limits.
- **Tests that pass on Windows may not on Linux.** The sample-bytes comparison depends on the PDF library giving identical bytes everywhere; if it doesn't, the comparison falls back to text and records on that platform and the plan records why.
- **LibGit2Sharp brings native binaries** and sometimes trips over repository-ownership checks on CI; if it proves fragile, reading the index file directly is the fallback.
- **Features that need a public repository** can't be exercised until it is public. They are built and kept inactive, so the first real run happens when you flip the repository; the plan lists them so that day has a checklist.
- **Pipeline supply chain.** Actions are pinned to commits and updated by Dependabot, with minimal permissions. This still trusts GitHub and the actions' authors, which is inherent.
- **Check names are an interface.** Renaming one silently drops it from the "required" list; the names are listed in `CONTRIBUTING.md` and a test compares them with the workflow files.
- **`unshare` needs elevated rights** on the runner, which the standard Linux runners allow.

## Verification

| Spec criterion | How it is checked |
|---|---|
| A use of the networking list fails the build, in each kind of project, with the rule message | Compile snippets with the real analyzer and ban list: application-style, tool-style and test-style projects each produce the error and the message names decision 0006 |
| OS networking libraries and starting programs fail the same way | Snippets using `DllImport` of `ws2_32`, `LibraryImport`, `Process.Start` |
| A project with an exception cannot reach statement types; no entries pass | Fixture project files; the exceptions-agreement and reachability tests |
| A dependency using networking fails unless allowlisted, with the path named | Assemblies compiled in the test: clean passes, networking fails with its path, allowlisted passes and is still reported |
| Denied packages fail wherever they appear | Fixture `project.assets.json` with a denied indirect package |
| Native libraries are listed, a new one fails | Fixture output directory with a native file |
| The current repository passes everything with no exceptions for our own code | The checks run on the real build outputs and the real repository |
| Network-off run passes; the canary fails to connect; a connecting canary fails the job | The workflow itself: positive control, negative canary, and the `--verify` run; rehearsal removes the sandbox |
| Real-data rules | Policy function tests on made-up path lists, plus the check on the real repository |
| Scanning configured, inactive while private | Workflow files with the visibility guard; the contributor docs list the settings; the first public run is on the checklist |
| A vulnerable package, direct or indirect, fails the build | Rehearsal: a branch referencing a package with a known vulnerability |
| Release builds only from the release workflow, with archives, a list of contents and provenance | Manual run of the release workflow; verification of the record with the documented command once public |
| Every check under its own stable name | A test comparing the names in the table with the workflow files |
| A check that can't run counts as failed | No `continue-on-error`; tests fail on missing inputs |
| Every guard shown failing on a violation | The guard tests above plus the recorded rehearsals |
| `PRIVACY.md` and `CONTRIBUTING.md` complete; commands work as written | Review plus each documented command tried |
| Local `dotnet build` and `dotnet test` run the ban, the inspector and the real-data check | The privacy tests are part of the solution; run locally |

Run everything with `dotnet build` and `dotnet test` from the repo root.
