# Plan: Create your own categories and sub-categories

Spec: [spec.md](spec.md)

## Approach

**No new dependency, and no change to what is saved.** Everything is built from what DocuSpend already uses: Avalonia 11.3.22 with its Fluent theme, LiveChartsCore.SkiaSharpView.Avalonia 2.0.5 and CommunityToolkit.Mvvm. `SavedRules`, `RuleStore` and the privacy lists don't change. The work is a new view model for the card, a few commands on `MainViewModel`, and a card in the window.

Checked on 29 October 2026, on this branch:
- **The categorization already does the adding.** `CategorizationConfig.AddCategory(name, role)` and `AddSubcategory(categoryId, name)` exist, refuse an empty name (`RefusalKind.EmptyName`, "Zadajte názov.") and a repeated one, ignoring case, diacritics and spaces around it (`DuplicateName`, "Taký názov už existuje."), give a new category its "Ostatné" (`<id>.other`), and refuse a sub-category in an uncategorized bucket. `CatalogChangeTests` covers them. Only the 40-character limit is new, and it belongs to the screen.
- **New things go to the end.** `AddCategory` appends the category after the two uncategorized buckets, and `AddSubcategory` appends after "Ostatné". `SavedRules` loads them back in the same places. So the categorization's order is stable across a restart, but "Ostatné" isn't last, and the user's categories sit after "Nezaradené". The card and the rule form therefore sort for display: built-in categories, then the user's own, then the uncategorized buckets; within a category, "Ostatné" (the sub-category whose identifier is `<category>.other`) last. The categorization's own order stays as it is, so nothing saved changes.
- **Slice colours won't shift.** `SliceColors.For` numbers the spending categories that aren't uncategorized, in catalog order. The 11 built-in ones come first, and a new category is appended, so it gets the 12th colour and no built-in colour moves. There are 14 colours, so a user's fourth spending category repeats the first colour, as the spec allows.
- **The rule form's choices are fixed at its start.** `RuleFormViewModel` takes the catalog in its constructor and keeps `Categories` from then on. It gets a method to take a new catalog that keeps the chosen category and sub-category by identifier.
- **There is room under the form.** In the approved #21 screenshot of the rules screen at 1280 × 940, the form card ends at y ≈ 440 and the column at ≈ 918. That leaves about 460 points for "Kategórie": the add fields and about five categories with their sub-categories, with the rest scrolling. Editing a rule makes the form taller (the priority row and messages), and the card gets shorter then.
- **`TextBox.MaxLength` and `BringIntoView` are in Avalonia 11.3.22** (its XML docs, `Avalonia.Controls.xml`). `MaxLength` cuts a paste at the limit. The look test doesn't check it, but the number comes from Presentation anyway (`x:Static`), so the window and the view model share one limit.

### The screen logic, in `DocuSpend.Presentation`
- **`CategoriesViewModel`** (new), owned by `MainViewModel` as `Categories`. It holds:
  - `Rows`: one `CategoryCardRow` per category, in display order, each with its name, role tag, whether it is the user's own, whether sub-categories can be added (not for the uncategorized buckets), and its sub-categories in display order, each with its name and whether it is the user's own. It is rebuilt from the catalog after every change.
  - the new category's fields: `NewName` (cut at `MaxNameLength` = 40), `Roles` (three options, "Výdavky" first) and `SelectedRole`, and `NameError`, cleared when `NewName` changes;
  - the sub-category line: `OpenRow` (the category whose line is open, or none), `SubcategoryName` and `SubcategoryError`. `OpenLine(row)` opens it, closing any other and emptying the name; `CancelLine()` closes it;
  - `Message` (the confirmation) and `Error` (a save failure), as the rule form has them, and `LastAdded`, the category to bring into view.
- **`MainViewModel`** gets `AddCategoryCommand` and `AddSubcategoryCommand`. Each calls the config's method; a refusal goes to the field's error through `Texts.Refusal`. A success goes through the existing `ApplyUserChange`, the one place every change is saved and shown, so saving, the "not saving" state and recategorizing work as for rules. Then it refreshes the card's rows and the rule form's choices, and sets the message: the confirmation when saved, otherwise the session-only or save-failed text, as `Confirm` does for rules.
- **Display order in one place**: a small `CatalogOrder` helper (categories, and sub-categories with "Ostatné" last) used by the card and by `RuleFormViewModel`.
- **`Texts`**: "Názov", "Počíta sa ako", "Pridať kategóriu", "Pridať podkategóriu", "Pridať", "vlastná", the three roles and their tags, the help text, `CategoryAdded(name)`, `SubcategoryAdded(name, category)`, and the four save texts. "Kategórie" and "Zrušiť" exist already.

### The window, in `DocuSpend.Desktop`
- **The rules screen's left column becomes a grid of two rows**: the form (`Auto`) and the new `CategoriesCard` (`*`). The rules card keeps the right column's full height.
- **`CategoriesCard`**: the heading "Kategórie"; the "Zmeny pravidiel sa teraz neukladajú." note while `RulesNotSaving`; the add fields, with the reason under the name; the message; then a `ScrollViewer` (the look's scroll bars, always shown) with an `ItemsControl` of rows:
  - a row: the name, the role tag and the "vlastná" tag as small pills, and "Pridať podkategóriu" on the right;
  - its sub-categories in a `WrapPanel` of pills, the user's own in the accent style;
  - the line, while it is open: a name field, "Pridať", "Zrušiť", and the reason under it.
- **Add fields on one line**, `[Názov…] [Výdavky ▾] (?) [Pridať kategóriu]`, rather than the spec sketch's three lines. They fit in the column's ≈ 560 points, and that leaves the list a row or two more. The screenshot review decides, and the spec follows.
- **Focus and scrolling in the code-behind**: when a line opens, its name field takes the focus; when `LastAdded` changes, the window brings that row into view.
- **The tags and pills** become styles in `Look/Look.axaml` (`Border.tag`, `Border.pill`, `Border.pill.own`) from the palette's existing colours. No new colour is needed; if the review asks for one, it goes in `Palette.cs` with its contrast test.

### Tests and screenshots
- Logic in `DocuSpend.Presentation.Tests` (`CategoriesTests.cs`, new; `TextsTests.cs`), and colour stability in `LookTests.cs` there. The window in `DocuSpend.Desktop.Tests` (`CategoriesWindowTests.cs`, new).
- New screenshots: `20-categories` (the user's own category and sub-categories listed, a confirmation shown), `21-subcategory-line` (a line open, with "Taký názov už existuje."), and `22-categories-not-saving`. `18-rules-screen` shows the new card as well.

### Alternatives considered
- **Putting "Ostatné" last inside the categorization** (inserting before it when adding): lost. It would change the order the categorization holds, and with it the order the saved file has for built-in categories with user sub-categories. Sorting for display gives the same picture with nothing saved differently.
- **Moving the user's categories before "Nezaradené" in the catalog**: lost for the same reason, and it would shift the slice colours' numbering for nothing.
- **A third screen, "Kategórie", in the header switch**: lost in the intent.
- **A pop-up box for adding**: lost in the spec.
- **A `TreeView`** for categories and sub-categories: lost. Its expand arrows and selection aren't wanted, its Fluent template would need restyling in both looks, and the spec shows every sub-category at once. An `ItemsControl` with a `WrapPanel` is plain and already styled by the look.
- **Commands on `CategoriesViewModel` with a callback into `MainViewModel`**: lost. The rule commands live on `MainViewModel`, next to `ApplyUserChange` and the store, and the categories' commands follow them.

### Layout
New files:
- `src/DocuSpend.Presentation/CategoriesViewModel.cs`, `CatalogOrder.cs`
- `tests/DocuSpend.Presentation.Tests/CategoriesTests.cs`
- `tests/DocuSpend.Desktop.Tests/CategoriesWindowTests.cs`

Changed:
- `src/DocuSpend.Presentation/MainViewModel.cs`, `RuleFormViewModel.cs`, `Texts.cs`
- `src/DocuSpend.Desktop/MainWindow.axaml`, `MainWindow.axaml.cs`, `Look/Look.axaml`
- `tools/DocuSpend.Screenshots/Screens.cs`
- `tests/DocuSpend.Presentation.Tests/TextsTests.cs`, `LookTests.cs`, `RuleFormViewModelTests.cs`
- docs: `README.md`, `AGENTS.md`, `work/025-own-categories/`

## Prerequisites
- #21 is merged into `main` (it is), and this branch was made from it.
- The maintainer reviews the screenshots (step 4) and does the hand check with a real statement (step 5).

## Steps
Test-first for logic; one commit per step, committed when it is done and every test passes.

1. **The card's list** (`CatalogOrder`, `CategoriesViewModel.Rows`, `Texts`). Tests:
   - with no user categories: the 14 built-in categories in order, then "Nezaradené výdavky" and "Nezaradené príjmy"; each with its role tag; sub-categories with "Ostatné" last; no add for the uncategorized buckets;
   - with user categories and sub-categories (built through `AddCategory` and `AddSubcategory`): the user's own after the built-in and before "Nezaradené", marked as own, "Ostatné" still last;
   - the rule form's sub-categories with "Ostatné" last;
   - the new texts, letter for letter.

   Sets the intent to `in-progress`.
2. **Adding** (`MainViewModel` commands, `CategoriesViewModel` fields, `RuleFormViewModel` taking a new catalog). Tests:
   - a category of each role is added and listed, with its confirmation; the name field empties and the role stays;
   - names trimmed; cut at 40; empty, spaces only, and repeated names (case, diacritics, "Nezaradené výdavky", a user's own) refused with the reason, nothing added, the name kept; the reason cleared on a change;
   - the sub-category line: opening, opening another, cancelling; adding to a built-in and to a user category; the refusals, including "Ostatné" and "ostatne"; the same name under another category or as its category's name allowed;
   - the rule form offers the new category and sub-category at once, for a new rule and an edited one, and keeps a half-typed rule's choices;
   - a rule targeting a new sub-category: its count, its row "<category> › <sub-category>", and the statement's categories;
   - a new spending, excluded and income category filled by a rule: the pie, "Výdavky spolu" and the income follow its role; a new category with no rule changes nothing in the statement;
   - built-in slice colours are the same before and after adding a spending category;
   - after a restart (a new `MainViewModel` on the same file): the category, its sub-category, its role and the rule are back, and the statement is categorized the same;
   - saving fails, and saving is off for the session: the texts for a category and for a sub-category, and the category still usable;
   - the card's state is kept across opening another statement.
3. **The card in the window** (`MainWindow.axaml`, `.cs`, `Look.axaml`). Window tests, on "Pravidlá":
   - the card is under the form, with the add fields, the help mark's tooltip, and the rows;
   - adding a category through the button shows its row and brings it into view;
   - "Pridať podkategóriu" opens the line with its field focused; a second one closes the first; "Zrušiť" closes it; a refusal shows under the field;
   - no "Pridať podkategóriu" on the "Nezaradené" rows;
   - the name fields take at most 40 characters;
   - with 30 user categories, the card's list scrolls inside it and the window has nothing to scroll at 1280 × 940;
   - "Zmeny pravidiel sa teraz neukladajú." shows in the card while saving is off;
   - the card's fields, an open line and the list's scroll position survive a switch to "Výpis" and back.

   The look test passes with the new styles.
4. **Screenshots.** **Stop for review:** the maintainer looks at `18`, `20`, `21` and `22` in both looks at the default size, and decides the add fields' arrangement (one line or three), the tags and pills, and how much of the list shows. Fixes go in this step, and the spec follows. The approved pictures go to `work/025-own-categories/screenshots/`.
5. **Hand check.** **Stop:** the maintainer opens a real statement, adds a category and a sub-category, adds a rule that sends transactions there, checks "Výpis", restarts DocuSpend, and finds the category, the sub-category and the rule as they left them.
6. **Docs and done**, with `finish-item`:
   - `README.md` ("What works today", and what is next) and `AGENTS.md` (the rules screen's cards);
   - the spec and this plan updated for anything the build changed;
   - the intent becomes `done`;
   - the follow-up issue for renaming and deleting your own categories, drafted with `new-issue` for the maintainer to approve, as the intent decided.

## Risks and unknowns

- **The card may show too few categories at the default size**, more so while a rule is being edited and the form is taller. Fallback, for the screenshot review: the add fields on one line (planned), smaller pills, or giving the card a minimum height so the window scrolls only when editing.
- **A refreshed rule form could lose its choice**, if the new category lists hold new objects and the combo boxes compare by reference. The form reselects by identifier, and a step 2 test covers a half-typed rule and a rule being edited.
- **Focusing a field in a line that is just appearing** may need a layout pass first. Fallback: focus it after the next layout (`Dispatcher.UIThread.Post` at `Loaded` priority), as the window already does for the charts.
- **Bringing a new row into view** may not work before its container exists. Fallback: scroll the `ScrollViewer` to the end, since a new category is always the last of the user's own, just above "Nezaradené".
- **Pills that wrap inside an `ItemsControl` row** can measure oddly in a `ScrollViewer` with an unbounded width. The `ItemsControl` gets the card's width, and the window test at the default size checks that nothing scrolls sideways.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| The rules screen shows the form, the rules card and "Kategórie" under the form; "Výpis" unchanged | `CategoriesWindowTests` (step 3); `ScreensWindowTests`, unchanged |
| Built-in list: 14 categories in order, then the two "Nezaradené", role tags, "Ostatné" last | `CategoriesTests` (step 1) |
| No add on the "Nezaradené" rows; every other row has it | `CategoriesTests` (step 1); `CategoriesWindowTests` (step 3) |
| More categories than fit: the card scrolls, the window doesn't | `CategoriesWindowTests` with 30 categories (step 3) |
| Adding "Šport" as "Výdavky": its place, tags, "Ostatné", confirmation, field emptied, role kept | `CategoriesTests` (step 2); `CategoriesWindowTests` through the button (step 3) |
| "Príjmy" and "Mimo výdavkov" tags | `CategoriesTests` (step 2) |
| "  Šport  " named "Šport" | `CategoriesTests` (step 2) |
| Empty or spaces-only name refused | `CategoriesTests` (step 2) |
| Repeated names refused (case, diacritics, "Nezaradené výdavky", the user's own), name kept | `CategoriesTests` (step 2) |
| A reason disappears when the name changes | `CategoriesTests` (step 2) |
| The help mark's tooltip text | `TextsTests` (step 1); `CategoriesWindowTests` (step 3) |
| "Pridať podkategóriu" opens a line with a focused field | `CategoriesWindowTests` (step 3) |
| Opening a second line closes the first | `CategoriesTests` (step 2); `CategoriesWindowTests` (step 3) |
| Adding "Obedy v práci" to "Stravovanie": before "Ostatné", own, confirmation | `CategoriesTests` (step 2) |
| Adding to a user category | `CategoriesTests` (step 2) |
| Sub-category refusals: empty, "Ostatné", "ostatne", "Potraviny"; line stays open | `CategoriesTests` (step 2); `CategoriesWindowTests` for the reason's place (step 3) |
| "Potraviny" under "Bývanie" and "Šport" under "Šport" allowed | `CategoriesTests` (step 2) |
| "Zrušiť" closes the line, nothing added | `CategoriesTests` (step 2); `CategoriesWindowTests` (step 3) |
| New choices in the rule form at once, new and edited rule, "Ostatné" last | `CategoriesTests`, `RuleFormViewModelTests` (steps 1, 2) |
| A half-typed rule keeps its choices | `CategoriesTests` (step 2) |
| A rule into a new sub-category: count, rule row, "Výpis" | `CategoriesTests` (step 2) |
| A new spending category gets a slice; built-in colours unchanged | `CategoriesTests`, `LookTests` (step 2) |
| A new excluded category: out of the pie and "Výdavky spolu", still in "Kategórie" | `CategoriesTests` (step 2) |
| A new income category isn't spending | `CategoriesTests` (step 2) |
| A new category with no rule: not on "Výpis", categorization unchanged | `CategoriesTests` (step 2) |
| Back after a restart, with its rule | `CategoriesTests` with a second `MainViewModel` (step 2) |
| A failed save: the texts, and the category still usable | `CategoriesTests`, store folder blocked (step 2) |
| Saving off: the note and the session-only texts | `CategoriesTests` (step 2); `CategoriesWindowTests` for the note (step 3) |
| Saved shape unchanged; earlier saved files still load | no change to `SavedRules`; `rules-v1.json` tests unchanged (every step) |
| Only the shared look; no literal values | `LookCheckTests` (every step) |
| Privacy checks pass, nothing new in the reviewed lists | `dotnet test` (every step) |
| Every new text in Slovak | `TextsTests`, letter for letter (step 1) |
| Screenshots approved | maintainer, step 4 |
| Hand check with a real statement | maintainer, step 5 |
| Edge: names cut at 40 characters | `CategoriesTests` (step 2); `CategoriesWindowTests`, `MaxLength` (step 3) |
| Edge: "Sport" and "Šport" the same | `CategoriesTests` (step 2) |
| Edge: spaces inside a name kept | `CategoriesTests` (step 2) |
| Edge: many categories, colours repeat after 14, never change | `LookTests` (step 2); `CategoriesWindowTests` (step 3) |
| Edge: adding while a rule is edited keeps the edit | `CategoriesTests` (step 2) |
| Edge: the top fields and the line keep their own text | `CategoriesTests` (step 2) |
| Edge: a switch to "Výpis" and back keeps the card | `CategoriesWindowTests` (step 3) |
| Edge: opening another statement keeps the card | `CategoriesTests` (step 2) |
| Edge: a later successful save saves what failed before | `CategoriesTests`: unblock the folder, add a rule, restart (step 2) |
