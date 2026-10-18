# 0006: Privacy approach and the network boundary

Status: accepted. The checks are built in [#4](https://github.com/mDubovsky/DocuSpend/issues/4).

## Context
The product's promise is that a statement never leaves the user's computer. The repository will be public so anyone can inspect it, but most users can't read code and shouldn't need to. Without the Microsoft Store ([decision 0005](0005-avalonia-for-the-user-interface.md)) there is no operating-system-enforced "no internet" label, so the repository itself has to be the evidence.

A later feature is wanted: users may choose to share a rule they made (never a statement, never amounts or personal details) and to fetch rules others shared, through a server of our own. Anonymization is to be done as far as possible; whether the server records IP addresses is not yet decided.

## Decision
1. **The boundary rule: no code that can see a statement may use the network.** Today that is all of it: no project uses networking, and none depends on a package that does.
2. **It is enforced by the build, not by promises.** A ban on networking APIs applies per project; a test checks that no shipped assembly or dependency references networking; the full read-and-categorize flow also runs in CI with the network switched off. Adding networking, or a package that has it, fails the build with an explanation.
3. **The optional online rules feature gets one deliberately small, separate module** (provisionally `DocuSpend.RulesOnline`), the only place allowed to use the network, with these limits:
   - it cannot reference the statement or transaction types; an architecture test enforces that. It works only with a small rule-pack data type;
   - it talks only to fixed, listed addresses, and sending is limited to a reviewed rule, with its exact content shown to the user first;
   - it does nothing at startup and nothing in the background. Sharing and fetching happen only when the user asks;
   - it is **off by default**, and the application is fully usable without it;
   - adding it is a change to this decision, not a quiet commit.
4. **Sharing never includes:** account-number, variable-symbol or amount rules, any statement content, or any identifier of the user. Rules containing free text are reviewed before they ever become a default for others. The server stores as little as possible and keeps nothing that identifies the sender, as far as that can be arranged.
5. **The public repository is the evidence.** Builds for release are made only by CI, each with a signed record of the commit and workflow it came from, so a download can be verified against the source. The repository carries code scanning, dependency and secret scanning, a list of what the application includes, and a short privacy document saying what the application does and doesn't do and how to check it.

## Why
- The strongest guarantees are the ones the build enforces on every change. A report written once, or a scanner that finds some classes of problems, is weaker evidence. Scanners will be used, but as a supplement.
- Scoping the ban to code that can see a statement keeps the future feature possible. A blanket ban would force a choice between the feature and the checks.
- The feature's server is a trust boundary of its own and is treated as one: it never receives anything that could contain a statement.

## Consequences
- The statement we can make today is "DocuSpend contains no networking code, and the build fails if any is added." With the optional module it becomes "networking exists only in one small optional part that never sees your statements and runs only when you press a button."
- Contributors can't add networking, analytics, crash reporting or auto-update to the main code. If one of those is ever wanted, it follows the same path as the online rules module.
- Anonymity and "no server of our own" pull in different directions; this decision accepts a server and defers the details (IP handling, retention) to the item that designs sharing.
- Scanners such as CodeQL, dependency checkers, and Privado (a privacy-specific data-flow scanner whose C# support is unconfirmed) are candidates; which ones are used is decided in #4.
