# Glossary

Shared terms. Use them consistently in docs, specs, and code.

- **Statement:** a document from a bank listing an account's transactions for a period.
- **Transaction:** a single movement of money on a statement: date, amount, type, counterparty, description.
- **Transaction type:** how the money moved, e.g. card payment, transfer, instant transfer.
- **Reader:** the part of DocuSpend that turns a statement file into a statement with its transactions, or rejects it.
- **Rejection:** the reader's answer for a file it can't read completely: one of a few kinds (empty, not a PDF, not a known statement, unreadable part, doesn't add up) with a human-readable reason that never repeats the file's content.
- **Reconcile:** a read statement reconciles when its transactions add up to its own printed totals and its closing balance follows from its opening balance and totals.
- **Counterparty:** the other party in a transaction (merchant, payer, or payee).
- **Category:** a named group of transactions shown in the spending chart, e.g. Bývanie. Every category has sub-categories, including one called Ostatné ("other").
- **Sub-category:** the second level, e.g. Úvery na bývanie under Bývanie. Every transaction is in exactly one sub-category. Two levels only.
- **Uncategorized:** the buckets for transactions that match no rule: one for money out (Nezaradené výdavky) and one for money in. They have no sub-categories.
- **Rule:** one condition plus a target sub-category. Matching transactions are assigned to that sub-category.
- **Default rule:** a rule built into the product.
- **User rule:** a rule created by the user. Persisted locally.
- **Priority:** a rule's number, used to choose between rules that match the same transaction. User rules always win over default rules.
- **Excluded from spending:** marks a category (and so everything under it) as not spending, e.g. transfers between the user's own accounts or investments.
- **Spending:** money going out, minus anything that came back into the same category (a refund, a reversed fee), excluding categories excluded from spending. Income is not spending.
- **VS / KS / SS:** variable, constant, and specific symbols; Slovak/Czech payment reference codes on transfers.
- **MVP:** the first version, defined in [mvp.md](mvp.md).
