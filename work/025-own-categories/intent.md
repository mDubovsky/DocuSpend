---
status: planned
issue: 25
---

# Intent: Create your own categories and sub-categories

Issue: [#25](https://github.com/mDubovsky/DocuSpend/issues/25)

## Problem
The built-in categories don't fit everyone. A user who wants to follow something of their own, such as a hobby, their children or a holiday, has no way to add a category or a sub-category for it. A rule can only send a transaction to a sub-category that already exists, so spending that fits no built-in sub-category ends up in an "Ostatné" or stays uncategorized, mixed with everything else there.

The MVP says a built-in set of categories ships "and users can add their own" ([mvp.md](../../docs/mvp.md)). DocuSpend can already hold user categories and keeps them between sessions ([#13](../013-keep-rules/intent.md)), but the user can't create one on screen.

## Desired outcome
- **The user sees the categories.** In a card "Kategórie" on the rules screen ("Pravidlá", [#21](../021-rules-screen/intent.md)) the user can see every category with its sub-categories, built-in and their own, and which are their own.
- **A category can be added.** The user names it and chooses its role: spending, income or excluded from spending. It gets its own "Ostatné" sub-category without being asked.
- **A sub-category can be added** to any category, built-in or the user's own. The uncategorized buckets have no sub-categories, so they get none.
- **Names are checked as the categorization already checks them.** A name can't be empty, a new category can't share a name with another category, and a new sub-category can't share a name with another sub-category of the same category. Case and diacritics are ignored when comparing names. A refused name is explained in Slovak, and nothing is added.
- **New categories are usable straight away.** A new sub-category can be chosen as a rule's target at once, when adding a rule or editing one. Once a rule sends transactions to it, it shows in the charts and lists like any built-in one, with its role respected: a new spending category gets its own slice in the pie, and an excluded one stays out of it.
- **New categories are kept.** They are saved as soon as they are added, the same way as rules ([#13](../013-keep-rules/intent.md)), including telling the user when they can't be saved. Nothing changes in the shape of what is saved.

## Success criteria
- A user adds a category with each of the three roles, and each appears in the list of categories with an "Ostatné" sub-category.
- A user adds a sub-category to a built-in category and to a category of their own.
- An empty name, or a name already taken in the same place (also when it differs only in case or diacritics), is refused with a Slovak reason, and nothing is added.
- The uncategorized buckets offer no way to add a sub-category.
- Right after adding it, a new sub-category can be chosen as the target of a new rule and of an edited one.
- A rule that sends transactions to a new spending category gives that category its own slice in the pie and its own place in the list of categories; a rule that sends them to a new excluded category leaves them out of the pie.
- After closing and reopening DocuSpend, the new categories and sub-categories are back, and rules that point at them still categorize the same transactions.
- When a category can't be saved, the user is told as for a rule, and the category still works until DocuSpend closes.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)), and the saved file's shape is unchanged.
- The maintainer adds a category and a sub-category, makes a rule send transactions of a real statement to it, restarts DocuSpend and finds it all as they left it, as a hand check.

## Non-goals
- Renaming or deleting categories and sub-categories, and deleting or hiding built-in ones (as the issue says).
- Changing a category's role after it is created.
- Choosing a category's colour in the pie.
- Creating a category from inside the rule form, or from a transaction ([#22](https://github.com/mDubovsky/DocuSpend/issues/22)).
- Moving transactions between categories by hand without a rule ([#22](https://github.com/mDubovsky/DocuSpend/issues/22)).
- More than two levels of categories.
- Exporting, importing or sharing categories ([mvp.md](../../docs/mvp.md), future directions).

## Decisions
- **This item is the screen only.** The categorization already adds categories and sub-categories with the name checks above and an automatic "Ostatné", and #13 already saves them, as the issue notes.
- **A new category's role is chosen when it is created**: spending, income or excluded, as the issue asks. The role decides whether its transactions count as spending ([glossary](../../docs/glossary.md), "Role").
- **No sub-categories in the uncategorized buckets**, as the [glossary](../../docs/glossary.md) and [mvp.md](../../docs/mvp.md) define them.
- **Categories are referred to by identifiers, not names** ([decision 0004](../../docs/decisions/0004-stable-category-and-rule-identifiers.md)). That leaves room to rename them later without breaking rules.
- **No rename or delete in this item**, as the issue says. A category added by mistake stays until a later item makes it possible to change it.

- **The categories are a card "Kategórie" on the rules screen**, beside the rules, not a third screen, agreed by the maintainer. [#21](../021-rules-screen/intent.md) left room there for it, and categories are what rules point at.
- **A mistyped category can't be fixed yet**, agreed by the maintainer. Renaming and deleting your own categories get an issue of their own: deleting raises its own questions (what happens to the rules that point at a category), which would make this item twice as big.
- **A new, still empty category doesn't show on the statement screen**, agreed by the maintainer. The statement screen's categories and pie show what the month holds, as for built-in categories today; a new category shows in the "Kategórie" card and as a rule target, and on the statement screen once a rule fills it.

## Open questions
None.
