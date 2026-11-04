# Plan: See whether the month ended ahead or behind

Spec: [spec.md](spec.md)

## Approach

No new dependency, package, file format or saved data. The change is one number worked out in screen logic, shown in the window's header, built on what is there (.NET SDK 10.0.100, Avalonia 11.3.22). The privacy lists don't change.

### Choices to know about first
- **No new colour.** "Bilancia" uses the palette's `Positive` (already the green of incoming amounts) and `Error` (already the red of error messages). The contrast test already checks both on `Card`, the header's background, at 4.5 to 1 in both looks, so `Palette.cs` and the contrast pairs don't change. The issue expected a new red. The spec chose the existing one.
- **The number comes from the categorization's own totals:** `IncomeTotal − SpendingTotal` of `CategorizedStatement`. Nothing in `DocuSpend.Categorization` changes.
- **The spec was corrected while planning:** the header's key figures show only while "Výpis" is chosen. The approved spec said all three screens, but `MainWindow` shows them only with `ShowsStatement`, and `ScreensWindowTests` checks that "Výdavky spolu" is hidden on "Pravidlá". "Bilancia" follows the other key figures.

### Verified before planning (2026-11-04)
- `CategorizedStatement.IncomeTotal` is the sum of the balances of every category with the role income, including "Nezaradené príjmy" (`uncategorized-in`, role `Income`). `SpendingTotal` is minus the sum of the balances of every spending category, including "Nezaradené výdavky" (`uncategorized-out`, role `Spending`). So `IncomeTotal − SpendingTotal` is the sum of the balances of every category that isn't excluded, as the spec defines it. `IsComplete` already relies on closing − opening = income + excluded − spending.
- A transaction of 0,00 € goes to the money-in bucket (`Amount >= 0`). It changes nothing in the sum.
- I checked how amounts format in sk-SK with `C2`, in a throwaway console app on .NET 10 (deleted afterwards). `0m`, a negative zero (`-(0m)`, `0m − -(0m)`) and `10m − 10.00m` all print "0,00 €" with no sign, and none is `> 0` or `< 0`. "-640,00 €" prints with a hyphen-minus, as `SlovakFormatTests` already expects. So the sign rule is `amount > 0 ? "+" + Amount(amount) : Amount(amount)`, and zero needs no special case.
- The palette pairs `Positive`/`Card` and `Error`/`Card` are in `LookTests.Pairs` at 4.5, for both looks.
- The header's key figures sit in a `WrapPanel` inside a `card` border, visible with `ShowsStatement`. The help mark is a `Button` with class `help` and a tooltip, as `UnknownTypesHelp` and `RoleHelp` do. Its style makes it reachable with the keyboard.
- The look test rejects a literal colour in a screen's XAML, so the colour goes through classes defined in `Look.axaml`.

### Screen logic (`DocuSpend.Presentation`)
- `SlovakFormat.SignedAmount(decimal)`: "+555,00 €", "-120,00 €", "0,00 €".
- `Texts.Net = "Bilancia"` and `Texts.NetHelp`, the help text exactly as the spec quotes it.
- `StatementViewModel`: `Net` (the formatted string) and `IsBehind` (`true` when the value is below zero), set in `Refresh` beside `SpendingTotal`. `Refresh` already runs on every change of `Config` (a rule, a hand choice, an undo, a new category) and on `Show`, so the number follows the spec's changes with nothing added. The view model offers one flag, not two. Green is "not behind", so zero needs no extra state.

### The window (`DocuSpend.Desktop`)
- `MainWindow.axaml`: a new `StackPanel` in the header's `WrapPanel`, between "Výdavky spolu" and "Počiatočný zostatok", with the same `Look.Margin.KeyFigure` and bottom alignment as the balances. It holds a label row (the label and the help mark `NetHelp`, in a `headingRow`) and the number, a `TextBlock` named `Net` with classes `keyNumber` and `ahead`, and `Classes.behind` bound to `IsBehind`.
- `Look.axaml`: `TextBlock.keyNumber.ahead` takes `Look.Positive`, and `TextBlock.keyNumber.ahead.behind` takes `Look.Error`. The two-class selector is the more specific one, so the red wins whenever `behind` is set.

### Alternatives considered
- **A new red in the palette.** The issue asked for one. The existing `Error` red already passes on the card in both looks, and a second red would look like a mistake next to the error messages' red.
- **Working the number out from the balances** (closing − opening − excluded). It gives the same value, since every statement shown reconciles, but it hides what the number means. Income − spending reads like the spec.
- **A converter in the window that picks the colour from the number.** It puts logic where only window tests reach it. A flag from the view model is tested in `DocuSpend.Presentation.Tests`.
- **A sum in `DocuSpend.Categorization`** (a `Net` property on `CategorizedStatement`). It is one subtraction used by one screen, and the categorization's results don't need to change.

### Layout
```
src/DocuSpend.Presentation/SlovakFormat.cs                  SignedAmount
src/DocuSpend.Presentation/Texts.cs                         "Bilancia" and its help text
src/DocuSpend.Presentation/StatementViewModel.cs            Net, IsBehind
src/DocuSpend.Desktop/MainWindow.axaml                      the key figure and its help mark
src/DocuSpend.Desktop/Look/Look.axaml                       ahead and behind
tests/DocuSpend.Presentation.Tests/SlovakFormatTests.cs     the signs
tests/DocuSpend.Presentation.Tests/StatementViewModelTests.cs   the number, its sign and colour flag, and its changes
tests/DocuSpend.Desktop.Tests/MainWindowTests.cs            the figure, its place and its help mark
tests/DocuSpend.Desktop.Tests/ScreensWindowTests.cs         shown on "Výpis" only
tests/DocuSpend.Desktop.Tests/LookSwitchWindowTests.cs      green and red in both looks
```

## Prerequisites
- #47 is merged into `main` (it is: `fdb7671`).
- The maintainer reviews the screenshots (step 3) and does the hand check on a real statement (step 4).

## Steps
1. **The number in screen logic**, test first. This step sets the intent to `in-progress`.
   - `SlovakFormatTests`: `SignedAmount` gives "+555,00 €", "-120,00 €", "0,00 €", and "0,00 €" for a negative zero.
   - `StatementViewModelTests`:
     - For every valid sample, `Net` is `SignedAmount(IncomeTotal − SpendingTotal)`, and that value equals closing − opening − `ExcludedTotal`.
     - Statements built in the test:
       - Money in and out of "Prevody medzi vlastnými účtami" and "Investície" doesn't change `Net`.
       - Uncategorized money in raises it and uncategorized money out lowers it.
       - A payment out of "Príjmy" lowers it, and a refund into a spending category raises it.
       - Income above spending gives a plus sign and `IsBehind` false. Spending above income gives a minus sign and `IsBehind` true. Equal amounts give "0,00 €" and `IsBehind` false.
       - An empty month (`04-empty-month`) gives "0,00 €" and `IsBehind` false.
     - After a user rule moves an incoming payment into "Prevody medzi vlastnými účtami", the number turns from positive to negative and `IsBehind` turns true, with property changes raised. Deleting the rule turns it back. A hand choice into an excluded category and its undo do the same.
   - `TextsTests`: the label and the help text are the spec's, exactly.
2. **The window**:
   - `MainWindowTests`: with `01-typical-month` open, the header shows "Bilancia" with the view model's number. Its key figure is the second one in the header's `WrapPanel`, after "Výdavky spolu" and before "Počiatočný zostatok". `NetHelp`'s tooltip is `Texts.NetHelp`, and the help mark can take keyboard focus.
   - `ScreensWindowTests`: "Bilancia" shows on "Výpis" and not on "Pravidlá" or "Kategórie".
   - `LookSwitchWindowTests`, in both looks: the number is in `Positive` for a statement that ended ahead. After a rule makes it behind, it is in `Error`, without reopening.
   - The look test passes.
3. **Screenshots** in both looks, with `dotnet run --project tools/DocuSpend.Screenshots`. **Stop: the maintainer reviews them** against the spec's sketch. Changes from the review go into the spec and are built before going on.
4. **Hand check. Stop: the maintainer** opens a real statement and checks that "Bilancia" matches income minus spending worked out by hand from the "Kategórie" card.
5. **Finish** with the `finish-item` skill:
   - the glossary gets "Net (Bilancia)": income minus spending, without excluded categories;
   - README, if it lists the header's figures;
   - the intent set to `done`.

## Risks and unknowns
- **The header gets crowded at the default size.** Four key figures and the unknown-types note might not fit on one line at 1280 pixels. The `WrapPanel` then moves a figure to a second line, which the spec allows. The screenshots in step 3 show it. If the maintainer finds it untidy, the fallback is a smaller margin between figures in `Look.axaml`, with the spec's sketch updated.
- **The help mark changes the label's height** and lifts "Bilancia" out of line with the balances. The fallback is aligning the help mark to the label's text, as the "Kategórie" form does, and checking it in the screenshots.
- **The red is read as an error.** It is the error colour on purpose (see alternatives). If the screenshot review finds it alarming, a separate colour becomes a palette change with its own contrast pairs, and the spec is updated.
- **Found in the screenshot review:** nothing to change. The four figures and the unknown-types note fit on one line at the default size in both looks. `01-typical-month` shows +554,99 € in green, `02-busy-month` −5 359,17 € in red, and `04-empty-month` 0,00 € in green. The help mark makes the label row about 2 pixels taller than the balances' labels, and the maintainer accepted it as it is.
- **A test relies on the header's figures by position** (for example the third text in the header). Step 2 runs the whole suite. Such a test is changed to find what it means by name.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| "Bilancia" after "Výdavky spolu", before "Počiatočný zostatok", on "Výpis" only | `MainWindowTests`, `ScreensWindowTests` |
| No excluded money: equals closing − opening | `StatementViewModelTests` (every valid sample: equals closing − opening − excluded) |
| Income − "Výdavky spolu"; excluded money doesn't change it | `StatementViewModelTests` |
| Uncategorized in as income, out as spending | `StatementViewModelTests` |
| Money out of income lowers it; a refund into spending raises it | `StatementViewModelTests` |
| Ahead: plus sign, green | `SlovakFormatTests`, `StatementViewModelTests` (`IsBehind`), `LookSwitchWindowTests` (colour) |
| Behind: minus sign, red | `SlovakFormatTests`, `StatementViewModelTests`, `LookSwitchWindowTests` |
| Zero (and an empty month): "0,00 €", no sign, green | `SlovakFormatTests`, `StatementViewModelTests` |
| Help mark shows the exact text, reachable by keyboard | `TextsTests`, `MainWindowTests` |
| Follows a rule, a hand choice, an undo | `StatementViewModelTests` |
| Colour turns without reopening | `StatementViewModelTests`, `LookSwitchWindowTests` |
| Green and red readable on the header in both looks | the existing `LookTests` pairs (`Positive`/`Card`, `Error`/`Card`) |
| Screenshots reviewed | step 3, maintainer |
| Privacy checks, no new entries | `DocuSpend.Privacy.Tests` |
| Hand check on a real statement | step 4, maintainer |
