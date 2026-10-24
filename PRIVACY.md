# Privacy

A bank statement says a lot about you. DocuSpend is built so that it never leaves your computer, and so that you don't have to take our word for it.

**Where things stand:** the DocuSpend application isn't built yet. Today the project has the parts that read and categorize statements, and two small command-line tools for developers. Everything below applies to them now and to the application when it comes.

## What DocuSpend does with your statement

It reads the file you choose, in your computer's memory, and shows you the result. That is all. It doesn't send a copy anywhere, and it doesn't save one.

## What it never does

- No network connections, of any kind.
- No analytics or usage statistics.
- No crash reports.
- No automatic updates.
- No accounts and no sign-in.

## How this is enforced

These aren't promises in a document; they are checks that run automatically on every change to the code, in public, and fail if the rule is broken. You can see them run on the project's [Actions page](https://github.com/mDubovsky/DocuSpend/actions).

- **No networking in the code.** The code is compiled with a list of everything that could reach the network (web requests, connections, email, starting other programs, and ways around these). Using any of it stops the build. This covers all of the code, including the developer tools and the tests.
- **No networking brought in by others' code.** DocuSpend uses a few libraries written by others. Everything that ships is inspected, down to the libraries those libraries use. Anything that can reach the network fails the check, unless a person has reviewed it, and that review is written down in the open with the reason (see [privacy/allowed-dependencies.json](privacy/allowed-dependencies.json)). Known analytics, crash-reporting and auto-update libraries are refused by name.
- **It works with the network switched off.** Every change is also tested on a computer with its network removed: DocuSpend reads and categorizes a set of sample statements there. A small test first makes sure that computer really can't connect.
- **No real data in the project.** Only invented sample statements are allowed in the project, and a check refuses anything that looks like a real one.
- **Downloads match the code.** Files you can download are built only by the automatic pipeline, never on someone's own computer, and each comes with a list of everything it contains and a signed record of exactly which code and which build produced it.

The technical detail, for those who want it: [privacy/README.md](privacy/README.md) and [CONTRIBUTING.md](CONTRIBUTING.md).

## Check it yourself

These take a few minutes and need no programming.

**Block it in your firewall and use it normally.** If DocuSpend still works, it didn't need the network.
- Windows: open *Windows Defender Firewall with Advanced Security*, choose *Outbound Rules* → *New Rule…* → *Program*, select DocuSpend's program file, and choose *Block the connection*.
- Linux: start it with no network at all: `sudo unshare --net -- ./DocuSpend.StatementCheck my-statement.pdf` (any DocuSpend program works the same way).

**Look at its network connections while it runs.** There should be none.
- Windows: open *Resource Monitor* (press Win+R, type `resmon`), go to the *Network* tab, and look for DocuSpend under *Processes with Network Activity* and *TCP Connections*. It shouldn't appear.
- Windows, in PowerShell: `Get-NetTCPConnection -OwningProcess (Get-Process DocuSpend*).Id`. A message starting "No MSFT_NetTCPConnection objects found" means no connections.
- Linux: `ss -tunp | grep -i docuspend` prints nothing.

**Check that a download came from this project.** With the [GitHub command-line tool](https://cli.github.com) installed, run, for the file you downloaded:

```
gh attestation verify DocuSpend.StatementCheck-win-x64.zip --repo mDubovsky/DocuSpend
```

It prints the exact commit and build the file came from, or an error if the file was changed or didn't come from here. These records exist for builds made while the project is public.

## What the checks don't cover

We'd rather tell you the limits than overstate the guarantee.

- **Compiled native components.** Some libraries include pre-compiled machine code. The checks can list it, and it must be reviewed, but they can't prove what it does. The desktop application includes three, all open source, for drawing: Skia (graphics), HarfBuzz (text) and ANGLE (graphics on Windows). None of them imports a networking library, which is what the review looked at; [privacy/native-libraries.json](privacy/native-libraries.json) lists them.
- **Your operating system.** Windows or Linux may connect to the internet on their own, for updates or for features like cloud-synced folders. That is outside DocuSpend.
- **Files in network locations.** If you open a statement stored on a network drive or in a synced cloud folder, your computer fetches it over the network. DocuSpend only reads what it's given.
- **Certification.** The checks don't certify DocuSpend against any privacy standard. Scanners find classes of problems, not every possible one; the strongest guarantees are the ones the build enforces on every change.

## A possible optional online feature

Later, DocuSpend may let you share a categorization rule you made, or fetch rules others shared, through a server of our own. If that happens, it will be:

- a separate, small part of DocuSpend, and the only part allowed to use the network;
- unable to see your statements at all, which a check enforces;
- off by default, used only when you press a button, and never running in the background;
- shown to you before anything is sent, and never including amounts, account numbers or anything else from your statements.

DocuSpend stays fully usable without it. Adding it means changing the project's recorded privacy decision ([decision 0006](docs/decisions/0006-privacy-approach-and-network-boundary.md)), in public, not a quiet change.
