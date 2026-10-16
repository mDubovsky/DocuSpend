# DocuSpend: Product

This document describes WHAT DocuSpend is and WHY it exists. It deliberately says nothing about HOW.

## The problem

Many banks give you nothing but a raw list of transactions. There are no charts and no categories, so you can't see how much you spent on food last month, whether travel is eating your budget, or how your spending changed over time. The data is all there, but it explains nothing.

Some people's banks have no such tool in their app at all. Others have one and still prefer to see their spending the way DocuSpend shows it.

## What DocuSpend does

You give it a bank statement, in whatever form your bank provides, and it shows you where your money went and when: spending by category, trends over time, the big items, the surprises.

Example: "In September, 31% of my spending went to food, 18% to travel, 12% to subscriptions."

## Who it's for

- Individuals who want to understand their own spending without opening a spreadsheet.
- People whose bank offers statements but no spending analytics (charts, categories) in its app.
- People who have such a tool but prefer a different way of looking at their spending.
- People who care about keeping their financial data private.

The first, deliberately small version is defined in [mvp.md](mvp.md).

## Principles

- **Private by default.** Your financial data is yours and stays under your control.
- **Works with what you have.** You shouldn't need to reformat a statement before using it.
- **Understandable at a glance.** The output should answer "where did my money go?" quickly.
- **Open source.** Anyone can inspect it, trust it, and extend it.
- **Well tested.** Parsing and categorizing money data must be correct, so the product is built with heavy automated testing from day one.

## Non-goals (for now)

- Not a budgeting or financial-planning app.
- Not a live bank connection or account aggregator.
- Not financial advice.

## Open questions

Intentionally unanswered until they become work items:

- Which banks, countries, and statement formats come first?
- How are transactions assigned to categories, and how can users correct them?
- What does "this month" mean across multiple statements or accounts?
- How is the result delivered and shared?
