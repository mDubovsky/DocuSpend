# 0001: Use .NET (C#)

Status: accepted

## Context
The first code in the repo is the synthetic statement generator ([work/001-synthetic-statements](../../work/001-synthetic-statements/plan.md)), so its language effectively becomes the project's language. Candidates were .NET, Python and TypeScript/Node.

## Decision
Use .NET (C#), targeting the latest LTS (.NET 10). The SDK version is pinned in `global.json`. Tests use xUnit v3 on Microsoft.Testing.Platform.

## Consequences
- .NET was already installed on the maintainer's machine; MIT and Apache-licensed PDF libraries (PDFsharp, PdfPig) cover generating and reading PDFs; the unit-testing story is strong.
- Contributors need the .NET SDK, not just the runtime.
- The charting and UI technology is still undecided and will be chosen in a later item.
- Rejected: Python (not installed, harder to package for non-technical users), TypeScript/Node (not installed; makes most sense only if the app is a web app).
