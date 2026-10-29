# Spec: Create your own categories and sub-categories

Intent: [intent.md](intent.md)

## Behavior

### The "Kategórie" screen
- With a statement open, the header's switch ([#21](../021-rules-screen/spec.md)) offers three screens: **"Výpis"**, **"Pravidlá"** and **"Kategórie"**. Choosing "Kategórie" shows the categories; the other two screens don't change. Opening another statement shows "Výpis", as it does from "Pravidlá".
- The screen has two cards side by side: on the left, the **list of categories**; on the right, the **chosen category's details**, or the **form for a new category** while it is open.
- **The list** has the heading "Kategórie" with the button **"Nová kategória"** beside it, then one row per category, showing its name, and the tag **"vlastná"** when it is the user's own. The order: the built-in categories in their usual order, then the user's own in the order they were added, then "Nezaradené výdavky" and "Nezaradené príjmy". A list longer than its card scrolls inside it. Choosing a row shows that category's details; the first category is chosen when DocuSpend starts.
- **The details** show the category's name (with "vlastná" for the user's own), its role in a sentence ("Počíta sa ako výdavky", "Počíta sa ako príjmy" or "Nepočíta sa do výdavkov"), then "Podkategórie" with its sub-categories, one per line, in the order they were added, with "Ostatné" always last. "Ostatné" says what it is for: "pre všetko, čo nepatrí inam". The user's own sub-categories have the "vlastná" tag, also inside built-in categories.
- Under the sub-categories is a field with the placeholder **"Nová podkategória"** and the button **"Pridať podkategóriu"**.
- For "Nezaradené výdavky" and "Nezaradené príjmy", the details say "Sem patria transakcie, ktoré nezaradilo žiadne pravidlo. Táto kategória nemá podkategórie." and offer no field.
- The same sub-category order holds in the rule form's "Podkategória" list: "Ostatné" is last.

### Adding a category
- "Nová kategória" shows the form in place of the details: the heading "Nová kategória", **"Názov"** (a text field, with the cursor in it), **"Počíta sa ako"** (a choice of "Výdavky", "Príjmy" and "Mimo výdavkov", with "Výdavky" chosen at first) with a help mark, and the buttons **"Pridať kategóriu"** and **"Zrušiť"**.
- The help mark's tooltip says: "Výdavky: platby sa započítajú do výdavkov a do grafu výdavkov. Príjmy: prichádzajúce platby, nikdy sa nepočítajú ako výdavky. Mimo výdavkov: napríklad prevody medzi vlastnými účtami alebo investície; do výdavkov sa nezapočítajú."
- "Pridať kategóriu", or Enter in the name field, adds the category with the typed name (spaces around it removed) and the chosen role. It gets an "Ostatné" sub-category without being asked. The form closes, the new category is chosen in the list and scrolled into view, and its details say **"Kategória „<name>“ je pridaná."**. The role chosen stays chosen for the next new category.
- A name that can't be used is refused, and nothing is added; the reason shows under the name field, the form stays open and the typed name stays, to be corrected:
  - an empty name, or one of spaces only: "Zadajte názov.";
  - a name that another category already has, ignoring case, diacritics and spaces around it, including "Nezaradené výdavky" and "Nezaradené príjmy": "Taký názov už existuje.".
- The reason disappears when the user changes the name.
- "Zrušiť" closes the form, adds nothing, and shows the chosen category's details again. Choosing a category in the list while the form is open closes it too.

### Adding a sub-category
- "Pridať podkategóriu", or Enter in its field, adds the typed sub-category to the chosen category, built-in or the user's own. It shows before "Ostatné", the field empties, and the details say **"Podkategória „<name>“ je pridaná do kategórie „<category>“."**
- A name that can't be used is refused as for a category, with the reason under the field and the name kept: empty, "Zadajte názov."; the same as another sub-category of the same category, "Ostatné" included, "Taký názov už existuje.". A sub-category may share its name with a sub-category of another category, or with a category.
- Choosing another category empties the field, its reason and the message.

### Using what was added
- A new category, and a new sub-category, are a choice in the rule form at once: in "Kategória" and "Podkategória", for a new rule and for a rule being edited. A rule half typed in the form keeps its chosen category and sub-category.
- In the rules card, a rule that sends transactions to a new sub-category shows it as any other target, "<category> › <sub-category>".
- Adding a category or sub-category doesn't change how the open statement is categorized: a new one starts empty. Only a rule that targets it moves transactions there.
- On the statement screen, a new category shows only once a rule has put transactions in it, like every category today. Then it behaves like a built-in one with the same role: a spending category has its own slice in the pie and its place in the legend, "Kategórie" and its transactions; an income category counts as income; an excluded category stays out of the pie and of "Výdavky spolu".
- A new spending category's colour in the pie follows the built-in ones and doesn't change the colour of any built-in category. It is the same in every statement.

### Saving
- A new category or sub-category is saved as soon as it is added, the same way as a rule ([#13](../013-keep-rules/spec.md)), and is back after DocuSpend restarts, with rules that point at it still working.
- When it can't be saved, it still works until DocuSpend closes, and instead of the confirmation the details say:
  - after a failed save: "Kategóriu sa nepodarilo uložiť, preto po zatvorení DocuSpend zmizne. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova." (for a sub-category: "Podkategóriu sa nepodarilo uložiť, …", the rest the same);
  - while changes aren't being saved this session (the saved rules couldn't be opened, or are from a newer DocuSpend, #13): "Kategória platí len do zatvorenia DocuSpend." ("Podkategória platí …" for a sub-category).
- While changes aren't being saved, the list also shows "Zmeny pravidiel sa teraz neukladajú." under its heading, as the rules card does.
- A rule that targets a category whose save failed is saved with that category's next successful save, as rules and categories are saved together today.

## Screens

The "Kategórie" screen at the default window size, with "Stravovanie" chosen just after a sub-category was added.

```
+--------------------------------------------------------------------------------+
| [Otvoriť výpis…] 1. 3. 2026 – 31. 3. 2026  (Výpis|Pravidlá|Kategórie)  Vzhľad (…)|
+------------------------------+-------------------------------------------------+
| Kategórie   [Nová kategória] | Stravovanie                                     |
|  Bývanie                     | Počíta sa ako výdavky                           |
|  Nákupy online               | [Podkategória „Obedy v práci“ je pridaná do …]  |
| (Stravovanie)                | Podkategórie                                    |
|  Auto-moto                   |  Potraviny                                      |
|  ...                         |  Reštaurácie a kaviarne                         |
|  Investície                  |  Obedy v práci                        vlastná   |
|  Šport              vlastná  |  Ostatné  pre všetko, čo nepatrí inam           |
|  Dane               vlastná  | [Nová podkategória          ] [Pridať podkat.]  |
|  Nezaradené výdavky          |                                                 |
|  Nezaradené príjmy           |                                                 |
|  (scrolls inside the card)   |                                                 |
+------------------------------+-------------------------------------------------+
```

The right card after "Nová kategória", with a refused name:

```
| Nová kategória                                  |
|  Názov          [Bývanie              ]         |
|                 Taký názov už existuje.         |
|  Počíta sa ako  [Výdavky v] (?)                 |
|                 [Pridať kategóriu] [Zrušiť]     |
```

No pop-up boxes: everything happens in the two cards.

## Acceptance criteria

The screen:
- [ ] Given a statement open, then the header's switch offers "Výpis", "Pravidlá" and "Kategórie"; choosing "Kategórie" shows its two cards and neither of the other screens.
- [ ] The rules screen has only the form and the rules card, as before this item; the statement screen is unchanged.
- [ ] Given no categories of the user's own, then the list shows the 14 built-in categories in their usual order, then "Nezaradené výdavky" and "Nezaradené príjmy", none tagged "vlastná"; the first one is chosen and its details show.
- [ ] Given a built-in category chosen, then its details show its role sentence and its sub-categories with "Ostatné" last and described as "pre všetko, čo nepatrí inam".
- [ ] Given "Nezaradené výdavky" or "Nezaradené príjmy" chosen, then the details say what it holds and offer no field for a sub-category.
- [ ] Given more categories than fit, then the list scrolls inside its card and the window doesn't scroll at the default size.

Adding a category:
- [ ] When the user chooses "Nová kategória", then the form shows in place of the details, with the cursor in "Názov"; "Zrušiť" brings the details back and adds nothing; choosing a category does too.
- [ ] Given the name "Šport" and "Výdavky", when the user chooses "Pridať kategóriu", then "Šport" is listed after the built-in categories and before "Nezaradené výdavky", tagged "vlastná", chosen and in view, with the details showing "Počíta sa ako výdavky", the one sub-category "Ostatné" and "Kategória „Šport“ je pridaná."; "Výdavky" is still chosen in the form for next time.
- [ ] The same with "Príjmy" and with "Mimo výdavkov" gives "Počíta sa ako príjmy" and "Nepočíta sa do výdavkov".
- [ ] Given "  Šport  ", then the category is named "Šport".
- [ ] Given an empty name, or spaces only, then "Zadajte názov." shows under the field, the form stays and nothing is added.
- [ ] Given "Bývanie", "bývanie", "Byvanie", "Nezaradené výdavky", or the name of a category the user added earlier, then "Taký názov už existuje." shows under the field, nothing is added and the typed name stays.
- [ ] Given a refused name, when the user changes the name, then the reason disappears.
- [ ] The help mark by "Počíta sa ako" shows the tooltip text above.

Adding a sub-category:
- [ ] Given "Stravovanie" chosen and "Obedy v práci" typed, when the user chooses "Pridať podkategóriu", then "Obedy v práci" shows before "Ostatné" tagged "vlastná", the field empties, and the details say "Podkategória „Obedy v práci“ je pridaná do kategórie „Stravovanie“."
- [ ] The same works on a category of the user's own.
- [ ] Given an empty name, "Ostatné", "ostatne", or "Potraviny" under "Stravovanie", then the reason shows under the field ("Zadajte názov." or "Taký názov už existuje."), the name stays and nothing is added.
- [ ] Given "Potraviny" under "Bývanie", or "Šport" under "Šport", then it is added.
- [ ] Given a name typed or a reason shown, when the user chooses another category, then the field, the reason and the message are empty.
- [ ] Enter in the name field adds the category, and Enter in the sub-category field adds the sub-category; neither adds a rule.

Using what was added:
- [ ] Given a new category with a new sub-category, then both are choices in the rule form's "Kategória" and "Podkategória" at once, for a new rule and for a rule opened with "Upraviť", with "Ostatné" last.
- [ ] Given a rule half typed with a category and sub-category chosen, when the user adds a category or sub-category, then the form still shows what was typed and chosen.
- [ ] Given a new sub-category, when the user adds a rule targeting it that catches transactions of the open statement, then the form says how many, the rules card shows "<category> › <sub-category>", and on "Výpis" the category is in "Kategórie" with those transactions.
- [ ] Given a new spending category that a rule fills, then the pie has a slice for it, and every built-in category keeps the colour it had before the category was added.
- [ ] Given a new excluded category that a rule fills, then its transactions are out of the pie and out of "Výdavky spolu", and the category is still listed in "Kategórie".
- [ ] Given a new income category that a rule fills, then its transactions are not counted as spending.
- [ ] Given a new category with no rule pointing at it, then "Výpis" doesn't list it and the categorization of the open statement is the same as before it was added.

Saving:
- [ ] Given a new category with a sub-category and a rule targeting it, when DocuSpend restarts, then the category, its sub-category, its role and the rule are back, and the statement is categorized the same.
- [ ] Given saving fails, when the user adds a category, then the details say "Kategóriu sa nepodarilo uložiť, …", and the category is listed and can be chosen until DocuSpend closes. The same for a sub-category with "Podkategóriu sa nepodarilo uložiť, …".
- [ ] Given changes aren't being saved this session, then the list shows "Zmeny pravidiel sa teraz neukladajú.", and adding a category says "Kategória platí len do zatvorenia DocuSpend." ("Podkategória platí …" for a sub-category).
- [ ] What is saved has the same shape as before; the committed saved files of earlier versions still load.

Look, privacy and hand checks:
- [ ] The screen uses only the shared look; the look test finds no literal colour, size or spacing in the screens.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.
- [ ] Every new text on screen is in Slovak.
- [ ] The maintainer has approved screenshots of the "Kategórie" screen, in both looks, at the default window size: a built-in category with a sub-category of the user's own, a category of the user's own, the form with a refused name, a refused sub-category, an uncategorized bucket, and changes not being saved.
- [ ] Hand check (maintainer): with a real statement open, add a category and a sub-category, add a rule that sends transactions there, check "Výpis", restart DocuSpend, and find the category, the sub-category and the rule as they were left.

## Edge cases and errors

- **A long name.** The name fields take at most 40 characters; a longer paste is cut at 40. Names that don't fit their place are cut with "…", as long names are today; the full name shows in the details.
- **Names that differ only in diacritics or case** ("Sport" and "Šport") count as the same and are refused.
- **A name with spaces inside** ("Obedy v práci") is kept as typed; only spaces around it are removed.
- **A sub-category named like its category** ("Šport" under "Šport") is allowed: names have to be unique only among siblings.
- **Many categories of the user's own.** The list scrolls; the rule form's lists scroll as today. After the 14 slice colours are used, colours repeat, as they would for built-in categories; a category's colour never changes once it has one.
- **Many sub-categories.** The details scroll inside their card.
- **Adding while a rule is being edited** keeps the edit as it was; the new choices just appear in the form.
- **Switching to another screen and back** keeps the screen as it was: the chosen category, the typed sub-category name, the open form with its name and role, the message, and the list's scroll position.
- **Opening another statement** keeps the screen as it was too; categories don't depend on the statement.
- **Saving fails, then works.** The next successful save of anything (a rule or a category) saves the categories added before, as rules are saved today.
- **No statement open.** The switch, and so the "Kategórie" screen, appears only with a statement open (#21).

## Out of scope

- Renaming or deleting categories and sub-categories, and hiding built-in ones (a follow-up issue, see the intent).
- Changing a category's role after it is created.
- Choosing a category's colour.
- Creating a category from the rule form or from a transaction ([#22](https://github.com/mDubovsky/DocuSpend/issues/22)).
- Reordering categories or sub-categories.
- Esc to close the form, and reaching every control with the keyboard alone, as [#21](../021-rules-screen/spec.md) left keyboard use for a later accessibility item. Enter in a name field does add what is typed (see Decisions).
- Exporting, importing or sharing categories ([mvp.md](../../docs/mvp.md), future directions).

## Decisions

- **"Kategórie" is a screen of its own, the third pill in the header's switch**, decided by the maintainer at the screenshot review. The first build put a card under the rule form, listing every category with its sub-categories as pills and a button on each row; in a third of the window it was cluttered and confusing. A screen of its own has room for a plain list beside one category's details.
- **A list of names on the left, one category's details on the right**, so only one category's sub-categories show at a time and the list stays easy to scan.
- **The form for a new category shows only when asked for**, in place of the details, so the screen isn't taken up by fields most visits don't need.
- **The roles are offered as "Výdavky", "Príjmy" and "Mimo výdavkov"**, with a help mark, and described in the details as "Počíta sa ako výdavky", "Počíta sa ako príjmy" and "Nepočíta sa do výdavkov". "Excluded from spending" needs explaining, and "Mimo výdavkov" says what it does in two words.
- **"Výdavky" is chosen at first**, since most categories a user adds are for spending.
- **"Ostatné" is always last** among sub-categories, here and in the rule form, so a new sub-category doesn't end up after the catch-all, and it says what it is for, since every category has one.
- **The user's own categories come after the built-in ones and before "Nezaradené"**, so the built-in list looks as it always has, and the uncategorized buckets stay at the end as everywhere.
- **The two "Nezaradené" buckets are listed**, with a note instead of a field, so the list shows every category the user can meet on the statement screen.
- **Adding doesn't ask for confirmation**, though nothing added can be removed yet: a confirmation for every addition would slow down the common case, and renaming and deleting get their own issue.
- **Names are limited to 40 characters.** The longest built-in name has 32; longer names would crowd the legend, the lists and the rule card.
- **A refusal shows under its field; a confirmation or a failed save shows in the details**, where the user looks next.
- **Enter in a name field adds what is typed**, found in the build: the rule form's default button would otherwise take the key and add a rule.
- **The screen is called "Kategórie", as the statement screen's list of the month's categories is.** They are on different screens; the statement screen's list still shows only what the month holds.

## Open questions

None.
