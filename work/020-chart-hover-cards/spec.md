# Spec: Charts' hover cards are always whole and always there

Intent: [intent.md](intent.md)

## Behavior

The statement screen's two charts show a small card when the pointer is over them, in the look's card style (#11). This item makes both cards reliable. Nothing else about the charts changes.

### The spending ring's card
- Hovering a slice shows a card with the category's name, and under it the amount and the share, as today: for example "Elektronika, komunikácia a médiá", then "48,90 € (3,1 %)". The slice still moves out a little while hovered, and clicking it still selects the category.
- The card appears next to the pointer. Where it would cross the chart's frame, it simply extends past it, over the rest of the window. It's never cut by the chart.
- Where it would cross the window's edge, it moves inwards so that it stays whole inside the window.
- The name is shown whole whenever the card fits in the window. Only if a name is too long for the room the window has, it is shortened at its end with an ellipsis ("Elektronika, komunikácia a…"). The amount and the share are never shortened.
- The card goes away when the pointer leaves the slice.

### The balance chart's card
- Anywhere over the balance chart's plotting area, from the period's first day to its last, the card shows **the day under the pointer**: its date, and under it "Zostatok" and that day's balance, for example "14. 5. 2026", then "Zostatok 1 204,37 €".
- **The day's balance is the balance at the end of that day**: the opening balance plus every transaction booked up to and including that day. A day without bookings shows the balance carried over from the last earlier booking day, or the opening balance before the first one. A day with several bookings shows the balance after all of them.
- **A dot in the accent colour marks that day on the line**, at the height of the day's balance, so it is clear which day the card belongs to, also where the line is flat.
- Moving the pointer left or right moves the dot and the card from day to day. The day is the one whose part of the chart the pointer is in, whatever the pointer's height.
- The card sits next to the dot and stays whole inside the window, like the ring's.
- When the pointer leaves the plotting area (onto the axis labels, the heading, or out of the card), the card and the dot go away.

### Both cards
- Both follow the look: the card's background and text in the light or dark look, the dot in the look's accent. Changing the look while a card is open leaves nothing in the old colours (#11's edge case still holds).
- Amounts and dates are in the Slovak format used everywhere else, with diacritics shown correctly.

## Screens

The statement screen's layout doesn't change. Only the cards change, and they appear only while the pointer is over a chart.

The ring's card, over a slice at the ring's left edge. The card extends past the ring's area, over the card's edge, and isn't cut:

```
+-- Výdavky podľa kategórií ---------------------------------+
|        ____                                                |
|     ,'      `.      ● Bývanie                  31 %  ...   |
| +----------------------------------+          ...          |
| | Elektronika, komunikácia a médiá |                       |
| | 48,90 € (3,1 %)                  |                       |
| +----------------------------------+                       |
|     `.____,'                                               |
+------------------------------------------------------------+
```

The balance chart, with the pointer on a day without bookings. The dot is on the flat stretch of the line, and the card is next to it:

```
+-- Zostatok na účte ----------------------------------------+
|  2 000 € -  ___                                            |
|                |                 +--------------------+    |
|  1 000 € -     |____________●    | 14. 5. 2026        |    |
|                             |   | Zostatok 1 204,37 €|    |
|      0 € -                  |_  +--------------------+    |
|            1. 5.   8. 5.   15. 5.   22. 5.   29. 5.       |
+------------------------------------------------------------+
```

## Acceptance criteria

- [ ] Given the example statement (`02-busy-month`), when the pointer is over any slice of the ring, including the smallest slices at the ring's edge, then a card shows the category's whole name, the amount and the share, and no part of the card is cut off, in both looks. (Screenshots of the hovered slices at the ring's left, right, top and bottom edges, reviewed by the maintainer.)
- [ ] Given a slice near the chart's frame, the card extends past the frame rather than being cut by it.
- [ ] Given a hovered slice near the window's edge, the card is moved inwards and stays whole inside the window.
- [ ] Given a category name too long for the room the window has, the name ends in an ellipsis, and the amount and the share are shown whole.
- [ ] At the size DocuSpend opens at, no built-in category's name is shortened in the card.
- [ ] Clicking a slice still selects its category.
- [ ] Given any day of the period, the balance shown for it equals the opening balance plus every transaction booked up to and including that day (checked automatically on every sample that reads, for every day of its period).
- [ ] Given a day without bookings between two booking days, the balance shown is the one after the earlier booking day.
- [ ] Given a day before the first booking day, the balance shown is the opening balance; given the period's last day, it is the closing balance.
- [ ] Given a day with several bookings, the balance shown is the one after all of them.
- [ ] Given a booking on the period's first day, that day shows the balance after it, not the opening balance.
- [ ] Given the pointer anywhere over the plotting area, at any height, a card shows the date of the day under the pointer and that day's balance, and a dot in the accent colour marks that day on the line at the day's balance.
- [ ] When the pointer moves to another day, the card and the dot move to that day; when it leaves the plotting area, both disappear.
- [ ] Given the empty month (`04-empty-month`), the ring shows its empty message as before, and every day of the balance chart shows the opening balance.
- [ ] Both cards and the dot follow the look; switching the look while a card is shown leaves nothing in the old colours.
- [ ] The look check passes: no screen defines a colour, size or spacing of its own (#11).
- [ ] Hand check (maintainer): on a real statement, in both looks, hovering both charts shows whole cards everywhere, and a balance card for every day.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.

## Edge cases and errors

- **A booking on the period's first day.** That day shows the balance after its bookings, like any other day. The opening balance is only shown for days before the first booking.
- **A negative balance** (`02-busy-month` closes below zero). The card shows it with a minus sign, in the normal text colour, as the header does.
- **One transaction in the month** (`05-single-transaction-month`). The days before it show the opening balance, the days from it onwards the closing balance.
- **A very small window.** The card stays inside the window as far as the window has room. The name is shortened further if needed. The amount, share, date and balance are never shortened.
- **Display scaling** (125 %, 150 %). The cards stay whole and the dot sits on the line.
- **The pointer at the boundary between two days.** The card shows one of them, never both and never nothing.
- **A statement opened while a card is shown.** The card and the dot go away, and the new statement's charts start without a card.
- **The pointer over the ring's hole** (the spending total). No card, as today.

## Out of scope

- New information in the cards, such as the day's transactions or the change from the day before.
- Other changes to the charts: colours, the legend, the ring's size, the line's shape, zooming or choosing a range.
- Reading the cards with the keyboard or a screen reader. That belongs to a later accessibility item.
- The look switch and scroll bars ([#19](https://github.com/mDubovsky/DocuSpend/issues/19)).

## Decisions

- **The cards keep their content** (the ring: name, then amount and share; the balance: date, then "Zostatok" and the amount). The issue is about the cards being cut off or missing, not about what they say.
- **The balance card responds to the pointer's left-right position only**, not its height, so the user never has to aim at the line itself. A stepped line has a balance for every day, so every position from the first day to the last has an answer.
- **The day's balance is the end-of-day balance**, also on the period's first day. A booking on the 1st then shows on the 1st, as the line draws it, rather than the opening balance hiding it.
- **The ring's card isn't confined to the ring's area** (intent: decisions). It only has to stay inside the window.
- **Edge-of-ring hovers are checked by screenshots**, because whether a card looks whole is for a person to judge. Every balance shown is checked automatically against the samples.

## Open questions

None.
