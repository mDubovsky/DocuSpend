---
status: done
issue: 41
---

# Intent: Open statements with unfamiliar transaction kinds

Issue: [#41](https://github.com/mDubovsky/DocuSpend/issues/41)

## Problem
A user opens a VÚB monthly statement and DocuSpend refuses it, because one of its transactions is of a kind DocuSpend has never seen. Everything else on the statement reads fine and adds up, yet the user gets no charts for the whole month.

The reader knows only the kinds seen on the statement it was built from: card payments ("PLATBA KARTOU"), Apple Pay payments ("PLATBA KARTOU A-PAY"), transfers, instant transfers and account fees. A recent real statement also has Google Pay payments ("PLATBA KARTOU G-PAY") and a cash withdrawal ("VÝBER HOTOVOSTI"), and was rejected over them. Banks add and rename kinds, and a single new payment habit is enough. Any month can bring one, so a list of known kinds will always be one step behind.

The rejection comes from the rule that a statement reads completely or not at all ([#2](../002-statement-reader/spec.md)). The rule exists so the user never sees a wrong number. A kind DocuSpend doesn't know is not a wrong number, though, as long as the dates and the amount read correctly and the statement still adds up.

## Desired outcome
- **A statement with an unfamiliar kind opens.** A transaction whose kind DocuSpend doesn't know, but whose dates and amount read correctly, is read like any other. It shows in the charts and the transaction lists, and it is categorized like any other transaction, so it lands in an uncategorized bucket unless a rule catches it.
- **The user can see what the kind was.** Such a transaction shows the kind exactly as the statement prints it (for example "VÝBER HOTOVOSTI"), not a blank or a made-up name.
- **The user knows DocuSpend met something new.** When a statement holds transactions of kinds DocuSpend doesn't know, the user is told how many, without being stopped. A user who cares can look them up; one who doesn't can carry on.
- **Damaged statements are still rejected.** If a transaction's date or amount can't be read, or the transactions don't add up to the statement's printed totals, the statement is rejected as before. A wrong number in the charts is worse than none.
- **A user can make a rule for an unknown kind.** The printed kind counts as text for text rules, so a rule with "contains" and a word from the printed kind catches such transactions.
- **Google Pay is a known kind.** "PLATBA KARTOU G-PAY" is a payment by mobile ("Platba mobilom"), like "PLATBA KARTOU A-PAY", and type rules for it work as they do for Apple Pay.
- **Cash withdrawals are a known kind.** "VÝBER HOTOVOSTI" is read as a cash withdrawal ("Výber hotovosti") with its card and value date, and a user can make a type rule for it like for any other kind.

## Success criteria
- A statement with a transaction of a kind DocuSpend doesn't know, whose dates and amount are readable, opens. That transaction is in the transaction list with its printed kind, in the charts, and in an uncategorized bucket unless a rule catches it.
- Such a statement shows a short line on the statement screen with how many transactions are of kinds DocuSpend doesn't know, and no such line shows for a statement without any.
- A text rule with "contains" and a word from an unknown kind's printed text catches transactions of that kind.
- A statement where a transaction of an unknown kind has no readable amount or date is still rejected, naming the transaction's place.
- A statement that doesn't add up is still rejected, whatever kinds its transactions are.
- A "PLATBA KARTOU G-PAY" transaction is read as a payment by mobile, with its card and value date, and a type rule for payments by mobile catches it.
- A "VÝBER HOTOVOSTI" transaction is read as a cash withdrawal, with its card and value date. A user can make a type rule for cash withdrawals, and it is still there after a restart.
- Statements DocuSpend read before read exactly as before, and the saved rules file from before loads unchanged.
- The maintainer's statement check reports how many transactions of unknown kinds each statement has, as a count only.
- All of this is shown with synthetic samples; no real statement content goes into the repository.
- The maintainer opens the October statement that was rejected, and it opens with sensible charts, as a hand check.

## Non-goals
- Other banks or statement formats ([mvp.md](../../docs/mvp.md)).
- Reading every kind a bank might print: what new kinds mean beyond Google Pay and cash withdrawals is left to later items, if a kind turns out to deserve one.
- Guessing what an unknown kind means (for example treating anything with a card number as a card payment).
- Accepting statements that don't add up, or transactions with an unreadable date or amount.
- New kinds of rule condition beyond what the open questions settle.

## Decisions
- **The totals check stays, and it is what decides.** The issue says so: reading an unknown kind is safe exactly because a statement that doesn't add up is still rejected.
- **Google Pay is added as a payment by mobile, not as a kind of its own.** It is printed and laid out like Apple Pay, and to the user it is the same thing: paying with the phone.
- **The printed kind is kept as printed.** It is the only honest name DocuSpend has for it, and Slovak is already what the user reads.
- **Cash withdrawals become a known kind in this item**, decided by the maintainer. They are common and already seen on a real statement, and a user wants to sort them by kind rather than by text.
- **A rule catches an unknown kind through text rules**, decided by the maintainer: the printed kind counts as text for text rules. That needs no new kind of condition and no change to the shape of the saved rules file. A type rule can't name a kind nobody knew when the rule was made.
- **The user is told about unknown kinds by a short, quiet line on the statement screen**, decided by the maintainer: roughly "N transakcií neznámeho druhu", shown only when there are some. It is enough to be noticed and doesn't get in the way.

- **Cash withdrawals get no built-in category**, decided by the maintainer. Cash can end up as anything, and no built-in category fits it, so it goes to "Nezaradené výdavky" (still counted as spending) until the user makes a type rule for it. A built-in category would add to the catalog and its colours, which is a choice of its own.

## Open questions
None.
