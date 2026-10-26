# DocuSpend

Turn a bank statement into a clear picture of where your money went.

Give DocuSpend a statement and get charts showing when and how you spent: how much went to food this month, how much to travel, and so on. Your statement never leaves your computer.

**Status:** early development, no release yet. The first version, a Windows desktop application in Slovak, reads monthly PDF statements in the format of Všeobecná úverová banka (VÚB) and shows:

- spending by category as a chart, with a list of the transactions in each category;
- the account balance through the month;
- rules you add to sort transactions into categories.

What works today: opening your own statement, with a clear message for a file that can't be read; sorting transactions into categories with built-in rules and rules of your own, which are kept between sessions and can be edited, reordered and deleted; and the charts, whose hover cards show a slice's category, amount and share, and the balance on any day under the pointer. Fixing the look switch and scroll bars ([#19](https://github.com/mDubovsky/DocuSpend/issues/19)) is next. What the first version includes: [docs/mvp.md](docs/mvp.md).

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
