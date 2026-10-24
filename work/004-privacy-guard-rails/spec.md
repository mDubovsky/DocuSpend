# Spec: Prove in CI that the app stays private

Intent: [intent.md](intent.md)

## Behavior

Three kinds of people meet these checks. A **contributor** changes the code and sees a check fail, or pass. A **visitor** to the public repository wants to be convinced the application is private. A **maintainer** reviews exceptions and decides when checks become mandatory. This spec describes what each of them sees and can do.

The rule being enforced is [decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md): **no code that can see a statement may use the network**, and no telemetry, analytics, crash reporting or auto-update exists in the application.

### What counts as networking

One list, kept in one place in the repository (the `privacy/` folder), says what counts. It covers the .NET types and namespaces for HTTP, sockets, web sockets, mail, DNS, pinging and secure streams; calls to the operating system's networking libraries; and starting another program, since a program can be started to send data. Anything not obviously harmless is on the list. Adding to it is a normal change. Removing from it is a change to decision 0006.

### The checks

Every check has a stable name, so it can later be marked as required for merging.

| Name | What it checks | Applies to |
|---|---|---|
| `build-and-test` | the whole repository builds and every test passes, on every change | all |
| `privacy-no-networking-code` | no networking in our own code | every project: application, tools and tests |
| `privacy-dependencies` | no networking or telemetry arriving through a package | everything that ships |
| `privacy-no-network-run` | the full read-and-categorize flow works with the network switched off | the committed samples |
| `privacy-no-real-data` | no real statements or look-alikes are committed | all tracked files |
| `code-scanning` | standard code scanning of the C# code | all code |
| `dependency-vulnerabilities` | known vulnerabilities in packages, and updates offered | all packages |
| `secret-scanning` | no tokens or other secrets in what is committed | all tracked files |
| `release-provenance` | release builds come from CI with a signed record of origin and a list of contents | release builds |

#### privacy-no-networking-code
- A change that uses anything on the networking list in any project's own code fails the build. The message names the rule, the offending location, decision 0006, and how to request an exception.
- This includes declaring calls to the operating system's networking libraries from our own code, and starting other programs.
- An **exception** is an entry in one file in `privacy/`, naming the project, the reason, and the decision that allows it. Today the file has no entries. A project with an entry is held to the extra limits of decision 0006: a test fails if it references the statement or transaction types, because the future online-rules module must never be able to see a statement.
- The ban covers `.cs` code. The CI workflow files themselves must use the network to build and aren't covered.

#### privacy-dependencies
- Everything that ships is inspected: each assembly produced for release and every assembly it depends on, however indirectly. A reference to networking in any of them fails the check, unless that assembly is on a **reviewed allowlist** in `privacy/`.
- An allowlist entry names the assembly and version range, says why it is acceptable (for example "the toolkit references HTTP types for loading images from web addresses, which the application never does"), and says who reviewed it. New entries are visible in the change and need that review.
- Known telemetry, analytics and crash-reporting packages are **denied by name**, wherever they appear in the dependency tree, even if they don't reference networking directly. That list also lives in `privacy/`.
- Test frameworks and other things that don't ship are out of scope for the dependency check, though our own test code is still under the ban.
- Compiled native libraries cannot be inspected this way. The check lists them, so they are visible and reviewed, but can't prove what they do. The privacy document says so.

#### privacy-no-network-run
- After everything is built and restored, the job removes network access and then runs one command from the repository: the read-and-categorize flow on the committed samples. The command succeeds only if samples 01 to 05 read and categorize completely, with totals that add up, and samples 06 to 09 are rejected.
- In the same job, a **canary**, a tiny program that tries to open a connection, must fail to connect. If it connects, the sandbox isn't working and the job fails.

#### privacy-no-real-data
- No PDF, spreadsheet or CSV file is tracked anywhere except under `samples/public`, and nothing under `samples/private` is tracked.
- Every file under `samples/public` is one the generator produces, byte for byte, which is already tested. Compressed streams in a PDF are compared decompressed and with uniform line endings, since the compressor's output and the line endings PDFsharp writes differ between platforms. A PDF without the synthetic marking, or an extra file, fails.
- The check explains why, and how to use `samples/private` for real statements.

#### code-scanning, dependency-vulnerabilities, secret-scanning
- Code scanning runs on every change and on a weekly schedule. New findings of high or critical severity fail the change; lower ones are reported without failing.
- Packages with a known vulnerability of moderate severity or higher fail the build, including packages brought in indirectly. Updates are proposed automatically, and each proposal goes through the same checks.
- Secret scanning looks for tokens and keys in what is committed, and blocks them on push where the platform supports it.
- These three need the repository to be public (or a paid plan for a private one). Until then the workflow files and settings are prepared but inactive, and the other checks stand alone.

#### release-provenance
- Release builds are produced only by CI, never on a developer's machine. Each build of the command-line tools is a downloadable archive that comes with: a signed record of the commit and workflow that produced it, and a list of everything it contains.
- The record is public and can be checked with one command, which the privacy document gives.
- This item rehearses the path on the two command-line tools. Real releases of the application come later. Public provenance records exist for builds made while the repository is public; until then the workflow is ready and a build still produces the archive and the list of contents.

### A checks pipeline that explains itself
- **Every failure says what rule was hit, where, why it matters, and what to do**, including how a justified exception would be requested.
- **Checks that can't run count as failed**, never as passed. A scanner that is unreachable doesn't quietly turn green.
- **The same checks run locally.** A contributor can run `dotnet build` and `dotnet test` and get the networking ban, the dependency check and the real-data check. Only the network-off run and the online scanners need CI, and the contributor documentation says how to run the rest.
- **They are mandatory-capable.** Each check has its own name from the table above, listed in the contributor documentation with how to mark any of them as required for merging. They aren't required while there is a single contributor.

### The privacy document
A short `PRIVACY.md` at the root, written for a person who doesn't write code. It says:
- what the application does with your statement: reads the file you choose, in your computer's memory, and nothing else; no copy is sent anywhere or saved by the application;
- what it never does: no network connection, no analytics, no crash reports, no automatic updates, no accounts;
- how that is enforced: the checks above, with links to where they run;
- how to check it yourself in a few minutes: block the application in the firewall and use it normally; look at what network connections it has while running; verify a downloaded file against its signed record with the command;
- **what the checks do not cover**, honestly: compiled native components, the operating system itself, and files you open from a network location;
- that a later, optional online feature for sharing rules would be a separate, small, off-by-default part that never sees statements, and is added only by changing decision 0006.

### Contributor documentation
`CONTRIBUTING.md` gets a section on the privacy checks: what each does, what a failure means, how to request an exception, and how to make a check required.

## Acceptance criteria

- [ ] A deliberate use of an item on the networking list, in each kind of project (application library, tool, test), fails the build, and the message names the rule, the location, decision 0006 and how to request an exception.
- [ ] Declaring a call into an operating-system networking library from our own code, and starting another program, each fail the build the same way.
- [ ] A project with an entry in the exceptions file that references the statement or transaction types fails a test; with no entries the test passes.
- [ ] The dependency check fails for an assembly that references networking, directly or through its own dependencies, when it is not on the allowlist, and passes when it is, and it names the assembly and the path that led to it.
- [ ] A denied telemetry or analytics package fails the dependency check wherever it appears in the tree.
- [ ] Native libraries in release output are listed by the dependency check.
- [ ] The current repository passes every check without any exceptions in the file for our own code.
- [ ] `privacy-no-network-run` runs after dependencies are restored with the network removed and passes on the committed samples; the canary fails to connect there, and a pipeline run where the canary does connect fails.
- [ ] A tracked PDF, spreadsheet or CSV outside `samples/public`, a tracked path under `samples/private`, and a PDF in `samples/public` that the generator doesn't produce each fail `privacy-no-real-data`.
- [ ] Code scanning, dependency vulnerability checks and secret scanning are configured, and inactive-but-ready while the repository is private, with the contributor documentation saying which are which.
- [ ] A package with a known vulnerability of moderate severity or higher, including an indirect one, fails the build.
- [ ] Release builds are made only by the release workflow; the rehearsal on the two tools produces archives, a list of contents for each, and (when the repository is public) a signed record that the verification command accepts.
- [ ] Every check in the table appears under its own stable name in the pipeline.
- [ ] A check that cannot run (an unreachable scanner, a missing file) is reported as failed.
- [ ] Every guard is shown failing on a deliberate violation in an automated test or a recorded rehearsal: the build ban, the dependency check, the exception limits, the real-data check, and the network-off canary.
- [ ] `PRIVACY.md` exists with each section above, including what is not covered, and its verification commands work as written.
- [ ] `CONTRIBUTING.md` explains each check, how to request an exception, and how to mark checks required.
- [ ] Running `dotnet build` and `dotnet test` locally runs the networking ban, the dependency check and the real-data check.

## Edge cases and errors

- **A legitimate need appears.** A contributor requests an exception in the file with a reason and the decision; it is reviewed like any change and, for networking in our own code, needs decision 0006 to be amended.
- **A third-party toolkit references networking types but never calls them.** It goes on the allowlist with a reason, not into the exceptions for our own code, and a reviewer sees it.
- **A dependency update brings networking.** The automatic update goes through the same checks and fails the dependency check until reviewed.
- **The no-network run can't disable the network** (a runner without the capability). The job fails; it doesn't skip.
- **The repository is private.** Checks that need a public repository are inactive and say so, without failing the pipeline.
- **A check is renamed.** The names in the table are part of the interface to the "required" setting. Renaming one is a documented change.
- **Platform differences.** The tests and checks run on the platform where each can be done reliably; the pipeline documents which.

## What these checks cannot do

They don't certify privacy to any standard. They don't prove what compiled native components do, what the operating system does on its own, or what happens when a user opens a file from a network location. They find classes of problems, and the strongest guarantees are the ones the build enforces. `PRIVACY.md` says this plainly.

## Out of scope

- The optional online-rules module and its server.
- The user interface, installers, and code signing of installers.
- How contributions are reviewed and merged: required reviews, protected branches, and turning the checks on as required.
- Real releases of the application.
- A Slovak translation of `PRIVACY.md`. It is required before the first real release (see Decisions), but not part of this item.

## Decisions

- **`PRIVACY.md` is written in English now, and a Slovak version must exist before the first real release.** Its readers are the product's users, who read Slovak ([decision 0003](../../docs/decisions/0003-slovak-interface-english-code.md)); the release checklist will include it.

## Open questions

None.
