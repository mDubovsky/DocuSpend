# Spec: The look switch and scroll bars follow the look

Intent: [intent.md](intent.md)

## Behavior

What the switches and lists do stays the same. What changes is how the two switches and every scroll bar look, in light and dark. "The two switches" are the look switch in the header ("Podľa systému", "Svetlý", "Tmavý") and the rules card's switch ("Moje pravidlá", "Vstavané pravidlá").

### The look switch keeps its shape
- The look switch is the same size and shape on every screen: on the start screen, with a statement open, and while a message is shown in the header.
- The chosen look is a pill inside the switch's own pill. Its text is centred in it, across and up and down.
- When something else in the header is taller than the switch (the "Otvoriť výpis…" button), the switch keeps its own height and sits centred beside it. It doesn't stretch.

### The switches react to the pointer
- When the pointer is over a choice that isn't chosen, that choice gets a soft pill of its own, in a tone that stands out from the switch's background. It goes away when the pointer leaves.
- The chosen choice keeps its accent pill when the pointer is over it, as the selected row in the "Kategórie" list does.
- Both switches behave the same way. Clicking a choice works as before.

### Scroll bars belong to the look
- Every scroll bar in the window looks the same: the window's own, the legend's beside the ring, the "Kategórie" and transaction lists', the rule lists', and the lists that open from the rule form's choice boxes.
- A scroll bar is a slim, rounded handle on a faint track, with no arrow buttons at its ends. The handle uses a muted tone of the look and becomes stronger when the pointer is over it or while it is dragged.
- A scroll bar shows whenever its list has more than fits, without waiting for the pointer. When everything fits, it isn't shown.
- A scroll bar sits in its own narrow strip at the list's edge. It never covers the list's content, so right-aligned amounts stay readable.
- Scrolling works as before: the mouse wheel, dragging the handle, and clicking the track.

### Both looks
- In light and dark, the switches and the scroll bars use the look's colours. Changing the look repaints them at once, with nothing left in the old colours.
- The scroll bar's handle has a contrast of at least 3 to 1 against what is behind it, like the other non-text parts of the look.

### Screenshots for review
- The screenshots from #11 show the look switch on the start screen and with a statement open, in both looks. New screenshots, in both looks, show:
  - a choice of each switch with the pointer over it;
  - a list long enough to scroll, with its scroll bar.

## Screens

The header, before and after a statement is opened. The switch is the same in both, centred up and down:

```
Start screen
+--------------------------------------------------------------------------------------------------+
|                                                     Vzhľad  ( (Podľa systému)  Svetlý  Tmavý )    |
+--------------------------------------------------------------------------------------------------+

Statement open: the button makes the row taller; the switch keeps its height and sits centred
+--------------------------------------------------------------------------------------------------+
| +------------------+                                                                             |
| | Otvoriť výpis…   |  <period>  [Ukážka s vymyslenými údajmi]  Vzhľad  ( (Podľa systému)  Svetlý  Tmavý ) |
| +------------------+                                                                             |
+--------------------------------------------------------------------------------------------------+
```

A choice under the pointer, beside the chosen one:

```
( (Podľa systému)  [Svetlý]  Tmavý )
   accent pill      soft pill under the pointer
```

A list that scrolls, with its scroll bar in its own strip:

```
+-- Kategórie ---------------------------------+
|  Potraviny                       142,30 €  ┃ |
|  Doprava                          58,00 €  ┃ |
|  Reštaurácie a kaviarne           41,90 €  │ |
|  Bývanie a energie                35,00 €  │ |
+----------------------------------------------+
   ┃ the handle: slim, rounded      │ the faint track
```

## Acceptance criteria

- [ ] Given the start screen and the statement screen, in each look, the look switch's chosen pill has the same size, and its text is centred in it.
- [ ] Given a statement is open, the look switch is no taller than on the start screen and is centred up and down in the header row.
- [ ] Given either switch, when the pointer is over a choice that isn't chosen, that choice shows a pill whose tone differs from the switch's background, in each look; when the pointer leaves, it goes away.
- [ ] Given either switch, when the pointer is over the chosen choice, it keeps its accent pill.
- [ ] Clicking a choice in either switch still changes the look or the shown rules, as before.
- [ ] Every scroll bar in the window takes its look from the shared look; no screen styles a scroll bar of its own, and the look test still finds no literal colour, size or spacing in the screens.
- [ ] Given each look, the scroll bar handle has a contrast of at least 3 to 1 against the card behind it, checked automatically.
- [ ] Given a list with more rows than fit, its scroll bar is shown without the pointer over it; given a list where every row fits, no scroll bar is shown.
- [ ] Given a list that scrolls, its rows are narrower than the list by the scroll bar's strip, so the scroll bar covers no row.
- [ ] Each list still scrolls with the mouse wheel, by dragging the handle, and by clicking the track.
- [ ] Changing the look while a list is scrolled, or while the pointer is over a switch, repaints the scroll bar and the switch in the new look and keeps the scroll position.
- [ ] One command produces screenshots, in both looks, of the look switch on both screens, of a choice of each switch under the pointer, and of a list with its scroll bar. *Checked by hand: the maintainer approves them.*
- [ ] *Checked by hand, by the maintainer:* with a real statement open, in both looks, the look switch keeps its shape, both switches react to the pointer, and the scroll bars look like part of DocuSpend.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.

## Edge cases and errors

- **A small window.** The window's own scroll bar appears and looks like the others. The header can wrap or be cut by the window's width as today, but the switch keeps its shape.
- **Display scaling** (125 %, 150 %). The pills and the scroll bar stay crisp and keep their proportions; the switch's text stays centred.
- **A list that only just overflows** (one row more than fits). The scroll bar shows; the handle is long but still rounded and draggable.
- **A very long list** (the built-in rules). The handle has a smallest size, so it stays easy to grab.
- **The pointer leaves the window while it is over a switch or a scroll bar.** The hover pill and the stronger handle go away; nothing stays highlighted.
- **A choice box's list opens near the bottom of the window.** Its scroll bar looks like the others.

## Out of scope

- Moving the look switch or rearranging the header.
- Restyling other controls, or changing the look's colours, sizes or rounding beyond what the switches and scroll bars need.
- Reaching the switches or the scroll bars with the keyboard, and Windows' contrast themes; a later accessibility item, as #11 left them.
- Horizontal scrolling; no part of the window scrolls sideways.
- The charts' hover cards ([#20](../020-chart-hover-cards/spec.md)).

## Decisions

- **A switch's hover has its own tone.** The switch's background already uses the soft tone that list rows use for hover, so the same tone on a choice would be invisible. The hover needs a tone that stands out against the switch.
- **The chosen choice doesn't change under the pointer**, like the selected row in the "Kategórie" list, so the accent always means "this is chosen".
- **No arrow buttons on scroll bars.** They are a desktop habit the cards and pills don't use; the wheel, the handle and the track cover scrolling.
- **The scroll bar has its own strip** rather than lying over the content, because the lists end in right-aligned amounts that a scroll bar on top would cover.
- **The handle meets 3 to 1 contrast**, the level #11 set for the look's other non-text parts, so a user can see that a list scrolls.

## Open questions

None.
