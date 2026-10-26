---
status: in-progress
issue: 20
---

# Intent: Charts' hover cards are always whole and always there

Issue: [#20](https://github.com/mDubovsky/DocuSpend/issues/20)

## Problem
Hovering a chart is how a user reads an exact figure from it, and on both charts it lets them down.

- **The spending ring.** For a slice near the edge of the chart, the card with the category, amount and share is cut off by the chart's frame. A long category name loses its start ("tronika, munikácia a médiá" instead of "Elektronika, komunikácia a médiá") and part of the card is missing, so the user can't tell which category they are looking at.
- **The balance line.** The card with the date and balance shows only when the pointer is exactly on a day where the balance changes, and disappears in between. Most days of a month have no transactions, so for most of the line the user gets nothing, although the line itself shows a balance for every day. Finding the few spots where a card appears feels broken.

[#11](../011-modern-look/spec.md) promised both cards ("Hovering a slice shows the category, the amount and the share", "Hovering shows a small card with the date and the balance"); they work only part of the time. Both show up straight away on the example statement.

## Desired outcome
- **The ring's card is always whole and readable**, wherever the slice is: it is never cut off by the chart's frame or the window's edge. A name that is still too long for the card is shortened with an ellipsis, the way the legend beside the ring already does it, rather than clipped.
- **The balance card is there anywhere over the balance line.** It shows the day under the pointer and the balance on that day, and follows the pointer from day to day. A day without transactions shows the balance carried over from the last change, as the stepped line already draws it.
- **The cards keep the look** from #11, in light and dark, and nothing else about the charts changes: slices are still clicked to select a category, the line stays stepped.

## Success criteria
- On the example statement, hovering any slice of the ring, including the smallest ones at the edge, shows a whole card with the category, the amount and the share, in both looks.
- A category name too long for the card ends in an ellipsis, never loses its start or is cut mid-letter.
- Hovering anywhere over the balance line, between days with transactions as well as on them, shows a card with that day's date and balance, and a dot on the line at that day; moving the pointer to the next day moves both.
- The balance shown for a day without transactions equals the balance after the last earlier change (or the opening balance before the first one).
- On a day with several transactions, the card shows the balance at the end of that day, which is what the line shows for it.
- Over the empty month, the ring shows its empty message as before and the flat balance line shows the opening balance for every day.
- The maintainer hovers both charts on a real statement, in both looks, and finds the cards whole and present everywhere, as a hand check.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)).

## Non-goals
- New information in the cards (for example the day's transactions, or the change from the previous day).
- Other changes to the charts: colours, the legend, the ring's size, the line's shape, zooming or choosing a range.
- Reading the cards with the keyboard or a screen reader; that belongs to a later accessibility item.
- The look switch and scroll bars ([#19](https://github.com/mDubovsky/DocuSpend/issues/19)).

## Decisions
- **The line stays stepped**, as #11 decided: a balance holds between changes, so the card shows the carried-over balance rather than an amount between two points.
- **Shortening with an ellipsis is a fallback**, not the first choice: the issue asks for the card to stay readable, and the legend already shortens names the same way.
- **The ring's card may extend beyond the chart's frame**, over neighbouring parts of the window, as long as it stays inside the window; the name is shortened only if it still doesn't fit. Keeping the card inside the small chart area would shorten long names far more often than needed. Decided by the maintainer.
- **The balance chart marks the day under the pointer** with a small dot in the accent colour on the line, as #11's intent described ("a highlighted point with a small callout"). Without it, it is hard to tell which day the card belongs to when the line is flat. Decided by the maintainer.
- **A day with several transactions shows its end-of-day balance**, which is what the stepped line shows for that day and what a bank would call the day's balance. Decided by the maintainer.

## Open questions
None.
