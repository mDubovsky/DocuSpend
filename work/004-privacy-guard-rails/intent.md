---
status: planned
issue: 4
---

# Intent: Prove in CI that the app stays private

Issue: [#4](https://github.com/mDubovsky/DocuSpend/issues/4)

## Problem
DocuSpend will handle bank statements, and the project will be public so anyone can inspect it. But most users can't read code, and even people who can won't easily tell whether a download does what the source says, or whether some later change slipped networking in. "Trust us, it's open source" is not proof. Today the repository has one small test that the statement reader uses no networking; nothing covers the rest of the code, the dependencies, or the binaries people will download.

## Desired outcome
The repository itself is the evidence of privacy, and the evidence is automatic: checks that run on every change, that anyone can read, and that fail loudly, with an explanation, when the promise "no code that can see a statement may use the network" ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)) is broken. The repository has no automated build at all today, so this item also creates that pipeline: build and test on every change, with the privacy checks as part of it.

- **Networking can't sneak in as code.** No project in the repository uses networking, and adding networking code to one fails the build. This covers every project, including the developer tools, because the tool that checks real statements can see them too. An exception, such as the later optional online-rules module, is explicit and reviewed, never implicit.
- **Networking can't sneak in as a dependency.** A new package that brings networking, or telemetry, directly or through its own dependencies, fails a check.
- **What ships is checked, not just the source.** Everything built for release is inspected for networking, and the whole read-and-categorize flow also runs in CI with the network switched off, which shows the application is fully usable offline.
- **Standard scanning is in place.** Code scanning, known-vulnerability checks on dependencies, and secret scanning run on every change, with results anyone can see.
- **No real data gets in.** A sample that isn't synthetic, or a file that looks like a real statement, fails a check.
- **A download can be traced to the source.** Release builds are made only by CI, each with a signed record of the commit and workflow it came from, and a list of everything the application includes, so a visitor can verify a download against the repository.
- **The checks explain themselves.** A short privacy document says what the application does and doesn't do and how anyone can check it in a few minutes, and a failing check tells a contributor what rule they hit and how a justified exception would be requested.
- **The checks can become mandatory.** Each privacy check is a separately named check, so any of them can later be marked as required for merging with one setting. It is not turned on while there is a single contributor; once there are real pull requests from others, a failing privacy check blocks merging, and the contributor documentation says how to switch that on.
- **The checks are themselves tested.** Each guard is shown to fail on a deliberate violation, so a check that silently stopped working would be noticed.

## Success criteria
- A change that adds a networking call to any project in the repository fails the build, with a message naming the rule and the decision.
- A change that adds a package that references networking, directly or through its dependencies, fails a check.
- A test inspects every assembly produced for release, with its dependencies, and fails if any references networking.
- CI runs the build, the tests and a full read-and-categorize run on a sample with the network disabled, and it passes.
- Code scanning, dependency vulnerability checks and secret scanning run on every change, and their results are visible in the repository.
- A PDF in `samples/public` without the synthetic marking, or a statement-like file outside `samples/`, fails a check.
- Release builds come only from CI, carry a signed record of their origin, and come with a list of their contents; the privacy document gives the command to verify a download.
- A short privacy document exists, and the contributor documentation explains each check, how an exception is requested, and how to make the checks required for merging.
- Every privacy check has its own name in the pipeline, so it can be selected as required without changing the checks themselves.
- For each guard there is a test that shows it failing on a deliberate violation.
- The repository builds and tests automatically on every change, so a contributor sees the result without running anything.

## Non-goals
- The optional online rules feature (a later issue). This item only makes sure it can be added later as a reviewed exception.
- The user interface, installers and code signing of installers.
- Certifying privacy to any standard, or promising that scanners find every possible problem. Scanners find classes of problems; the guarantees that matter are the ones the build enforces.
- Setting up how contributions are reviewed or merged, such as required reviews and protected branches.

## Decisions
- **Going public has no fixed date.** The repository can be made public whenever it suits. The checks are planned to work in a private repository wherever they can, and the ones that need a public repository (GitHub's free code scanning, the public record of build provenance) are identified in the plan, so they start when the repository goes public. Making it public soon after the first checks are in place is encouraged.
- **A failing privacy check will block merging once there are real pull requests**, but not before; see "The checks can become mandatory".
- **Build and release proof is rehearsed now.** The pipeline can also build downloadable files and attach a signed record of their origin. There is no application yet, only two command-line tools, so the release path is tried on those, to prove it works before the application depends on it. Publishing real releases waits for the application.

## Open questions
None.
