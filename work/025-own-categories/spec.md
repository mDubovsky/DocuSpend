# Spec: Create your own categories and sub-categories

Intent: [intent.md](intent.md)

## Behavior

### The "Kategórie" card
- The rules screen ("Pravidlá", [#21](../021-rules-screen/spec.md)) gets a third card, **"Kategórie"**, in the left column under the "Nové pravidlo" form. It takes the column's remaining height, and its list scrolls inside it, as the rules card's list does. The form and the rules card don't change.
- The card lists **every category**, one row each, in this order: the built-in categories in their usual order, then the user's own in the order they were added, then "Nezaradené výdavky" and "Nezaradené príjmy".
- A category's row shows its **name**, its **role** as a small tag ("výdavky", "príjmy" or "mimo výdavkov"), the tag **"vlastná"** when it is the user's own, and a button **"Pridať podkategóriu"**. The two "Nezaradené" rows have no button and no sub-categories.
- Under the name, the category's **sub-categories** are shown as small pills that wrap onto more lines as needed, in the order they were added, with "Ostatné" always last. The user's own sub-categories are marked as their own, also inside built-in categories.
- The same order holds wherever sub-categories are chosen: in the rule form's "Podkategória" list, "Ostatné" is last too.

### Adding a category
- At the top of the card are the fields for a new category: **"Názov"** (a text field), **"Počíta sa ako"** (a choice of "Výdavky", "Príjmy" and "Mimo výdavkov", with "Výdavky" chosen at first) with a help mark, and the button **"Pridať kategóriu"**.
- The help mark's tooltip says: "Výdavky: platby sa započítajú do výdavkov a do grafu výdavkov. Príjmy: prichádzajúce platby, nikdy sa nepočítajú ako výdavky. Mimo výdavkov: napríklad prevody medzi vlastnými účtami alebo investície; do výdavkov sa nezapočítajú."
- "Pridať kategóriu" adds the category with the typed name (spaces around it removed) and the chosen role. It gets an "Ostatné" sub-category without being asked. The new row shows in the list, which scrolls to it; the name field is emptied, and the role stays as chosen.
- Under the fields, the card says **"Kategória „<name>“ je pridaná."**
- A name that can't be used is refused, and nothing is added; the reason shows under the name field and the typed name stays, to be corrected:
  - an empty name, or one of spaces only: "Zadajte názov.";
  - a name that another category already has, ignoring case, diacritics and spaces around it, including "Nezaradené výdavky" and "Nezaradené príjmy": "Taký názov už existuje.".
- The reason disappears when the user changes the name.

### Adding a sub-category
- "Pridať podkategóriu" opens a line under that category's pills: a text field for the name, focused, with the buttons **"Pridať"** and **"Zrušiť"**. Only one such line is open at a time; opening it on another category closes the first one and drops what was typed there.
- "Pridať" adds the sub-category to that category, built-in or the user's own, and closes the line. The new pill shows before "Ostatné". The message under the new category's fields at the top of the card says **"Podkategória „<name>“ je pridaná do kategórie „<category>“."**
- A name that can't be used is refused as for a category, with the reason under the field: empty, "Zadajte názov."; the same as another sub-category of the same category, "Ostatné" included, "Taký názov už existuje.". A sub-category may share its name with a sub-category of another category, or with a category.
- "Zrušiť" closes the line and adds nothing.

### Using what was added
- A new category, and a new sub-category, are a choice in the rule form at once: in "Kategória" and "Podkategória", for a new rule and for a rule being edited. A rule half typed in the form keeps its chosen category and sub-category.
- In the rules card, a rule that sends transactions to a new sub-category shows it as any other target, "<category> › <sub-category>".
- Adding a category or sub-category doesn't change how the open statement is categorized: a new one starts empty. Only a rule that targets it moves transactions there.
- On the statement screen, a new category shows only once a rule has put transactions in it, like every category today. Then it behaves like a built-in one with the same role: a spending category has its own slice in the pie and its place in the legend, "Kategórie" and its transactions; an income category counts as income; an excluded category stays out of the pie and of "Výdavky spolu".
- A new spending category's colour in the pie follows the built-in ones and doesn't change the colour of any built-in category. It is the same in every statement.

### Saving
- A new category or sub-category is saved as soon as it is added, the same way as a rule ([#13](../013-keep-rules/spec.md)), and is back after DocuSpend restarts, with rules that point at it still working.
- When it can't be saved, it still works until DocuSpend closes, and instead of the confirmation the card says:
  - after a failed save: "Kategóriu sa nepodarilo uložiť, preto po zatvorení DocuSpend zmizne. Pri ďalšej zmene sa DocuSpend pokúsi uložiť znova." (for a sub-category: "Podkategóriu sa nepodarilo uložiť, …", the rest the same);
  - while changes aren't being saved this session (the saved rules couldn't be opened, or are from a newer DocuSpend, #13): "Kategória platí len do zatvorenia DocuSpend." ("Podkategória platí …" for a sub-category).
- While changes aren't being saved, the card also shows "Zmeny pravidiel sa teraz neukladajú." at its top, as the rules card does.
- A rule that targets a category whose save failed is saved with that category's next successful save, as rules and categories are saved together today.

## Screens

The rules screen at the default window size. The form and the rules card are as in #21 and #24; "Kategórie" is new.

```
+--------------------------------------------------------------------------------+
| [Otvoriť výpis…] 1. 9. 2026 – 30. 9. 2026      (Výpis|Pravidlá)     Vzhľad (…) |
+---------------------------------------+----------------------------------------+
| Nové pravidlo                         | (Moje pravidlá|Vstavané pravidlá)      |
|  Podmienka   [Text v popise v] ...    |  Text obsahuje „lidl“                  |
|  Kategória   [               v]       |  Stravovanie › Potraviny               |
|  Podkategória[               v]       |               [Upraviť] [Odstrániť]    |
|              [Pridať pravidlo]        |  Text obsahuje „decathlon“             |
+---------------------------------------+  Šport › Vybavenie                     |
| Kategórie                             |               [Upraviť] [Odstrániť]    |
|  Názov [            ]                 |  ...                                   |
|  Počíta sa ako [Výdavky v] (?)        |                                        |
|               [Pridať kategóriu]      |                                        |
|  Kategória „Šport“ je pridaná.        |                                        |
|  ------------------------------------ |                                        |
|  Bývanie  výdavky  [Pridať podkat.]   |                                        |
|   (Nájom)(Vybavenie a doplnky)        |                                        |
|   (Úvery na bývanie)(Ostatné)         |                                        |
|  Stravovanie  výdavky [Pridať podkat.]|                                        |
|   (Potraviny)(Reštaurácie a kaviarne) |                                        |
|   (Obedy v práci·vlastná)(Ostatné)    |                                        |
|  ...                                  |                                        |
|  Šport  výdavky  vlastná [Pridať pod.]|                                        |
|   (Vybavenie)(Ostatné)                |                                        |
|  Nezaradené výdavky  výdavky          |                                        |
|  Nezaradené príjmy   príjmy           |                                        |
|  (scrolls inside the card)            |                                        |
+---------------------------------------+----------------------------------------+
```

A category with its "Pridať podkategóriu" line open; a refused name shows under the field:

```
|  Stravovanie  výdavky [Pridať podkat.]|
|   (Potraviny)(Reštaurácie a kaviarne) |
|   (Ostatné)                           |
|   [Ostatné          ] [Pridať][Zrušiť]|
|   Taký názov už existuje.             |
```

No pop-up boxes: everything happens inside the card.

## Acceptance criteria

The card:
- [ ] Given the rules screen, then it shows "Nové pravidlo", the rules card and "Kategórie", with "Kategórie" under the form; the statement screen is unchanged.
- [ ] Given no categories of the user's own, then "Kategórie" lists the 14 built-in categories in their usual order, then "Nezaradené výdavky" and "Nezaradené príjmy", each with its role tag, and each built-in category with its sub-categories, "Ostatné" last.
- [ ] The two "Nezaradené" rows have no "Pridať podkategóriu" and no sub-categories; every other row has the button.
- [ ] Given more categories than fit, then the card's list scrolls inside the card and the window doesn't scroll at the default size.

Adding a category:
- [ ] Given the name "Šport" and "Výdavky", when the user chooses "Pridať kategóriu", then "Šport" is listed after the built-in categories and before "Nezaradené výdavky", tagged "výdavky" and "vlastná", with the one sub-category "Ostatné"; the card says "Kategória „Šport“ je pridaná."; the name field is empty and "Výdavky" is still chosen.
- [ ] The same with "Príjmy" and with "Mimo výdavkov" gives the tag "príjmy" and "mimo výdavkov".
- [ ] Given "  Šport  ", then the category is named "Šport".
- [ ] Given an empty name, or spaces only, then "Zadajte názov." shows under the field and nothing is added.
- [ ] Given "Bývanie", "bývanie", "Byvanie", "Nezaradené výdavky", or the name of a category the user added earlier, then "Taký názov už existuje." shows under the field, nothing is added and the typed name stays.
- [ ] Given a refused name, when the user changes the name, then the reason disappears.
- [ ] The help mark by "Počíta sa ako" shows the tooltip text above.

Adding a sub-category:
- [ ] When the user chooses "Pridať podkategóriu" on "Stravovanie", then a line with a focused name field, "Pridať" and "Zrušiť" opens under its sub-categories.
- [ ] Given that line open, when the user chooses "Pridať podkategóriu" on another category, then the first line closes and one opens under the other category.
- [ ] Given "Obedy v práci" in the line under "Stravovanie", when the user chooses "Pridať", then the line closes, "Obedy v práci" shows before "Ostatné" and is marked as the user's own, and the card says "Podkategória „Obedy v práci“ je pridaná do kategórie „Stravovanie“."
- [ ] The same works on a category of the user's own.
- [ ] Given an empty name, "Ostatné", "ostatne", or "Potraviny" under "Stravovanie", then the reason shows under the field ("Zadajte názov." or "Taký názov už existuje."), the line stays open and nothing is added.
- [ ] Given "Potraviny" under "Bývanie", or "Šport" under "Šport", then it is added.
- [ ] When the user chooses "Zrušiť", then the line closes and nothing is added.

Using what was added:
- [ ] Given a new category with a new sub-category, then both are choices in the rule form's "Kategória" and "Podkategória" at once, for a new rule and for a rule opened with "Upraviť", with "Ostatné" last.
- [ ] Given a rule half typed with a category and sub-category chosen, when the user adds a category, then the form still shows what was typed and chosen.
- [ ] Given a new sub-category, when the user adds a rule targeting it that catches transactions of the open statement, then the form says how many, the rules card shows "<category> › <sub-category>", and on "Výpis" the category is in "Kategórie" with those transactions.
- [ ] Given a new spending category that a rule fills, then the pie has a slice for it, and every built-in category keeps the colour it had before the category was added.
- [ ] Given a new excluded category that a rule fills, then its transactions are out of the pie and out of "Výdavky spolu", and the category is still listed in "Kategórie".
- [ ] Given a new income category that a rule fills, then its transactions are not counted as spending.
- [ ] Given a new category with no rule pointing at it, then "Výpis" doesn't list it and the categorization of the open statement is the same as before it was added.

Saving:
- [ ] Given a new category with a sub-category and a rule targeting it, when DocuSpend restarts, then the category, its sub-category, its role and the rule are back, and the statement is categorized the same.
- [ ] Given saving fails, when the user adds a category, then the card says "Kategóriu sa nepodarilo uložiť, …", and the category is listed and can be chosen until DocuSpend closes. The same for a sub-category with "Podkategóriu sa nepodarilo uložiť, …".
- [ ] Given changes aren't being saved this session, then the card shows "Zmeny pravidiel sa teraz neukladajú.", and adding a category says "Kategória platí len do zatvorenia DocuSpend." ("Podkategória platí …" for a sub-category).
- [ ] What is saved has the same shape as before; the committed saved files of earlier versions still load.

Look, privacy and hand checks:
- [ ] The card uses only the shared look; the look test finds no literal colour, size or spacing in the screens.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.
- [ ] Every new text on screen is in Slovak.
- [ ] The maintainer has approved screenshots of the rules screen, in both looks, at the default window size: with the user's own categories and sub-categories listed, with a "Pridať podkategóriu" line open, and with a refused name.
- [ ] Hand check (maintainer): with a real statement open, add a category and a sub-category, add a rule that sends transactions there, check "Výpis", restart DocuSpend, and find the category, the sub-category and the rule as they were left.

## Edge cases and errors

- **A long name.** The name fields take at most 40 characters; a longer paste is cut at 40. Names that don't fit their place are cut with "…", as long names are today; the full name shows in the rule form's lists.
- **Names that differ only in diacritics or case** ("Sport" and "Šport") count as the same and are refused.
- **A name with spaces inside** ("Obedy v práci") is kept as typed; only spaces around it are removed.
- **A sub-category named like its category** ("Šport" under "Šport") is allowed: names have to be unique only among siblings.
- **Many categories of the user's own.** The card's list scrolls; the rule form's lists scroll as today. After the 14 slice colours are used, colours repeat, as they would for built-in categories; a category's colour never changes once it has one.
- **Adding while a rule is being edited** keeps the edit as it was; the new choices just appear in the form.
- **Adding with the sub-category line open** on the category fields at the top, and the other way round: each field keeps its own text; only one sub-category line is open at a time.
- **Switching to "Výpis" and back** keeps the card as it was: the typed name, the chosen role, an open sub-category line with its text, the message, and the list's scroll position.
- **Opening another statement** keeps the card as it was too; categories don't depend on the statement.
- **Saving fails, then works.** The next successful save of anything (a rule or a category) saves the categories added before, as rules are saved today.
- **The rules screen without a statement** doesn't exist (#21), so categories can be added only with a statement open.

## Out of scope

- Renaming or deleting categories and sub-categories, and hiding built-in ones (a follow-up issue, see the intent).
- Changing a category's role after it is created.
- Choosing a category's colour.
- Creating a category from the rule form or from a transaction ([#22](https://github.com/mDubovsky/DocuSpend/issues/22)).
- Reordering categories or sub-categories.
- Adding with the keyboard alone (Enter to add, Esc to cancel), as [#21](../021-rules-screen/spec.md) left keyboard use for a later accessibility item.
- Exporting, importing or sharing categories ([mvp.md](../../docs/mvp.md), future directions).

## Decisions

- **"Kategórie" goes under the "Nové pravidlo" form**, in the left column, where the rules screen has had empty space since #21. The rules card keeps the right column's full height, and the form stays next to the categories it chooses from.
- **The roles are shown as "Výdavky", "Príjmy" and "Mimo výdavkov"** (tags in lower case), with a help mark, because "excluded from spending" needs explaining and "Mimo výdavkov" says what it does in two words.
- **"Výdavky" is chosen at first**, since most categories a user adds are for spending.
- **"Ostatné" is always last** among sub-categories, here and in the rule form, so a new sub-category doesn't end up after the catch-all.
- **The user's own categories come after the built-in ones and before "Nezaradené"**, so the built-in list looks as it always has, and the uncategorized buckets stay at the end as everywhere.
- **The two "Nezaradené" buckets are listed**, without a way to add to them, so the list shows every category the user can meet on the statement screen.
- **Adding doesn't ask for confirmation**, though nothing added can be removed yet: a confirmation for every addition would slow down the common case, and renaming and deleting get their own issue.
- **Names are limited to 40 characters.** The longest built-in name has 32; longer names would crowd the legend, the lists and the rule card.
- **A sub-category's confirmation shows at the top of the card, its refusal under its own field**: a refusal belongs where the user is typing, while a confirmation in one place is easy to find after the line closes.
- **Everything happens in the card, with no pop-up box**, since adding a name is too small for one.

## Open questions

None.
