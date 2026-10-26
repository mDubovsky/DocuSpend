---
status: done
issue: 19
---

# Intent: The look switch and scroll bars follow the look

Issue: [#19](https://github.com/mDubovsky/DocuSpend/issues/19)

## Problem
[#11](../011-modern-look/intent.md) gave DocuSpend one look, defined once, with pill-shaped selections and the same styles for every control. A few controls on the statement screen don't follow it, and they are the ones a user sees first.

- **The look switch changes shape.** On the start screen, the chosen look ("Podľa systému", "Svetlý", "Tmavý") is a pill with its text centred. Once a statement is open, the highlight changes shape and its text is no longer centred in it. It is most visible in the dark look. The same control looks different on two screens, which makes it look broken.
- **The look switch doesn't react to the pointer.** List rows, the legend and buttons change softly when the pointer is over them. The look switch doesn't, so it looks inactive, and a user may not try to click it.
- **Scroll bars are the toolkit's default.** The scroll bar beside the "Kategórie" list is plain and square next to rounded cards and pills, and doesn't take the look's colours. It looks like a part from a different program.

## Desired outcome
- **The look switch looks the same on every screen:** the chosen look is a pill with its text centred, on the start screen and with a statement open, in light and dark.
- **The look switch shows it can be clicked:** when the pointer is over a choice that isn't chosen, it changes softly, the way list rows and the legend do.
- **Scroll bars belong to the look:** slim, rounded, in the look's colours, in light and dark, wherever something in the window scrolls.
- **Nothing else changes:** the switch works as before, lists scroll as before, and the rest of the window looks as it does now.

## Success criteria
- Screenshots of the start screen and the statement screen, in both looks, show the look switch with the same pill shape and centred text. The maintainer approves them.
- Screenshots show a choice of the look switch under the pointer looking different from the same choice without it, in both looks.
- Screenshots of a list long enough to scroll show a slim, rounded scroll bar in the look's colours, in both looks. The maintainer approves them.
- Every scroll bar in the window comes from the shared look, not from a single screen, and the look test still finds no literal colour, size or spacing in the screens.
- Choosing a look, scrolling each list with the mouse wheel and by dragging the scroll bar, and selecting a category still work.
- The maintainer opens a statement and checks the switch and the scroll bars by hand, in both looks.
- Nothing in this item adds networking, telemetry or an unreviewed dependency ([decision 0006](../../docs/decisions/0006-privacy-approach-and-network-boundary.md)).

## Non-goals
- Moving the look switch or rearranging the header.
- Restyling other controls, or changing the look's colours, sizes or rounding.
- Reaching the look switch or the scroll bars with the keyboard, or Windows' contrast themes; these belong to a later accessibility item, as #11 left them.
- The charts' hover cards ([#20](../020-chart-hover-cards/intent.md)).

## Decisions
- **The fix belongs in the shared look**, not in a screen: #11 defined the look once, and the issue says so too. The screenshots are the review.
- **The rules card's switch ("Moje pravidlá" / "Vstavané pravidlá") gets the same hover.** It is the same kind of control in the same style, so both switches behave alike and the fix lives in one place. Decided by the maintainer.
- **Every scroll bar in the window follows the look:** the window's, the legend's, the "Kategórie" and transaction lists', the rule lists' and the choice boxes' lists'. The look is defined once; fixing one would leave the same mismatch next to it. Decided by the maintainer.
- **A scroll bar is always visible, but slim,** when its list can scroll, so a user can tell there is more below without hovering first. Decided by the maintainer.

## Open questions
None.
