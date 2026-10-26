---
status: specced
issue: 24
---

# Intent: Edit, delete and reorder rules

Issue: [#24](https://github.com/mDubovsky/DocuSpend/issues/24)

## Problem
A user can add rules but can't change them afterwards. A rule with a typo, the wrong target, or a condition that catches too much can only be outvoted by adding another rule on top. Since rules are kept between sessions ([#13](../013-keep-rules/intent.md)), a mistaken rule stays for good, and the list only ever grows. The longer it gets, the harder it is to see which rule decides what, and why a transaction landed where it did.

The MVP includes "a very basic editor to list, edit, and delete rules and change their priority" ([mvp.md](../../docs/mvp.md)). DocuSpend has the list, but not the rest.

## Desired outcome
- **Every kind of condition can be made on screen.** The form for a rule offers all six kinds the MVP lists (transaction type, text, counterparty name, counterparty account, amount, variable symbol), for adding and editing alike. Until now it offered three.
- **Every user rule can be edited.** The user can change its condition (including the kind of condition) and its target sub-category, with the same choices and the same checks as when adding a rule. It stays the same rule: it keeps its identifier ([decision 0004](../../docs/decisions/0004-stable-category-and-rule-identifiers.md)) and its place among rules with the same priority number.
- **Every user rule can be deleted**, after confirming, so a slip of the mouse never loses a rule.
- **Every user rule's priority can be changed.** The user sees each rule's priority number and can give it a different one. The number starts at the rule's current value, so a user who doesn't care about numbers never has to touch it, and a help mark next to it explains in a short Slovak tooltip what the number means (1 wins, own rules always beat built-in ones, the newest wins between equal numbers). The list shows the rules in the order they are tried, so the rule that wins is the one higher up.
- **The user sees what a change did.** After an edit, the user is told how many transactions of the open statement the rule now catches, as when adding a rule.
- **Changes take effect straight away.** After any edit, deletion or priority change, the open statement is categorized again, and the change is saved the same way as adding a rule ([#13](../013-keep-rules/intent.md)), including telling the user when it can't be saved.
- **Default rules can be seen, not changed.** The user can see them in a list of their own, "Vstavané pravidlá", below their own rules and closed until opened, so the whole order is visible without crowding the user's rules. They can't be edited, deleted or renumbered. User rules always win over them, as before.
- **Nothing new is saved.** What is kept between sessions doesn't change shape; this item only changes which rules it holds.

## Success criteria
- A user can add, and edit, a rule of each of the six kinds of condition on screen.
- A user changes a rule's condition, and its target, and the open statement is categorized as if the rule had been added that way; after a restart the changed rule is back as changed.
- An edit with invalid input (for example an empty text or an amount range that is upside down) is refused with the same Slovak reason as when adding a rule, and the rule is left as it was.
- After an edit, the user is told how many transactions of the open statement the rule now catches.
- Deleting asks for confirmation; declining leaves the rule as it was. A deleted rule no longer categorizes anything, and is not back after a restart.
- A user changes a rule's priority number, and the order in which rules are tried, the list's order, and the categorization follow it; after a restart they are the same.
- The priority number starts at the rule's current value, and its help mark shows a Slovak tooltip explaining what the number does.
- Two rules sharing a priority number still decide newest first after either is edited or renumbered.
- Default rules are listed under "Vstavané pravidlá", closed at first, and offer no way to edit, delete or renumber them.
- A change that can't be saved is reported as it is for a new rule, and still applies until DocuSpend closes.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)), and the saved file's shape is unchanged.
- The maintainer edits, renumbers and deletes rules while viewing a real statement, restarts DocuSpend and finds the rules as they left them, as a hand check.

## Non-goals
- Creating, renaming or deleting categories and sub-categories on screen (a separate issue).
- Editing, switching off or deleting default rules.
- Moving the rules to a screen of their own ([#21](https://github.com/mDubovsky/DocuSpend/issues/21)). The editor goes wherever the rules list is when this item is built.
- Showing which rule put a transaction in its category, and categorizing a single transaction from the statement screen (separate issues).
- Undo or a change history.
- Moving rules up and down, or dragging them, instead of typing a number.
- Choosing a priority number when adding a rule: a new rule still goes on top without asking.
- Rule types or conditions beyond those the MVP lists; combining conditions.
- Exporting, importing or sharing rules ([mvp.md](../../docs/mvp.md), future directions).

## Decisions
- **Editing a rule keeps it the same rule**: same identifier, same place among rules with the same number. That is what [#3's spec](../003-categorize-transactions/spec.md) already says for changing a number ("doesn't change which was created first"), and it keeps a corrected rule from suddenly winning or losing ties.
- **No change to what is saved**, as the issue notes: #13 already saves whatever the user's rules are, whichever screen changes them.
- **Default rules are shown but read-only**, as the issue asks. They belong to the version of DocuSpend the user runs (#13), and a user rule on top already overrides any of them.
- **The form offers all six kinds of condition**, decided by the maintainer while specifying. The MVP already lists them for user rules, and an editor has to be able to show any rule the user can have.
- **Deleting asks for confirmation**, not undo. It is simpler to get right, and deleting is rare.
- **Priority is changed by typing a number only**, as the MVP and the issue describe. Priority numbers can be confusing, so the field starts at the rule's current value and a help mark explains it in a tooltip. Moving rules up and down can come later if numbers turn out awkward.
- **After an edit, the user is told how many transactions the rule now catches**, as after adding one. It is the quickest way to see whether the edit did what the user meant.
- **The editor doesn't wait for [#21](https://github.com/mDubovsky/DocuSpend/issues/21).** It is built in "Moje pravidlá" where the list is today; #21 moves the whole card, editor included.
- **Default rules are listed under "Vstavané pravidlá"**, below the user's rules and closed until opened, so they don't crowd the user's own rules. The maintainer chose "vstavané" (built-in) over "predvolené".

## Open questions
None.
