---
status: in-progress
issue: 11
---

# Intent: Give the MVP screens a modern look, light and dark

Issue: [#11](https://github.com/mDubovsky/DocuSpend/issues/11)

## Problem
DocuSpend's screens are a functional sketch from the Avalonia spike ([#5](../005-avalonia-spike/findings.md)): plain cards, default controls, colours picked in passing, and only a light look. It works, but it doesn't look like something a non-technical person would trust with their bank statement, and people who use dark mode get a bright window. Each new screen built on the sketch would inherit that, and restyling many screens later costs more than settling the look now.

## Desired outcome
DocuSpend has one consistent, modern look in two variants, light and dark, in the spirit of current finance dashboards:

- **Dark:** deep navy or near-black backgrounds, rounded cards with soft depth, and one accent colour (blue or violet) for selections, highlights and the chart line.
- **Light:** the same feel, with white cards on a pale background.
- **Numbers first:** the key figures (balance, spending total) are large and bold; labels are quieter and muted.
- **Charts that belong to the look:** a smooth balance line with a soft gradient fill and a highlighted point with a small callout; pie colours that are distinct and pleasant in both looks.
- **Calm and consistent:** generous spacing, pill-shaped selected items, and the same styles for every list, form, button and message.
- **Light or dark:** DocuSpend follows Windows' light or dark setting, including when it changes while DocuSpend runs. A switch in the application overrides it.

The look applies to the screens that exist: the statement view with both charts, the category and transaction lists, the rule form and the rule list. It is defined once, so the screens built later (opening a statement, the rule editor) pick it up rather than restyle themselves.

## Success criteria
- The maintainer approves screenshots of every existing screen in both looks before the item is done.
- Every screen uses the shared look: no colour, font size or spacing is chosen in a single screen.
- Text is readable in both looks: normal text has a contrast of at least 4.5 to 1 against its background, and large text and chart lines at least 3 to 1 (the WCAG AA levels).
- Slovak diacritics and amounts display correctly in both looks, in the window and inside the charts.
- Changing Windows between light and dark while DocuSpend runs switches the window and the charts, with nothing left in the old colours.
- The switch in the application overrides Windows' setting, and setting it back to "follow Windows" follows it again.
- The choice is still in effect after DocuSpend is closed and opened again.
- Everything that works today still works, in both looks: selecting a category in the pie or the list, adding a rule, switching samples.
- Nothing about the look adds networking, telemetry or an unreviewed dependency (decision 0006).

## Non-goals
- How the screens are arranged and navigated: what is on the main screen, where the rule editor lives. That comes with the items that build those screens.
- New features, and new screens.
- An application icon, a logo, branding.
- Decorations from dashboards that DocuSpend has no use for: profile pictures, notifications, product images, card graphics.
- Copying any particular reference design. The references set a direction, not a template.
- Animations beyond what the toolkit and charts do by default.

## Decisions
- **The light or dark choice is remembered** when DocuSpend is closed and opened again. It is a single setting on the user's computer, saved by this item on its own, without waiting for the rules work in [#13](https://github.com/mDubovsky/DocuSpend/issues/13). It holds only the choice: nothing about statements.
- **The switch lives in the window's header** for now, as a small control. It can move when the screens are arranged.
- **Windows' own font.** It has every Slovak letter and needs no review; a bundled font would be a new dependency to review for licence and privacy, which the look doesn't need.

## Open questions
None.
