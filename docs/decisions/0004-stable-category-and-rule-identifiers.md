# 0004: Categories and rules are referred to by stable identifiers, never by name

Status: accepted

## Context
Decision [0003](0003-slovak-interface-english-code.md) made the names of the built-in categories Slovak data that may be corrected or extended. Users will create rules that point at categories, and a later item will save those rules. If a rule stored a category's *name*, correcting a typo in a built-in name, or a user renaming their own category, would silently break the rule or send it to the wrong place.

## Decision
Everything that refers to something in the categorization uses an identifier, and no identifier is derived from a display name that can change.

- **Built-in categories and sub-categories** have fixed English keys that never change, for example `housing` and `housing.rent`. The Slovak name is a separate property. The Ostatné sub-category of a category is `<category>.other`.
- **User-created categories and sub-categories** get a generated identifier (`user-` followed by a GUID), so they can never collide with a built-in key.
- **Default rules** get an identifier derived from their target and condition, for example `default/housing.rent/najom`, not from their position in the list. Reordering the list or adding rules doesn't change the others' identifiers.
- **User rules** get a generated identifier.
- Results (which sub-category a transaction is in, which rule decided) carry identifiers. Names are looked up from the catalog when something is shown.
- Identifiers are small value types (`CategoryId`, `SubcategoryId`, `RuleId`), so one kind can't be passed for another.

## Consequences
- Correcting or translating a built-in name changes no saved data. Renaming a user category (an editing feature that doesn't exist yet) will change only its name property.
- Built-in identifiers are part of what saved data depends on: once released, a built-in key is never reused for something else. A built-in category that is later retired must keep its key.
- A derived default-rule identifier changes if the rule's own text changes. That is deliberate: it is then a different rule.
- Saving user rules in the persistence item stores identifiers, plus each rule's priority and creation order, and needs no names.
