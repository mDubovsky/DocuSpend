# Plan: Edit, delete and reorder rules

Spec: [spec.md](spec.md)

## Approach

**No new dependency, and no change to what is saved.** Everything is built from what DocuSpend already uses: Avalonia 11.3.22 with its Fluent theme, and CommunityToolkit.Mvvm. The saved file stays version 1, so `SavedRules`, `RuleStore` and `rules-v1.json` don't change. The privacy checks and the reviewed lists don't change either.

Verified on 26 October 2026, in a scratch project on .NET 10 referencing Avalonia 11.3.22 and Avalonia.Themes.Fluent 11.3.22 from the local package cache:
- **`Expander`** exists, with `IsExpanded` (false by default) and expanded and collapsed events. It was the fold-out section for "Vstavané pravidlá" until the screenshot review replaced it with two views (below).
- **`CheckBox`** exists. It is the tick boxes for transaction types.
- **`ToolTip`** has `Tip`, `IsOpen`, `ShowDelay`, `BetweenShowDelay`, `ShowOnDisabled` and opening and closing events, but **no option to open on keyboard focus**. The window opens the help mark's tooltip itself on keyboard focus (`ToolTip.SetIsOpen`) and closes it when focus leaves.

The work has three parts: the categorization, the screen logic and the window.

### The categorization, in `DocuSpend.Categorization`
`CategorizationConfig` gets the changes the editor needs. Each one returns a new configuration or a refusal, as the existing changes do:
- **`MaxUserPriority = 255`**, a public constant, so the form and the categorization can't disagree about the bound. `AddUserRule` and `SetUserRulePriority` refuse a number above it with `InvalidPriority`.
- **`AddUserRuleOnTop`** uses the lowest number any user rule has, capped at 255. A rule loaded with a higher number (spec: edge cases) then still loses to a new rule on top.
- **`ChangeUserRule(RuleId, Condition, SubcategoryId target, int priority)`** replaces a user rule's condition, target and priority, and keeps its identifier and creation order. It checks the input as `AddUserRule` does, and refuses an unknown rule with `UnknownRule`.
- **`RemoveUserRule(RuleId)`** removes a user rule, or refuses an unknown one with `UnknownRule`.
- **Loading doesn't change.** A saved rule numbered above 255 still loads, since files of the first shape allowed it. Only changes made from now on are capped.
- **Default rules** can't be passed to any of these. Only user rules have these identifiers, so `UnknownRule` covers that.

### The screen logic, in `DocuSpend.Presentation`
- **`RuleFormViewModel` offers six kinds.** `ConditionKind` becomes `Text`, `CounterpartyName`, `CounterpartyAccount`, `Amount`, `Type`, `VariableSymbol`, in the spec table's order, with its Slovak labels.
  - Text and counterparty name share the mode choice and the text field.
  - Account and symbol each get a field.
  - Types become a list of `TypeChoice` items (type, label, `IsChecked`), one tick box each. The single `SelectedType` goes.
  - `BuildCondition` makes each kind's condition, trimming spaces. Checking stays with `Condition.Validate()`, so the reasons are the existing `Texts.Refusal` ones.
- **Edit mode in the form:**
  - `Edit(Rule)` fills the fields from the rule's condition (the inverse of `BuildCondition`), its target and its priority. It sets `EditingRule` (the rule's identifier), which drives `IsEditing`, the heading ("Nové pravidlo" or "Upraviť pravidlo") and which buttons show.
  - `Priority` is a text field, parsed as a whole number from 1 to `MaxUserPriority` after trimming spaces. Anything else gives `Texts.Refusal(InvalidPriority)`, whose text becomes "Priorita musí byť celé číslo od 1 do 255."
  - `Reset()` goes back to an empty "Nové pravidlo".
  - Amounts go into the fields with a new `SlovakFormat.AmountInput(decimal)`: Slovak digits with a decimal comma, keeping the decimal's own places ("12,50") and no "€". A test checks that `TryParseAmount` reads every such text back to the same value, so saving unchanged changes nothing.
- **`MainViewModel`:**
  - **`Rules`** becomes rows in the order tried (`RulesInOrder()`, user rules only). Each `RuleRow` is a record with the rule's identifier, its number (kept for the order and tests, not shown), its condition (`Describe`), its target, `IsEditing` and `IsAskingToDelete`.
  - **`BuiltInRules`**: rows for the default rules in order, with no number and no commands.
  - **`RuleViews` and `SelectedRuleView`**: the two views of the rules card, "Moje pravidlá" first and chosen at start, like `Looks` and `SelectedLook`.
  - **Commands:**
    - `EditRuleCommand(row)` opens the rule in the form;
    - `SaveRuleCommand` applies `ChangeUserRule` and sets the form's message from the rule's count, as `AddRule` does;
    - `CancelEditCommand` resets the form;
    - `AskToDeleteCommand(row)` shows the question in that row;
    - `DeleteRuleCommand` applies `RemoveUserRule` and resets the form if it held that rule;
    - `KeepRuleCommand` takes the question back.

    Starting any other command (add, edit, ask on another row) takes back an open question first.
  - **Every change goes through the existing `ApplyUserChange`**, so it is saved before it is confirmed, exactly like adding.
  - **Messages:**
    - the form's messages after an edit reuse the texts for adding; the two "won't be kept" texts are new for edits;
    - `RulesListMessage` ("Pravidlo je odstránené.") and `RulesListError` (the deletion texts) belong to "Moje pravidlá" and are cleared by the next change.

    All new Slovak texts go in `Texts`, including `PriorityHelp`, the tooltip.

### The window, in `DocuSpend.Desktop`
- **The form card:** the heading binds to the form's heading. The kind choice shows the six kinds, each kind's fields show under it, and types show as a column of tick boxes. A "Priorita" row with its field and help mark shows while editing. Under the fields are "Pridať pravidlo", or "Uložiť zmeny" and "Zrušiť".
- **"Moje pravidlá":**
  - The heading is the two views as a segmented choice (`ListBox.segmented`, as the look switch), with a `tabs` class for heading-sized pills.
  - Each row has the condition, the target, and "Upraviť" and "Odstrániť" as small secondary buttons. "Odstrániť" opens the deletion pop-up (below). The row of the rule being edited is marked with the selected-row look the lists already use.
  - The list has a maximum height from the look and scrolls inside the card.
- **"Vstavané pravidlá"** is the card's second view, whose rows show the condition and the target.
- **The help mark** (by "Priorita" only) is a small focusable "?" with `ToolTip.Tip` bound to `Texts.PriorityHelp`. The window opens its tooltip on keyboard focus and closes it on focus loss (see above).
- **The deletion pop-up** (after the screenshot review): a panel over the whole window, a dimmed `scrim` from the window background and a `card dialog` in the middle with the question, the rule, "Odstrániť" and "Ponechať". It shows while `RuleToDelete` (the row asked about) is set. The window puts the keyboard focus on the box itself when it opens, with no focus mark and no button chosen, and Tab cycles inside the box.
- **Escape** takes back an open deletion question: a window key binding to `KeepRuleCommand`, which can run only while a question is open.
- **The look:** new styles (help mark, small row buttons, the heading pills, the marked row) and their sizes go in `Look/Look.axaml` with the existing resources; colours come from `Palette` only. If a new colour is needed, it goes in `Palette.cs` with its contrast test. The test that forbids literal values in screens stays as it is.
- **Screenshots:** `tools/DocuSpend.Screenshots` gets new screens:
  - `09-rules-list`: rules of every kind, two sharing a number;
  - `10-rule-delete-question`;
  - `11-rule-editing`, with the help tooltip open;
  - `12-built-in-rules`, the second view chosen.

  `05-rule-added` and `07`/`08` change too, since the list now shows buttons.

### Alternatives considered
- **Editing inside the row** instead of in the form: lost in the spec (decisions). It would duplicate every field in a narrow card.
- **A separate confirmation window, or a flyout, for deleting:** lost in the spec. A window needs its own look and focus handling, and a flyout is a popup that headless tests and screenshots handle less reliably than a row.
- **A `ListBox` for the rules:** it would add a selection that means nothing here and fights the buttons in the row. `ItemsControl`, as today, stays.
- **Waiting for a tooltip option in Avalonia 12** ([#14](https://github.com/mDubovsky/DocuSpend/issues/14)): not needed. Opening it ourselves is a few lines and goes away when that option exists.
- **Capping the priority only in the form:** then the categorization could still be given 300 by other code later (the rules screen of #21, rule packs). One constant in the categorization keeps one rule.
- **Refusing to load saved rules numbered above 255:** it would call a valid file of the first shape damaged and move it aside, losing nothing but scaring the user for no reason.

### Layout
No new folders. Changed files:
- `src/DocuSpend.Categorization/CategorizationConfig.cs`
- `src/DocuSpend.Presentation/RuleFormViewModel.cs`, `MainViewModel.cs`, `Texts.cs`, `SlovakFormat.cs`
- `src/DocuSpend.Desktop/MainWindow.axaml`, `MainWindow.axaml.cs`, `Look/Look.axaml`, perhaps `src/DocuSpend.Presentation/Look/Palette.cs`
- `tools/DocuSpend.Screenshots/Screens.cs`
- tests in `tests/DocuSpend.Categorization.Tests` (`ConfigRuleTests.cs`), `tests/DocuSpend.Presentation.Tests` (`RuleFormViewModelTests.cs`, `MainViewModelTests.cs`, `SlovakFormatTests.cs`, `TextsTests.cs`) and `tests/DocuSpend.Desktop.Tests` (`RulesWindowTests.cs`)
- docs: `docs/glossary.md`, `AGENTS.md` if needed, `README.md` if it lists features, `work/024-edit-rules/`

## Prerequisites
- #13 is merged into `main` (it is), and this branch was made from it.
- The maintainer reviews the screenshots (step 7) and does the hand check with a real statement (step 8).

## Steps
Test-first for logic; one commit per step, each after the maintainer approves it.

1. **Change and remove user rules, and cap the priority** (`CategorizationConfig`). Tests first:
   - `ChangeUserRule` keeps the identifier and creation order and changes the categorization of the samples;
   - it refuses invalid conditions, unknown targets, unknown or default rules, and numbers 0 and 256, accepting 1 and 255;
   - `RemoveUserRule`;
   - `SetUserRulePriority` and `AddUserRule` refuse 256;
   - adding on top over a loaded rule numbered 300 gives 255 and wins;
   - ties between equal numbers stay newest-first after a change.

   Sets the intent to `in-progress`.
2. **Six kinds in the form** (`RuleFormViewModel`, the window's form card). Tests:
   - each kind and option builds the condition the categorization would make, and categorizes every valid sample the same as that rule added directly;
   - each missing value gives its reason;
   - several ticked types make one condition.

   Window test: every kind's fields show for that kind only.
3. **The list in the order tried, and the help mark** (`MainViewModel.Rules`, the window). Tests:
   - the order for numbers 3, 1, 1;
   - a new rule is first;
   - the help text;
   - the tooltip opens on keyboard focus in the window.
4. **Editing** (form edit mode, `Edit`/`Save`/`Cancel`, `AmountInput`). Tests:
   - the form shows every kind's values exactly, and saving unchanged changes nothing;
   - changing to each kind;
   - invalid input and priorities;
   - the count message;
   - the "won't be kept" texts while saving is off and after a failed save;
   - the restart round trip through `RuleStore`;
   - editing another rule, and a half-typed new rule, are handled as the spec's edge cases say.

   Window test: the heading, the fields, the priority row and the marked row.
5. **Deleting** (the question, `Delete`/`Keep`, Escape). Tests:
   - asking doesn't delete;
   - keeping and Escape close the question;
   - deleting recategorizes, shows "Pravidlo je odstránené.", saves, and is gone after a restart;
   - deleting the rule in the form resets it;
   - deleting the last rule, then adding, gives 1;
   - the deletion texts while saving is off and after a failed save.
6. **"Vstavané pravidlá"** (`BuiltInRules`; after the screenshot review, the two views). Tests:
   - "Moje pravidlá" at start, and switching both ways;
   - every default rule in order, with no number;
   - no buttons;
   - no change to a user rule changes the default rules.

   Also a timing test: with 200 user rules, an edit and a deletion each finish in under a second in the view model.
7. **Screenshots.** **Stop for review:** the maintainer compares every screen, in both looks, against `work/011-modern-look/screenshots/` and approves the new ones. Fixes to the look go in this step.
8. **Hand check.** **Stop:** the maintainer edits, renumbers and deletes rules with a real statement open, restarts and finds them as left; and checks the tooltip on the screen if the screenshot couldn't show it (see risks).
9. **Docs and done:**
   - the glossary's **Priority** gets the 1 to 255 bound, and that built-in rules count as 256 in the tooltip;
   - `README.md` and `AGENTS.md` change only where they describe the rules screen;
   - the spec is updated for anything the build changed;
   - the intent becomes `done`.

## Risks and unknowns
- **A tooltip is a popup, which a headless screenshot of the window may not show.** Fallback: the window test checks the tooltip's text and that it opens. The screenshot shows the form in edit mode without it, and the maintainer checks the tooltip on screen in the hand check.
- **The Fluent `CheckBox` (and the `Expander`, before the review replaced it) may not follow the palette** in one of the looks, like the scroll bars in [#19](https://github.com/mDubovsky/DocuSpend/issues/19). Fallback: style their parts in `Look.axaml` from the palette; the screenshot review catches what is missed.
- **Changing the list's order breaks tests that assumed creation order** (from #13). They change with step 3, because the spec changes the order. The saved file's order is unaffected (creation order, as before).
- **Escape inside a text box** goes to the text box first. With the pop-up, the focus is in the box while the question is open, so this no longer arises.
- **The row's buttons may not fit a narrow card** with long conditions. The condition and the target are cut with "…" first, and the screenshot review checks the default window size.
- **Found in the build: the Fluent `Expander` kept Fluent's colours.** Its header was grey in the light look and black in the dark one. It was styled from the palette, then removed when the review replaced it with two views.
- **Found in the build: a rule list taller than its card centred itself.** A `ScrollViewer` with a maximum height in a taller space is centred by default; the rule lists are pinned to the top.
- **Found in the screenshot review: numbers, the fold-out section and the question in the row.** The maintainer asked for priority numbers only in the form, for the rules card to switch between "Moje pravidlá" and "Vstavané pravidlá" with pills in its heading, and for the deletion question to be a pop-up box with nothing chosen at start. The spec, the intent and this plan describe what was built. The help mark by "Moje pravidlá" went with the numbers.
- **Found in the hand check:** nothing; the maintainer approved it as built.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| Each of six kinds and options added through the form builds the right rule, is described as in the table, and categorizes samples `01`–`05` as when added directly | `RuleFormViewModelTests`, `MainViewModelTests` (step 2) |
| Each kind's missing value gives its reason and adds nothing | `RuleFormViewModelTests` (step 2) |
| A saved and loaded rule of each kind shows its values exactly in "Upraviť", and saving it unchanged leaves it as it was | `MainViewModelTests` through `RuleStore`, `SlovakFormatTests` for `AmountInput` (step 4) |
| "Upraviť" shows the heading, the values, the priority, "Uložiť zmeny" and "Zrušiť" | `MainViewModelTests`; `RulesWindowTests` (step 4) |
| Changing text, mode and target categorizes as if added so, with the count message | `MainViewModelTests` (step 4) |
| Changing to each of the other five kinds | `MainViewModelTests` (step 4) |
| Invalid edit input gives the adding reason and changes nothing (rule, statement, file) | `MainViewModelTests` (step 4) |
| Priorities "0", "-1", "1,5", "abc", "", "256" refused with the new text; "1" and "255" accepted | `RuleFormViewModelTests`, `ConfigRuleTests` (steps 1, 4) |
| An edited rule keeps its identifier and creation order; ties stay newest first | `ConfigRuleTests` (step 1), `MainViewModelTests` (step 4) |
| "Zrušiť" changes nothing and empties the form | `MainViewModelTests` (step 4) |
| An edited rule is back as edited after a restart | `MainViewModelTests` with a second view model on the same file (step 4) |
| Numbers 3, 1, 1 are listed third, second, first, with no numbers | `MainViewModelTests`; `RulesWindowTests` (step 3) |
| A renumbered rule moves, and the categorization follows | `MainViewModelTests` (step 4) |
| A rule added through the form is first | `MainViewModelTests` (step 3) |
| The help mark by "Priorita" shows the tooltip on hover and keyboard focus | `RulesWindowTests` (step 3); hand check if not visible in screenshots |
| "Odstrániť" asks and doesn't delete | `MainViewModelTests`; `RulesWindowTests` (step 5) |
| "Ponechať" and Escape close the pop-up and keep the rule | `RuleEditorTests`; `RuleEditorWindowTests` (step 5, and after the review) |
| Deleting removes, recategorizes, shows "Pravidlo je odstránené." and isn't back after a restart | `MainViewModelTests` (step 5) |
| Deleting the rule in the form resets the form | `MainViewModelTests` (step 5) |
| Deleting the last rule shows "Zatiaľ žiadne pravidlá." and the next rule gets 1 | `MainViewModelTests` (step 5) |
| Every edit, renumbering and deletion is saved before its confirmation | `MainViewModelTests`: the file holds the change when the message is set (steps 4, 5) |
| A folder that can't be written: edit and deletion apply with the failed-save texts, and the next change saves all | `MainViewModelTests` with a file where the folder should be (steps 4, 5) |
| Saving off: the "only until closed" texts, and the file unchanged | `MainViewModelTests` with a newer-version file (steps 4, 5) |
| A file saved after edits and deletions is of the same shape; the first-shape file still loads | `MainViewModelTests` reading the file with `SavedRules`; the existing `FirstShapeTests`, unchanged |
| "Moje pravidlá" at start; "Vstavané pravidlá" lists every default rule in order, no number, no buttons; switching back | `RuleEditorTests`; `RuleEditorWindowTests` (step 6) |
| No change to user rules changes the default rules | `ConfigRuleTests` (step 1), `MainViewModelTests` (step 6) |
| All privacy checks pass with no new entries | `dotnet test` (every step) |
| No new text is in English | `TextsTests`: every new text equals the Slovak quoted in the spec, letter for letter (each step that adds texts) |
| Hand check: edit, renumber, delete with a real statement, restart, find them as left | maintainer, step 8 |
| Screenshots approved in both looks | maintainer, step 7 |
| Edge: a saved rule above 255 loads, shows its number in the form, wins over built-in, and needs 1–255 to save | `ConfigRuleTests`, `MainViewModelTests` (steps 1, 4) |
| Edge: 200 rules scroll in the card, and an edit and a deletion are quick | `MainViewModelTests` timing (step 6); screenshot review for scrolling (step 7) |
| Edge: long texts cut with "…" | screenshot review (step 7) |
| Edge: "Upraviť" over a half-typed rule, another "Upraviť" while editing, " 2 ", another statement opened while editing, two identical rules | `MainViewModelTests`, `RuleFormViewModelTests` (step 4) |
