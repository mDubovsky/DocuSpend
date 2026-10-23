# Privacy rules

DocuSpend's rule is [decision 0006](../docs/decisions/0006-privacy-approach-and-network-boundary.md): **no code that can see a statement may use the network**. The files in this folder are what the build and the tests enforce. Changing them is a normal, reviewed change. Weakening them is a change to decision 0006.

| File | What it is |
|---|---|
| [BannedSymbols.txt](BannedSymbols.txt) | What counts as networking in our own code: HTTP, sockets, web sockets, mail, DNS, ping, secure streams and pipes; starting other programs; calling operating-system libraries or COM directly; and loading types or assemblies by name, which would get around the rest. |
| [exceptions.json](exceptions.json) | Projects allowed to use networking. Empty. |

## The ban on networking in our own code (`privacy-no-networking-code`)

Every project in the repository, including the developer tools and the tests, is compiled with the [banned-API analyzer](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers) and `BannedSymbols.txt` (wired up in [Directory.Build.targets](../Directory.Build.targets)). A use of anything on the list fails the build with a message like:

```
error RS0030: The symbol 'HttpClient' is banned in this project: privacy-no-networking-code: HTTP is not allowed in DocuSpend code (decision 0006, ...). To request an exception, see privacy/README.md.
```

The build also fails if the ban is switched off: if the analyzer or the list is missing from a project, or analyzers are disabled. The tests in `tests/DocuSpend.Privacy.Tests` fail if the warning is suppressed in code (`#pragma warning disable`, `SuppressMessage`) or lowered in an `.editorconfig`, `.globalconfig` or project file.

If you hit the ban, the usual answer is to do the work without the network: DocuSpend reads files the user chooses, and nothing else.

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
