# Privacy rules

DocuSpend's rule is [decision 0006](../docs/decisions/0006-privacy-approach-and-network-boundary.md): **no code that can see a statement may use the network**. The files in this folder are what the build and the tests enforce. Changing them is a normal, reviewed change. Weakening them is a change to decision 0006.

For an overview of all the checks, including the network-off run and the scanners, see [CONTRIBUTING.md](../CONTRIBUTING.md#privacy-checks); for users, see [PRIVACY.md](../PRIVACY.md).

| File | What it is |
|---|---|
| [BannedSymbols.txt](BannedSymbols.txt) | What counts as networking in our own code: HTTP, sockets, web sockets, mail, DNS, ping, secure streams and pipes; starting other programs; calling operating-system libraries or COM directly; and loading types or assemblies by name, which would get around the rest. |
| [exceptions.json](exceptions.json) | Projects allowed to use networking. Empty. |
| [native-networking.txt](native-networking.txt) | Native networking libraries and functions that a shipped assembly must not import. |
| [denied-packages.txt](denied-packages.txt) | Telemetry, analytics, crash-reporting, remote-logging and auto-update packages, denied by name. |
| [allowed-dependencies.json](allowed-dependencies.json) | Third-party assemblies whose networking references were reviewed and accepted, with the reason and the reviewer. |
| [native-libraries.json](native-libraries.json) | Native files in what ships, reviewed: the graphics and text libraries the desktop application draws with. |

## The ban on networking in our own code (`privacy-no-networking-code`)

Every project in the repository, including the developer tools and the tests, is compiled with the [banned-API analyzer](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers) and `BannedSymbols.txt` (wired up in [Directory.Build.targets](../Directory.Build.targets)). A use of anything on the list fails the build with a message like:

```
error RS0030: The symbol 'HttpClient' is banned in this project: privacy-no-networking-code: HTTP is not allowed in DocuSpend code (decision 0006, ...). To request an exception, see privacy/README.md.
```

The build also fails if the ban is switched off: if the analyzer or the list is missing from a project, or analyzers are disabled. The tests in `tests/DocuSpend.Privacy.Tests` fail if the warning is suppressed in code (`#pragma warning disable`, `SuppressMessage`) or lowered in an `.editorconfig`, `.globalconfig` or project file.

If you hit the ban, the usual answer is to do the work without the network: DocuSpend reads files the user chooses, and nothing else.

## Networking in what ships (`privacy-dependencies`)

Our own code is covered by the ban; the packages we use are not, so the tests in `tests/DocuSpend.Privacy.Tests` inspect every project that ships (everything under `src/` and `tools/`) in its build output. Every assembly there is read with `System.Reflection.Metadata`, so nothing is loaded or run. The check fails when:

- an assembly references, defines or forwards a type in a networking namespace or type of `BannedSymbols.txt` (the `System.Net` entries; reflection and `Process` are banned only in our own code, because libraries use them legitimately), or imports something from `native-networking.txt`, unless `allowed-dependencies.json` covers that assembly and version. The failure names the assembly, what it uses, and the path of references from our assembly to it;
- a package matching `denied-packages.txt` is anywhere in a project's dependency tree, read from its `obj/project.assets.json`, even when it doesn't reference networking itself;
- a native file (anything that holds code but isn't a managed assembly) is in an output and not in `native-libraries.json`;
- a file a package provides is missing from an output, which would make the inspection incomplete. Every project copies its packages to its output for this (`CopyLocalLockFileAssemblies` in `Directory.Build.props`). A project built for one platform (the desktop application is built for `win-x64`) is expected to hold only that platform's files;
- a review in `allowed-dependencies.json` or `native-libraries.json` matches nothing any more, so it can't quietly allow the same thing back later;
- a project resolves Avalonia's build telemetry with its build step included (see below).

Reviewed findings still pass with a line in the test output, so they stay visible. Test frameworks and other things that don't ship are out of scope; our test code is still under the ban.

**Keeping a package file out of what ships.** When a package brings a file with networking that the application doesn't need at all, the better answer than a review is not to ship it. The project declares `<PrivacyRemovedFromOutput Include="file.dll" />`; `Directory.Build.targets` then leaves it out of the output and the dependency manifest, and the check requires it to be absent instead of reporting it missing, and fails if no package provides it any more. The desktop application does this for `Avalonia.DesignerSupport.dll`, Avalonia's hook for the IDE's design-time previewer, which uses sockets; the cost is that the live XAML preview doesn't work.

**Networking a review relies on our code never reaching** is added to `BannedSymbols.txt` too, so that "never reached" is enforced rather than claimed: Avalonia's previewer protocol (`Avalonia.Remote.Protocol`) is reviewed in `allowed-dependencies.json` and banned in our code.

### Avalonia's build telemetry is off

The `Avalonia` package always brings `Avalonia.BuildServices`, whose build step sends anonymous usage data to the Avalonia team on every build: hashed project and machine names, a machine identifier, versions, the operating system and whether it runs in CI. It never ships in the application, but it would send data from every contributor's machine and from CI, so it is switched off for everyone, in three layers:

- `Directory.Build.targets` references it in every project with all its assets excluded, so its build step is never imported, and defines an empty `AvaloniaStats` target in case it gets in some other way;
- the workflows set `AVALONIA_TELEMETRY_OPTOUT=1`, Avalonia's own opt-out;
- a test under `privacy-dependencies` reads every restored project's `obj/project.assets.json` and fails if any resolves the package with its build files.

Don't remove any of these. Setting `AVALONIA_TELEMETRY_OPTOUT=1` in your own environment as well does no harm.

**Asking for a review.** If a dependency references networking it never uses (for example a toolkit that can load images from web addresses, which DocuSpend never does), add an entry to `allowed-dependencies.json` in the same pull request:

```json
{ "assembly": "PdfSharp.Quality", "versions": "[6.2.4, 6.3)",
  "reason": "Why the networking is never reached.", "reviewer": "Who reviewed it" }
```

`versions` is a range of assembly versions: `[a, b)` includes `a` and excludes `b`; a bare version means exactly that one. Keep the range narrow, so that an update is reviewed again. Native files work the same way in `native-libraries.json`, with the file's path in the output, a reason and a reviewer. The check can list native code but can't prove what it does.

## No real data (`privacy-no-real-data`)

Real statements must never be committed. The check fails when:

- a statement-like file (PDF, spreadsheet, CSV, or a bank-export format such as OFX, QIF, MT940 or CAMT) is tracked anywhere except `samples/public`;
- anything at all under `samples/private` is tracked, even when forced past `.gitignore`;
- a file in `samples/public` isn't one the sample generator produces, byte for byte (the existing `CommittedSamplesTests`, which run under the same check name). PDFs are compared with their compressed streams decompressed and line endings made uniform, because the compressor's output and the line endings PDFsharp writes differ between platforms; everything else must match exactly.

The tracked files are read from git's index, so a file that is staged but not yet committed is caught too. The tests read the index with [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) because running `git` would mean starting a program, which the networking ban forbids in tests as well. LibGit2Sharp brings native binaries; it is used only by the test project, which doesn't ship.

Real statements belong in `samples/private`, which git ignores. Tests use the synthetic samples in `samples/public`; see [samples/README.md](../samples/README.md).

## Requesting an exception

An exception exists for one reason: the optional online-rules module that decision 0006 foresees, which must never see a statement. To request one:

1. Change decision 0006 to allow it, in the same pull request.
2. Set `<PrivacyException>true</PrivacyException>` in the project file. That project is then built without the ban.
3. Add an entry to `exceptions.json`:

   ```json
   { "project": "src/DocuSpend.RulesOnline/DocuSpend.RulesOnline.csproj",
     "reason": "Fetches and shares rule packs when the user asks. Never sees a statement.",
     "decision": "docs/decisions/0006-privacy-approach-and-network-boundary.md" }
   ```

A test requires the projects that set `PrivacyException` and the entries in `exceptions.json` to match exactly, and fails if an excepted project can reach the statement or categorization projects, directly or through other projects.
