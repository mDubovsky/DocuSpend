# MVP

The smallest version of DocuSpend that is genuinely useful. WHAT only, no technology.

## Outcome

A user uploads one monthly PDF statement from Všeobecná úverová banka, a.s. (VÚB) and can see where their money went that month.

## In scope

- **Input:** one monthly PDF statement from VÚB.
- **Balance chart:** a line chart of the account balance over the month.
- **Spending chart:** a pie chart of spending by category. Spending only; income is not part of the pie.
- **Inspect a category:** selecting a category shows the list of transactions in it.
- **Uncategorized:** transactions matching no rule go into an "Uncategorized" category, inspectable like any other.
- **Excluding from spending:** a category (or rule) can be marked as excluded from spending, e.g. transfers between the user's own accounts.
- **Default rules:** built-in rules assign common transactions to categories (e.g. a description containing NETFLIX goes to Subscriptions).
- **User rules:** the user can create simple rules of their own, matching on:
  - transaction type (card payment, transfer, instant transfer)
  - text in the merchant or description
  - the originator/destination account number
  - the counterparty name
  - amount (a range or threshold)
  - variable symbol
- **Rule priority:** every rule has a numbered priority. User rules always win over default rules. Among user rules, the priority number decides. (Exact semantics, such as ties, are settled in the spec.)
- **Rule persistence:** user rules are saved locally and are still there the next time the software is used.
- **Rule editor:** a very basic editor to list, edit, and delete rules and change their priority.
- **Clear failures:** if the file isn't a readable VÚB statement, the user gets a clear message, never a silently wrong chart.
- **Private:** the statement is never sent to any server.

## Out of scope (for the MVP)

- Other banks or statement formats
- Saving parsed statements between sessions (only rules are saved)
- Weekly, yearly, or multi-month views
- Rule types beyond the ones listed above (e.g. combining conditions, matching on card, purpose category code, or direction)

## Done when

- A real VÚB monthly statement produces both charts without manual fixing.
- Every transaction lands in exactly one category (possibly "Uncategorized").
- A user can create a rule, see the charts update, close the software, return, and find the rule still applied.
- A non-VÚB or unreadable file produces a clear error.

## Future directions to keep in mind

Not built now, but early decisions should not make these hard:

- Other banks, with different statement structures
- Local retention of parsed statements, so a whole year can be viewed with one upload per month
- Other periods: weekly, yearly, custom ranges
