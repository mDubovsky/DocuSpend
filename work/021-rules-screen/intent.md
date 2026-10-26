---
status: in-progress
issue: 21
---

# Intent: Move the rules off the statement screen

Issue: [#21](https://github.com/mDubovsky/DocuSpend/issues/21)

## Problem
The statement screen shows everything at once: the key figures, both charts, the categories, the transactions of the selected category, the rule form and the rule list. Since [#24](../024-edit-rules/intent.md) the rule part has grown: a form for six kinds of condition, editing, deleting, priority numbers, and a switch between "Moje pravidlá" and "Vstavané pravidlá". The screen is crowded, and the window has to be scrolled to reach the bottom, even at its default size.

Rules are needed only now and then. Someone who opens a statement to see where their money went has to look past the rule tools to do it. The MVP promises an answer to "where did my money go?" at a glance ([product.md](../../docs/product.md), "Understandable at a glance"). Setting DocuSpend up is a second activity, and it costs the charts and lists the space they need.

## Desired outcome
- **Two screens with a statement open:** the statement and the rules. The user switches between them at any time, and back, from a control that is always in the same place.
- **The statement screen is for reading a month:** the key figures, both charts, the categories and the transactions of the selected category. No rule form and no rule list. At the default window size it fits the window without scrolling; a list that is longer than its card scrolls inside the card, as lists do today.
- **The rules screen holds everything about rules that is on the statement screen today:** the form for a new rule, "Moje pravidlá" with editing, deleting and priority, and "Vstavané pravidlá". It works as it does today; this item moves it, it doesn't change it.
- **Rules still act on the open statement straight away.** Adding, editing or deleting a rule on the rules screen categorizes the open statement again, as today, and the user is still told how many of its transactions the rule catches. Going back to the statement screen shows the new categorization.
- **Switching loses nothing.** Going to the rules and back keeps what the user was looking at: the selected category and its transactions. Going to the statement and back keeps a rule that is half typed or being edited.
- **Messages that concern the whole application stay visible on both screens**, for example that a statement couldn't be opened, or that the saved rules couldn't be loaded. Messages that answer a rule change stay with the change, on the rules screen.
- **Room to grow:** the rules screen leaves room for what will join it later, such as managing categories, and nothing in this item stands in the way of categorizing a single transaction from the statement screen later.

## Success criteria
- With a statement open, the statement screen shows no rule form and no rule list.
- At the default window size, in both looks, the statement screen fits the window without scrolling; screenshots of both screens in both looks are approved by the maintainer.
- A user can switch to the rules screen and back at any time a statement is open, and the switch looks and behaves the same on both screens.
- Everything the rules card does today works on the rules screen: adding a rule of each kind, editing, deleting with confirmation, changing priority, the built-in rules, and the messages for invalid input and for rules that can't be saved.
- Adding a rule on the rules screen recategorizes the open statement: the user is told how many transactions it catches, and back on the statement screen the charts, categories and transactions follow it.
- After switching to the rules and back, the same category is selected and its transactions are shown. After switching to the statement and back, a half-typed rule, or a rule being edited, is still there.
- An error about opening a statement, or about loading the saved rules, is visible whichever screen is shown.
- Every new part of the screens comes from the shared look; the look test still finds no literal colour, size or spacing in the screens.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)), and what is saved doesn't change.
- The maintainer opens a real statement, switches between the screens, adds and edits a rule, and checks the statement screen at the default window size, as a hand check.

## Non-goals
- Changing what the rules can do: editing, deleting and reordering were built by [#24](../024-edit-rules/intent.md); this item only moves them.
- Saving rules ([#13](../013-keep-rules/intent.md)); what is saved doesn't change.
- Managing categories and sub-categories on screen (a separate issue).
- Categorizing a single transaction from the statement screen (a separate issue), though this item must not rule it out.
- Showing which rule put a transaction in its category (a separate issue).
- Redesigning the charts, the lists or the header beyond what moving the rules needs; changing the look's colours, sizes or rounding.
- Fitting windows smaller than the default size without scrolling.
- Reaching the screens with the keyboard, as [#11](../011-modern-look/intent.md) and [#19](../019-look-switch-scroll-bars/intent.md) left keyboard access for a later item.

## Decisions
- **The rules move as a whole**, everything #24 built included. The issue predates #24; #24 itself said "#21 moves the whole card, editor included".
- **This item only moves what exists**, as the issue says. Any change to how rules work is a separate item.
- **A rule changed on the rules screen applies to the open statement at once**, as today and as the issue asks.
- **The rules screen is reached only with a statement open**, decided by the maintainer. Today the rules are shown only with a statement, and the "how many transactions it catches" message needs one; rules without a statement can be a later item.
- **The screens are switched by a pair of pills in the window's header, next to "Otvoriť výpis…", named "Výpis" and "Pravidlá"**, decided by the maintainer. It matches the switches the look already has and keeps the header the one place for actions; the spec settles the exact wording.
- **Opening another statement always shows the statement screen**, decided by the maintainer. Opening a statement means the user wants to see it.
- **"Fits without scrolling" holds for the default window size only**, decided by the maintainer. The window can be made much smaller (down to its minimum), and below the default size it may scroll as it does today.

## Open questions
None.
