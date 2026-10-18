# 0005: Avalonia for the user interface

Status: accepted. To be confirmed by the spike in [#5](https://github.com/mDubovsky/DocuSpend/issues/5); if it fails, this decision is revisited.

## Context
The MVP needs charts (a spending pie and a balance line, with a category you can click into), lists, a rule editor with forms, and Slovak text ([decision 0003](0003-slovak-interface-english-code.md)). The users are not technical, the product's promise is that a statement never leaves the computer, and the code is in .NET ([decision 0001](0001-use-dotnet.md)).

Options considered: a Blazor WebAssembly website, Avalonia, WPF, Blazor Hybrid (web interface in a desktop window), and a JavaScript front end with .NET behind it. A throwaway experiment drew the same two charts in WPF and Avalonia (LiveCharts2) and in a web chart library; all three showed the MVP's charts equally well, so chart quality does not decide this.

## Decision
- The interface is a **native desktop application built with Avalonia**. **Windows first**; macOS and Linux stay possible later without a rewrite.
- Charts are drawn with a library that runs on Avalonia (LiveCharts2 is the candidate; the choice is confirmed in the plan of the first interface item).
- The application is distributed as **builds produced by CI on GitHub**, not through the Microsoft Store.
- No interface code is written until the spike (#5) has said go.

## Why
- **A website is rejected because of trust.** A user who knows nothing about development will not upload a bank statement to a web page, however local the processing really is, and cannot be expected to check.
- **No browser engine in the process.** Blazor Hybrid would show the interface in WebView2, Microsoft's browser component, whose own [privacy documentation](https://learn.microsoft.com/he-il/microsoft-edge/webview2/concepts/data-privacy) describes diagnostic data collection under Windows settings. That is code we don't control running next to a bank statement. A native toolkit has no such component, so the claim "no networking code, no telemetry component" is easier to make and to defend ([decision 0006](0006-privacy-approach-and-network-boundary.md)).
- **Reach and effort.** WPF is the most mature on Windows but closes off every other platform. Avalonia costs little extra today and keeps them open.
- **A JavaScript front end with .NET behind it** means two languages and a bridge, against decision 0001, and reuses the least of what exists.

## Consequences
- Distribution without the Microsoft Store means no operating-system-enforced "no internet" label. The proof of privacy rests on the public repository and the checks in decision 0006.
- Unsigned downloads trigger Windows SmartScreen warnings. Code signing for open-source projects (there are free and low-cost options) is to be looked at when there is something to release.
- The ecosystem is smaller than WPF's, and fewer examples exist; the spike is meant to expose the gaps early.
- A later website, for example to browse shared rules, would be a separate small project and would not reuse the application's interface.
- Interface code must keep user-visible texts and number formatting in one place so Slovak formatting ("1 349,87 €") comes from a culture setting, not from hand-built strings.
