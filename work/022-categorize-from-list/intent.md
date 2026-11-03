---
status: draft
issue: 22
---

# Intent: Categorize a transaction from the list

Issue: [#22](https://github.com/mDubovsky/DocuSpend/issues/22)

## Problem
When a transaction lands in "Nezaradené výdavky" (or the uncategorized income), the user can see it in the list but can't do anything about it there. To sort it, they have to go to "Pravidlá", retype what identifies it (the merchant, the counterparty, the amount) and pick a sub-category, without the transaction in front of them.

Uncategorized transactions are the most common thing a user wants to fix, and fixing them is how DocuSpend becomes accurate for their spending. Today that is a separate task, so "this looks wrong" rarely becomes "fixed".

Some transactions also can't be fixed well with a rule at all: a one-off payment to a person, or a purchase that belongs somewhere different from everything else at the same shop. A rule would catch too much, and the only alternative is to leave it uncategorized.

## Desired outcome
- **Categorize from the transaction.** In a list of transactions on "Výpis", the user selects a transaction and can choose to categorize it ("Zaradiť"), without leaving the statement screen.
- **What identifies it is already filled in.** The user picks the sub-category and the kind of condition, with the same choices and checks as the rule form on "Pravidlá". The condition starts filled in from this transaction: "Text v popise" with its description, "Protistrana" with its counterparty, and so on. The user can change it before confirming.
- **Just this one, or a rule.** The user decides whether the choice is for **just this transaction** or becomes **a rule from this transaction**.
- **A rule made here is an ordinary user rule.** It goes on top like any new rule (priority, newest wins; [mvp.md](../../docs/mvp.md), "Rule priority"), applies to every matching transaction now and in later statements, shows on "Pravidlá", and can be edited or deleted there ([#24](../024-edit-rules/intent.md)). The user is told how many transactions of the open statement it catches, as when adding a rule there.
- **A choice for just this transaction is kept.** It is still there after closing DocuSpend, and it applies whenever the same transaction is seen again: when the statement is opened again, downloaded again, or when the next statement overlaps with it. DocuSpend recognises the transaction by what it says (its date, amount, description and counterparty, and which one it is among identical transactions), never by the statement file. If the bank changes a transaction's text between downloads, it is no longer recognised and goes back to where the rules put it.
- **Only a fingerprint is kept, never the details.** What DocuSpend saves for such a transaction is a fingerprint made from those details, from which they can't be read back, together with the chosen sub-category. The date, amount, description and counterparty themselves are never saved.
- **A choice for just this transaction wins over every rule**, built-in or the user's own. That is the point of making it.
- **It is visible and can be undone.** A transaction categorized just for itself shows a mark saying it was categorized by hand. From that mark, or with the row selected, the user can undo the choice ("Zrušiť zaradenie"), and the transaction goes back to where the rules put it.
- **The result shows at once.** The charts, the categories and the lists follow straight away, and the choice is saved the same way as rules ([#13](../013-keep-rules/intent.md)), including telling the user when it can't be saved.
- **The user knows what is kept, where they make the choice and in full.** When the user confirms "just this transaction", a pop-up explains in plain Slovak that DocuSpend will keep a fingerprint of this transaction, not its details, and that it stays on this computer, the user's own, and is never sent anywhere. It says where to read more. It has a "don't show again" checkbox; once ticked, the pop-up no longer appears, and that choice is kept like the light or dark look. [PRIVACY.md](../../PRIVACY.md) says in plain words what the fingerprint is made from, where it is kept, what it can't reveal (the details can't be read back from it), what it can (someone who has the same statement could check whether a transaction in it was categorized by hand), and how to remove it. [mvp.md](../../docs/mvp.md) ("only rules are saved") says the same. Nothing leaves the user's computer.

## Success criteria
- A user selects an uncategorized transaction on "Výpis", categorizes it, and stays on the statement screen.
- Each kind of condition the rule form offers starts filled in from the selected transaction, and the user can change it before confirming.
- Choosing a rule adds an ordinary user rule: it categorizes every matching transaction of the open statement, the user is told how many, it shows on "Pravidlá", and it is back after a restart.
- Choosing just this transaction moves that transaction only; an identical-looking transaction elsewhere in the statement stays where it was.
- A transaction categorized just for itself stays in its sub-category even when a rule, built-in or the user's own, matches it.
- After closing and reopening DocuSpend, a transaction categorized just for itself is in the same sub-category again, and also in an overlapping statement that contains it.
- Among identical transactions (same date, amount, description and counterparty), the one the user chose is the one that stays chosen after a restart.
- A transaction whose text differs from the one the user chose is not recognised, and goes where the rules put it.
- A transaction categorized just for itself shows the mark; undoing it puts the transaction back where the rules put it, removes the mark, and the undo is kept after a restart.
- The charts, categories and lists change straight away after each of these.
- A choice that can't be saved is reported as for a rule, and still applies until DocuSpend closes.
- Rules and categories saved by an earlier version still load and categorize the same way.
- Nothing DocuSpend saves contains the date, amount, description or counterparty of a transaction categorized just for itself (checked by categorizing transactions and inspecting everything DocuSpend wrote).
- Confirming "just this transaction" shows a Slovak pop-up saying that a fingerprint of the transaction, not its details, will be kept on this computer and never leaves it, and where to read more.
- Ticking "don't show again" in that pop-up stops it from appearing, also after a restart; without ticking it, it appears every time.
- PRIVACY.md says what the fingerprint is made from, where it is kept, what it can and can't reveal, and how to remove it; mvp.md agrees with it.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)).
- The maintainer categorizes transactions of a real statement both ways, undoes one, restarts DocuSpend, reopens the statement and finds everything as they left it, as a hand check.

## Non-goals
- Editing existing rules from the transaction (they are edited on "Pravidlá", [#24](../024-edit-rules/intent.md)).
- Categorizing many transactions at once.
- Showing which rule put a transaction in its category.
- Creating a category or sub-category from the transaction; that is done on "Kategórie" ([#25](../025-own-categories/intent.md)).
- A list of all choices made for single transactions, or managing them anywhere else than on the transaction itself.
- Recognising a transaction whose text the bank changed between downloads.
- Saving statements or transactions for any other purpose; DocuSpend keeps only what recognises a transaction the user categorized by hand.
- Sharing choices made for single transactions. They are never part of sharing rules ([mvp.md](../../docs/mvp.md), future directions).

## Decisions
- **A rule made from a transaction is an ordinary user rule**, as the issue says, so there is nothing new to learn and it is edited where every rule is.
- **A choice for just this transaction is recognised by the transaction's own details, not by the statement file**, as the issue says. That way it survives downloading the statement again, a different format, and overlapping statements, and nothing about the file is kept.
- **A transaction whose text changes is not recognised.** Guessing would risk moving the wrong transaction silently; going back to where the rules put it is visible and easy to fix again.
- **The privacy decision doesn't change.** Nothing leaves the computer, so [decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md) holds.
- **Only a fingerprint of the transaction is kept, never its details**, decided by the maintainer. [#13](../013-keep-rules/intent.md) promised that nothing saved contains anything read from a statement; a fingerprint from which the details can't be read back keeps that promise as closely as possible while still recognising the transaction. PRIVACY.md and mvp.md are clarified to say exactly that, so the promise users read stays true.
- **Disclosed as fully and clearly as possible**, decided by the maintainer: being open with the user about their data comes first. So the user is told at the moment of choosing, not only in PRIVACY.md, and PRIVACY.md states the fingerprint's limits too, including that someone with the same statement could check it, rather than a wording stronger than the truth.
- **The privacy pop-up says "this computer"**, decided by the maintainer: the user must understand that the fingerprint stays on their own machine, not on "a" or "the" computer somewhere.
- **The privacy pop-up can be switched off with "don't show again"**, decided by the maintainer, so a user who has understood it isn't asked every time. PRIVACY.md always explains it in full.
- **Any transaction in any list on "Výpis" can be categorized**, not only uncategorized ones, agreed by the maintainer. A wrongly categorized transaction is just as worth fixing, and a choice for just this transaction overrides any rule, so it is the same action either way.
- **One item, not two**, agreed by the maintainer. The window and the choice between "just this one" and "a rule" are one design; splitting would mean building and reviewing that window twice.
- **Rules saved by an earlier version still load** ([#13](../013-keep-rules/intent.md)): if keeping these choices changes the shape of what is saved, the older shapes are still read.

- **Once hidden, the privacy pop-up doesn't come back in this item**, agreed by the maintainer. PRIVACY.md always has the full explanation, and a way to bring back hidden messages can come with a settings screen if one is ever needed.

## Open questions
None.
