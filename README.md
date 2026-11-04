# DocuSpend

Turn a bank statement into a clear picture of where your money went.

Give DocuSpend a statement and get charts showing when and how you spent: how much went to food this month, how much to travel, and so on. Your statement never leaves your computer.

**Status:** early development, no release yet. The first version, a Windows desktop application in Slovak, reads monthly PDF statements in the format of Všeobecná úverová banka (VÚB) and shows:

- spending by category as a chart, with a list of the transactions in each category;
- the account balance through the month;
- rules you add to sort transactions into categories.

What works today: opening your own statement, with a clear message for a file that can't be read; sorting transactions into categories with built-in rules and rules of your own, which are kept between sessions and can be edited, reordered and deleted; the charts, whose hover cards show a slice's category, amount and share, and the balance on any day under the pointer; a statement screen that fits the window without scrolling, with the rules on a screen of their own, one click away; categories and sub-categories of your own, added on a "Kategórie" screen and ready as targets for your rules; statements with transaction types DocuSpend doesn't know yet, which still open, with those transactions left for your rules and a note saying how many there are; categorizing a single transaction from the list, by hand or with a rule filled in from it, with only a fingerprint of a hand-chosen transaction kept; everything the statement says about a transaction, shown by opening its row in the list; the largest categories and transactions first on the statement screen, money out and money in alike; whether the month ended ahead or behind, as income minus spending beside the total spending, in green or red; and one look, light or dark, that every control follows, scroll bars and the look switch included. Fixing the window tests that sometimes hang on Linux CI ([#44](https://github.com/mDubovsky/DocuSpend/issues/44)) is next. What the first version includes: [docs/mvp.md](docs/mvp.md).

## Privacy

DocuSpend contains no networking code, no telemetry and no automatic updates, and the build checks that on every change. [PRIVACY.md](PRIVACY.md) explains what it does with a statement and how you can check it yourself.

## Build and run

You need the .NET SDK version in [global.json](global.json). From the repository root:

```
dotnet build
dotnet test
dotnet run --project src/DocuSpend.Desktop
```

The application runs on Windows. It opens a VÚB monthly statement you choose, by its open window or by dropping the file onto it, and offers one invented example statement to try it on; [samples/public](samples/public) has more synthetic statements to open. The tests, including the privacy checks, also run on Linux.

## More

- What and why: [docs/product.md](docs/product.md)
- How we work (AI-assisted, intent → spec → plan → code): [docs/sdlc.md](docs/sdlc.md)
- Work items: [work/](work/); decisions: [docs/decisions/](docs/decisions/)
- Contributing, and the privacy checks in detail: [CONTRIBUTING.md](CONTRIBUTING.md)

## License

GPL-3.0, see [LICENSE](LICENSE).
