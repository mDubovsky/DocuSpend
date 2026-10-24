# DocuSpend

Turn a bank statement into a clear picture of where your money went.

Give DocuSpend a statement and get charts showing when and how you spent: how much went to food this month, how much to travel, and so on. Your statement never leaves your computer.

**Status:** early development, no release yet. The first version, a Windows desktop application in Slovak, reads monthly PDF statements in the format of Všeobecná úverová banka (VÚB) and shows:

- spending by category as a chart, with a list of the transactions in each category;
- the account balance through the month;
- rules you add to sort transactions into categories.

What works today: reading statements, sorting transactions into categories with built-in and user rules, and the charts, shown on synthetic sample statements. Opening your own statement ([#12](https://github.com/mDubovsky/DocuSpend/issues/12)) and keeping your rules between sessions ([#13](https://github.com/mDubovsky/DocuSpend/issues/13)) are next. What the first version includes: [docs/mvp.md](docs/mvp.md).

## Privacy

DocuSpend contains no networking code, no telemetry and no automatic updates, and the build checks that on every change. [PRIVACY.md](PRIVACY.md) explains what it does with a statement and how you can check it yourself.

## Build and run

You need the .NET SDK version in [global.json](global.json). From the repository root:

```
dotnet build
dotnet test
dotnet run --project src/DocuSpend.Desktop
```

The application runs on Windows and shows the synthetic statements in [samples/public](samples/public). The tests, including the privacy checks, also run on Linux.

## More

- What and why: [docs/product.md](docs/product.md)
- How we work (AI-assisted, intent → spec → plan → code): [docs/sdlc.md](docs/sdlc.md)
- Work items: [work/](work/); decisions: [docs/decisions/](docs/decisions/)
- Contributing, and the privacy checks in detail: [CONTRIBUTING.md](CONTRIBUTING.md)

## License

GPL-3.0, see [LICENSE](LICENSE).
