# Glossary

Shared terms. Use them consistently in docs, specs, and code.

- **Statement:** a document from a bank listing an account's transactions for a period.
- **Transaction:** a single movement of money on a statement: date, amount, type, counterparty, description.
- **Transaction type:** how the money moved, e.g. card payment, transfer, instant transfer.
- **Counterparty:** the other party in a transaction (merchant, payer, or payee).
- **Category:** a named group of transactions shown in the spending chart, e.g. Subscriptions.
- **Uncategorized:** the category for transactions that match no rule.
- **Rule:** a condition plus a target category (and optionally "excluded from spending"). Matching transactions are assigned to that category.
- **Default rule:** a rule built into the product.
- **User rule:** a rule created by the user. Persisted locally.
- **Priority:** a rule's number, used to choose between rules that match the same transaction. User rules always win over default rules.
- **Excluded from spending:** marks a category or rule so its transactions don't count as spending, e.g. transfers between the user's own accounts.
- **Spending:** money going out, excluding anything excluded from spending. Income is not spending.
- **VS / KS / SS:** variable, constant, and specific symbols; Slovak/Czech payment reference codes on transfers.
- **MVP:** the first version, defined in [mvp.md](mvp.md).
