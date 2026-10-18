---
status: draft
issue: 3
---

# Intent: Sort transactions into spending categories

Issue: [#3](https://github.com/mDubovsky/DocuSpend/issues/3)

## Problem
After a statement is read, DocuSpend knows every transaction but not what it was for. A list of payments can't answer "how much did I spend on food this month?" until each transaction belongs to a category. Today nothing assigns categories, so the MVP's spending chart and category view have nothing to show.

## Desired outcome
Every transaction of a statement ends up in exactly one sub-category of exactly one category (for example Bývanie › Úvery na bývanie), decided by rules:

- **Default rules** built into DocuSpend assign common transactions (for example, a description containing NETFLIX goes to Nákupy online › Webové služby).
- **User rules** let the user assign the rest. Each rule has one condition, which can be on: the transaction type, text in the merchant or description, the counterparty's account number, the counterparty's name, the amount (a range or threshold), or the variable symbol.
- **Text conditions.** When creating a text condition the user chooses whether the text must be *contained* in the transaction's text or *equal* to it. Either way, case and diacritics are ignored, so "lekaren" matches "LEKÁREŇ".
- **Priority.** Every rule has a number. User rules always win over default rules; among user rules, the priority number decides which wins. DocuSpend refuses to create a user rule whose priority number another user rule already has.
- **Uncategorized.** A transaction that matches no rule goes to Uncategorized, so the user can see what still needs a rule. There is one for money going out (Nezaradené výdavky, "uncategorized expenses") and a separate one for incoming payments.
- **Excluded from spending.** A category can be marked as not spending (moving money between the user's own accounts, investments, …), and everything under it follows. Exclusion comes only from the category, never from a single sub-category. In the MVP, excluded transactions are simply left out of the spending totals and the spending chart; counting them as spending is a later option.
- **Spending is money going out.** Money coming back into a spending category (a refund, a reversed fee) reduces that category's spending, so a fee and its reversal cancel out. Incoming payments in the income category are never spending, but they are categorized too.

### Categories and sub-categories
- Two levels, not deeper. A rule's target is always a sub-category, and a transaction is always in a sub-category, never directly in a category. The only exceptions are the two Uncategorized buckets, which have no sub-categories.
- **Every category has a sub-category called Ostatné ("other")**, so there is always somewhere to put a transaction that fits the category but none of its specific sub-categories.
- A category's total is the sum of its sub-categories' totals.
- DocuSpend ships a built-in set (defined in the spec, based on the categories the user is used to from their bank's own spending view). More categories and sub-categories will be added to it later. It contains:
  - the spending categories, such as Bývanie, Stravovanie and Auto-moto;
  - one category for incoming payments, with sub-categories Mzda (salary), Náhrady výdavkov (expense reimbursements), Dávky a prídavky (benefits and allowances) and Ostatné;
  - two categories excluded from spending: moving money between the user's own accounts, and investments.
- Users can create their own categories and sub-categories. Built-in ones can't be deleted in the MVP.

### Language
The interface is in Slovak, so the names of built-in categories and sub-categories are Slovak. Documentation and code stay in English.

For any transaction, DocuSpend can tell which rule put it in its category, so the user can find out why a payment landed where it did.

## Success criteria
- On the committed sample statements, every transaction has exactly one category and one sub-category, and a statement's amounts are accounted for completely: whatever is counted as spending, plus whatever is excluded, plus income, adds up to the statement's own totals.
- A default rule categorizes a known example on the samples (the NETFLIX card payment goes to Nákupy online › Webové služby).
- A transaction is never directly in a category: every built-in category has an Ostatné sub-category.
- A category's total equals the sum of its sub-categories' totals.
- A user rule beats a default rule that matches the same transaction, and between two user rules the one with the winning priority number applies.
- Creating a user rule with a priority number that another user rule already has is refused.
- Each of the six kinds of rule condition matches the transactions it describes and no others.
- A text condition set to "contains" and one set to "equals" each match accordingly, both ignoring case and diacritics.
- A transaction that matches no rule is Uncategorized, and it still counts as spending if it is money going out.
- A transaction in an excluded category is not counted in spending, and income never is.
- Incoming payments are categorized like outgoing ones.
- The same statement with the same rules always gives the same result.
- All of this is checked by automated tests on synthetic statements.

## Non-goals
- Saving user rules between sessions (a separate issue).
- The rule editor and any other user interface, charts or views, including drilling down from a category into its sub-categories and the Slovak texts around them (separate issues).
- Combining several conditions in one rule.
- An option to count excluded categories, such as investments, as spending.
- Suggesting categories or learning from the user's choices.
- More than two levels of categories; deleting built-in categories.
- Other banks, other statement periods.

## Open questions
None. The exact names of the built-in categories for incoming payments and for the two excluded ones, and the full built-in list, are set in the spec.
