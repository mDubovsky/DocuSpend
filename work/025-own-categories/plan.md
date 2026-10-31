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
  - `Rows`: one `CategoryItem` per category, in display order, each with its name, role tag and role sentence, whether it is the user's own, whether sub-categories can be added (not for the uncategorized buckets), and its sub-categories in display order (`SubcategoryItem`: name, the user's own, the category's "Ostatné"). It is rebuilt from the catalog after every change, keeping the chosen category.
  - `SelectedRow`, the chosen category (the first at start); choosing another empties the sub-category field, its reason and the message, and closes the form;
  - the form for a new category: `IsAddingCategory` (`StartNewCategory`, `CancelNewCategory`), `NewName` (cut at `MaxNameLength` = 40), `Roles` (three options, "Výdavky" first) and `SelectedRole`, and `NameError`, cleared when `NewName` changes;
  - the new sub-category: `SubcategoryName` and `SubcategoryError`;
  - `Message` (the confirmation) and `Error` (a save failure), as the rule form has them.
- **`MainViewModel`** gets `AddCategoryCommand` and `AddSubcategoryCommand`. Each calls the config's method; a refusal goes to the field's error through `Texts.Refusal`. A success goes through the existing `ApplyUserChange`, the one place every change is saved and shown, so saving, the "not saving" state and recategorizing work as for rules. Then it refreshes the card's rows and the rule form's choices, and sets the message: the confirmation when saved, otherwise the session-only or save-failed text, as `Confirm` does for rules.
- **Display order in one place**: a small `CatalogOrder` helper (categories, and sub-categories with "Ostatné" last) used by the card and by `RuleFormViewModel`.
- **`Texts`**: "Názov", "Počíta sa ako", "Pridať kategóriu", "Pridať podkategóriu", "Pridať", "vlastná", the three roles and their tags, the help text, `CategoryAdded(name)`, `SubcategoryAdded(name, category)`, and the four save texts. "Kategórie" and "Zrušiť" exist already.

### The window, in `DocuSpend.Desktop`
Rebuilt after the first screenshot review (see Risks): the categories moved from a card under the rule form to a screen of their own.
- **A third screen.** `AppScreen.Categories` and the pill "Kategórie" join the header's switch; `MainViewModel.ShowsCategories` shows `CategoriesScreen`, a grid of two cards (`2*` and `3*`). The rules screen is back as #21 left it.
- **The list card**: the heading "Kategórie" with "Nová kategória" beside it, the "Zmeny pravidiel sa teraz neukladajú." note while `RulesNotSaving`, and a `ListBox` of the categories (its selection is `Categories.SelectedRow`), styled as the statement screen's category list, with a "vlastná" tag on the user's own.
- **The right card** holds two panels, one shown at a time: the details (`ScrollViewer`: name, role sentence, message, "Podkategórie" with an `ItemsControl` of rows, the field and "Pridať podkategóriu", and the reason under it; or the note for an uncategorized bucket) and the form for a new category (labelled fields like the rule form's: "Názov", "Počíta sa ako" with the help mark, "Pridať kategóriu" and "Zrušiť").
- **In the code-behind**: the name field takes the focus when the form opens; the list scrolls the chosen category into view; Enter in either name field adds, so the rule form's default button doesn't take the key.
- **The look**: `Border.tag` and `Border.tag.own` for "vlastná", `Border.subcategoryRow`, `TextBlock.fieldError`, `TextBlock.heading.plain` (a heading beside a button, without its bottom margin), and `Look.Padding.Tag` and `Look.Margin.FieldLabel`. Every `TextBox` centres its text and placeholder vertically (the placeholder sat high before). No new colour.

### Tests and screenshots
- Logic in `DocuSpend.Presentation.Tests` (`CategoriesTests.cs` and `CategoriesAddingTests.cs`, new), with the slice colours checked there. The window in `DocuSpend.Desktop.Tests` (`CategoriesWindowTests.cs`, new).
- New screenshots: `20-categories` to `25-categories-not-saving` (step 4).

### Alternatives considered
- **Putting "Ostatné" last inside the categorization** (inserting before it when adding): lost. It would change the order the categorization holds, and with it the order the saved file has for built-in categories with user sub-categories. Sorting for display gives the same picture with nothing saved differently.
- **Moving the user's categories before "Nezaradené" in the catalog**: lost for the same reason, and it would shift the slice colours' numbering for nothing.
- **A card under the rule form, listing every category with its sub-categories as pills**: built first, and lost at the screenshot review as cluttered and confusing (see Risks).
- **A list that opens one category in place, under the rule form**: offered at the review, and lost to a screen of its own, which has room for both the list and the details.
- **A pop-up box for adding**: lost in the spec.
- **A `TreeView`** for categories and sub-categories: lost. Its expand arrows and selection aren't wanted, and its Fluent template would need restyling in both looks. A `ListBox` of names beside the chosen category's details is plain, and styled by the look already.
- **Commands on `CategoriesViewModel` with a callback into `MainViewModel`**: lost. The rule commands live on `MainViewModel`, next to `ApplyUserChange` and the store, and the categories' commands follow them.

### Layout
New files:
- `src/DocuSpend.Presentation/CategoriesViewModel.cs`, `CatalogOrder.cs`
- `tests/DocuSpend.Presentation.Tests/CategoriesTests.cs`, `CategoriesAddingTests.cs`
- `tests/DocuSpend.Desktop.Tests/CategoriesWindowTests.cs`

Changed:
- `src/DocuSpend.Presentation/MainViewModel.cs`, `RuleFormViewModel.cs`, `Texts.cs`
- `src/DocuSpend.Desktop/MainWindow.axaml`, `MainWindow.axaml.cs`, `Look/Look.axaml`
- `tools/DocuSpend.Screenshots/Screens.cs`
- `tests/DocuSpend.Presentation.Tests/ScreensTests.cs`; `tests/DocuSpend.Desktop.Tests/ScreensWindowTests.cs`, `MainWindowTests.cs` (three screens)
- docs: `README.md`, `AGENTS.md`, `work/025-own-categories/`

## Prerequisites
- #21 is merged into `main` (it is), and this branch was made from it.
- The maintainer reviews the screenshots (step 4) and does the hand check with a real statement (step 5).

## Steps
Test-first for logic; one commit per step, committed when it is done and every test passes.

1. **The list of categories** (`CatalogOrder`, `CategoriesViewModel.Rows`, `Texts`). Tests:
   - with no user categories: the 14 built-in categories in order, then "Nezaradené výdavky" and "Nezaradené príjmy"; each with its role tag; sub-categories with "Ostatné" last; no add for the uncategorized buckets;
   - with user categories and sub-categories (built through `AddCategory` and `AddSubcategory`): the user's own after the built-in and before "Nezaradené", marked as own, "Ostatné" still last;
   - the rule form's sub-categories with "Ostatné" last;
   - the new texts, letter for letter.

   Sets the intent to `in-progress`.
2. **Adding** (`MainViewModel` commands, `CategoriesViewModel` fields, `RuleFormViewModel` taking a new catalog). Tests:
   - a category of each role is added and listed, with its confirmation; the name field empties and the role stays;
   - names trimmed; cut at 40; empty, spaces only, and repeated names (case, diacritics, "Nezaradené výdavky", a user's own) refused with the reason, nothing added, the name kept; the reason cleared on a change;
   - adding to a built-in and to a user category; the refusals, including "Ostatné" and "ostatne"; the same name under another category or as its category's name allowed;
   - the rule form offers the new category and sub-category at once, for a new rule and an edited one, and keeps a half-typed rule's choices;
   - a rule targeting a new sub-category: its count, its row "<category> › <sub-category>", and the statement's categories;
   - a new spending, excluded and income category filled by a rule: the pie, "Výdavky spolu" and the income follow its role; a new category with no rule changes nothing in the statement;
   - built-in slice colours are the same before and after adding a spending category;
   - after a restart (a new `MainViewModel` on the same file): the category, its sub-category, its role and the rule are back, and the statement is categorized the same;
   - saving fails, and saving is off for the session: the texts for a category and for a sub-category, and the category still usable.
3. **The card in the window**, as first planned: under the rule form on "Pravidlá". Built, then replaced in step 4 (see Risks).
4. **Screenshots.** **Stop for review.** At the first review the maintainer found the card cluttered and confusing and chose a screen of its own. Rebuilt in this step:
   - logic: `AppScreen.Categories` and `ShowsCategories`; `CategoriesViewModel` with the chosen category and the form shown on request, instead of a line per row. Tests: the first category chosen at start; choosing another empties the field, its reason and the message, and closes the form; "Nová kategória" and "Zrušiť"; adding chooses the new category; the uncategorized buckets take no sub-category; "Ostatné" marked; the screen's state kept across another statement and a switch of screens; the three screens in order;
   - the window: the "Kategórie" screen, the rules screen back as #21 left it. Window tests (`CategoriesWindowTests`): the third pill and only this screen's cards; the list, the first category's details, "Ostatné" described; an uncategorized bucket's note and no field; the form with the cursor in "Názov", "Zrušiť"; adding a category chooses it and brings it into view; refusals under their fields; adding a sub-category; the 40-character fields; 30 categories scroll inside the list with nothing to scroll in the window; the note while saving is off; the screen kept across a switch; Enter adds and adds no rule; a new sub-category in the rule form with a chosen target kept. The two #21 window tests that counted two screens, or looked for "Kategórie" anywhere on the rules screen, now count three and look only inside the rules screen;
   - screenshots `20-categories`, `21-own-category`, `22-new-category`, `23-subcategory-refused`, `24-uncategorized-chosen`, `25-categories-not-saving`.

   **Stop for review again:** the maintainer looks at these in both looks. The approved pictures go to `work/025-own-categories/screenshots/`.
5. **Hand check.** **Stop:** the maintainer opens a real statement, adds a category and a sub-category, adds a rule that sends transactions there, checks "Výpis", restarts DocuSpend, and finds the category, the sub-category and the rule as they left them.
6. **Docs and done**, with `finish-item`:
   - `README.md` ("What works today", and what is next) and `AGENTS.md` (the three screens);
   - the spec and this plan updated for anything the build changed;
   - the intent becomes `done`;
   - the follow-up issue for renaming and deleting your own categories, as the intent decided, is left to the maintainer to open; the maintainer asked to finish this item without starting another.

## Risks and unknowns

- **Found in the screenshot review: the card under the rule form was cluttered and confusing.** Every category with all its sub-categories as pills, a button and tags on every row, in a third of the window; half-scrolled rows left stray "Ostatné" pills under the card's messages, and the "Názov" placeholder sat high in its field. The maintainer chose a screen of its own with a list and one category's details. The placeholder is centred for every text box.
- **A refreshed rule form could lose its choice**, if the new category lists hold new objects and the combo boxes compare by reference. The form reselects by identifier; tests cover a half-typed rule, a rule being edited, and the window.
- **A list replaced on refresh lets go of its selection** for a moment, which would look like the user choosing nothing. `CategoriesViewModel` ignores that while it refreshes, and keeps the chosen category by identifier.
- **Found in the build: a screen never shown reports its text boxes as visible** when searched through the logical tree. A #21 window test that looked for the one visible text box in the whole window now looks only inside the rules screen.
- **Found in the build: Enter in a name field added a rule**, through the rule form's default button. The window handles Enter in both name fields first.
- **Found in the hand check:** nothing; the maintainer added a category and a sub-category, sent a real statement's transactions there with a rule, and found them all after a restart.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| The switch offers three screens; "Kategórie" shows only its cards | `CategoriesWindowTests`, `ScreensWindowTests`, `ScreensTests` (step 4) |
| The rules and statement screens are as before | `ScreensWindowTests`, `RulesWindowTests`, `FitWindowTests`, unchanged in substance (every step) |
| Built-in list in order, then the two "Nezaradené"; the first chosen with its details | `CategoriesTests` (step 1); `CategoriesAddingTests`, `CategoriesWindowTests` (step 4) |
| A built-in category's role sentence and sub-categories, "Ostatné" last and described | `CategoriesTests` (step 1); `CategoriesWindowTests` (step 4) |
| An uncategorized bucket: the note, no field | `CategoriesAddingTests`, `CategoriesWindowTests` (step 4) |
| More categories than fit: the list scrolls, the window doesn't | `CategoriesWindowTests` with 30 categories (step 4) |
| "Nová kategória" shows the form with the cursor in "Názov"; "Zrušiť" and choosing a category close it | `CategoriesAddingTests`, `CategoriesWindowTests` (step 4) |
| Adding "Šport": its place, tag, chosen and in view, details, confirmation, role kept | `CategoriesAddingTests` (steps 2, 4); `CategoriesWindowTests` (step 4) |
| "Príjmy" and "Mimo výdavkov" role sentences | `CategoriesAddingTests` (steps 2, 4); `CategoriesTests`, texts (step 1) |
| "  Šport  " named "Šport" | `CategoriesAddingTests` (step 2) |
| Empty or spaces-only name refused; the form stays | `CategoriesAddingTests` (step 2); `CategoriesWindowTests` (step 4) |
| Repeated names refused (case, diacritics, "Nezaradené výdavky", the user's own), name kept | `CategoriesAddingTests` (step 2); `CategoriesWindowTests` (step 4) |
| A reason disappears when the name changes | `CategoriesAddingTests` (step 2) |
| The help mark's tooltip text | `CategoriesTests`, texts (step 1); `CategoriesWindowTests` (step 4) |
| Adding "Obedy v práci" to "Stravovanie": before "Ostatné", own, field empty, confirmation | `CategoriesAddingTests` (steps 2, 4); `CategoriesWindowTests` (step 4) |
| Adding to a user category | `CategoriesAddingTests` (step 2) |
| Sub-category refusals: empty, "Ostatné", "ostatne", "Potraviny"; name kept | `CategoriesAddingTests` (step 2); `CategoriesWindowTests` (step 4) |
| "Potraviny" under "Bývanie" and "Šport" under "Šport" allowed | `CategoriesAddingTests` (step 2) |
| Choosing another category empties the field, its reason and the message | `CategoriesAddingTests` (step 4) |
| Enter adds the category or the sub-category, and no rule | `CategoriesWindowTests` (step 4) |
| New choices in the rule form at once, new and edited rule, "Ostatné" last | `CategoriesAddingTests`, `CategoriesTests` (steps 1, 2) |
| A half-typed rule keeps its choices | `CategoriesAddingTests` (step 2); `CategoriesWindowTests` (step 4) |
| A rule into a new sub-category: count, rule row, "Výpis" | `CategoriesAddingTests` (step 2) |
| A new spending category gets a slice; built-in colours unchanged | `CategoriesAddingTests` (step 2) |
| A new excluded category: out of the pie and "Výdavky spolu", still in "Kategórie" | `CategoriesAddingTests` (step 2) |
| A new income category isn't spending | `CategoriesAddingTests` (step 2) |
| A new category with no rule: not on "Výpis", categorization unchanged | `CategoriesAddingTests` (step 2) |
| Back after a restart, with its rule | `CategoriesAddingTests` with a second `MainViewModel` (step 2) |
| A failed save: the texts, and the category still usable | `CategoriesAddingTests`, store folder blocked (step 2) |
| Saving off: the note and the session-only texts | `CategoriesAddingTests` (step 2); `CategoriesWindowTests` for the note (step 4) |
| Saved shape unchanged; earlier saved files still load | no change to `SavedRules`; `rules-v1.json` tests unchanged (every step) |
| Only the shared look; no literal values | `LookCheckTests` (every step) |
| Privacy checks pass, nothing new in the reviewed lists | `dotnet test` (every step) |
| Every new text in Slovak | `CategoriesTests`, letter for letter (steps 1, 4) |
| Screenshots approved | maintainer, step 4 |
| Hand check with a real statement | maintainer, step 5 |
| Edge: names cut at 40 characters | `CategoriesAddingTests` (step 2); `CategoriesWindowTests`, `MaxLength` (step 4) |
| Edge: "Sport" and "Šport" the same | `CategoriesAddingTests` (step 2) |
| Edge: spaces inside a name kept | `CategoriesAddingTests` (step 2) |
| Edge: many categories; colours repeat after 14 and never change | `CategoriesAddingTests`, `CategoriesWindowTests` (steps 2, 4) |
| Edge: many sub-categories scroll in the details | the details are a `ScrollViewer`; screenshot review (step 4) |
| Edge: adding while a rule is edited keeps the edit | `CategoriesAddingTests` (step 2) |
| Edge: a switch of screens keeps the screen | `CategoriesAddingTests`, `CategoriesWindowTests` (step 4) |
| Edge: opening another statement keeps the screen and an open form | `CategoriesAddingTests` (step 4) |
| Edge: a later successful save saves what failed before | `CategoriesAddingTests`: unblock the folder, add, restart (step 2) |
