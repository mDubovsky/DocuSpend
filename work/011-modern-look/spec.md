# Spec: Give the MVP screens a modern look, light and dark

Intent: [intent.md](intent.md)

## Behavior

The screens keep what they show and how they work. What changes is how they look, in two variants, and a way to choose between them. "Screens" here are the ones that exist: the header with the sample choice, the spending chart, the balance chart, the category list, the transaction list, the rule form and the rule list.

### One look, defined once
- The look is a single, named set of choices that every screen uses: background, card, accent, text tones, the colours for money in and money out, the colours of the chart slices, corner rounding, spacing, and the text sizes for headings, key numbers, normal text and labels.
- No screen picks a colour, a text size, a corner rounding or a spacing value of its own. A screen built later uses the same set and looks like it belongs without extra work.

### Dark
- The window background is a deep navy, almost black. Content sits on cards that are a little lighter, with rounded corners and a soft shadow or edge that separates them from the background.
- One accent colour, a blue leaning towards violet, marks what is selected or active: the selected row, the primary button, the balance line, focus.
- Text comes in two tones: bright for content and key numbers, muted for labels, dates and secondary details.

### Light
- The same layout and feel with a pale grey-blue window background, white cards and the same accent colour, adjusted where needed so it stays readable on white.

### Numbers first
- The header shows the key figures large and bold: the spending total most prominently, then the opening and closing balance. Their labels ("Výdavky spolu", "Konečný zostatok") are small and muted above them.
- Amounts are right-aligned in lists, so they line up.
- Money coming in is shown in a calm positive colour (a green); money going out in the normal text colour, not in red, because spending is the normal case, not an alarm.

### Charts
- **The spending chart** is a ring (a pie with a hole), with the spending total in its centre. The ring has a fixed space of its own, so it keeps its size however many categories a month has. Each slice has its own colour from a palette of distinct colours that work in both looks; the colours are the same in both looks, so a category keeps its colour. Hovering a slice shows the category, the amount and the share in a small card in the look's style. Clicking a slice selects the category, as now.
- **The legend** is beside the ring, not inside the chart: one row per category with its colour, name, share and amount. It scrolls when there are more rows than fit, and a long name is shortened, with the full name shown on hover. Clicking a row selects the category, like clicking its slice.
- **The balance chart** is a line in the accent colour with a soft fill fading downwards from it, without point markers. It still steps from day to day: the balance changes on booking dates and holds in between, and a smoothed curve would show balances that never existed. Hovering shows a small card with the date and the balance.
- Grid lines are faint, axis labels muted, and there is no chart border.

### Lists and forms
- Rows in the category and transaction lists have room to breathe, and change background softly when the pointer is over them. The selected category row is marked with the accent colour, as a rounded "pill".
- The rule form's fields, choice boxes and button have rounded corners in the look's style. "Pridať pravidlo" is the one primary button, filled with the accent colour.
- Messages have their own style: an error in a soft red with a matching tint behind it, a confirmation in the positive green, both readable in both looks.
- Empty states ("V tomto mesiaci neboli žiadne výdavky.", "Vyberte kategóriu…", "Zatiaľ žiadne pravidlá.") are muted and centred in their card.

### Choosing light or dark
- The header has a small control with three choices: **Podľa systému** (follow Windows), **Svetlý** (light), **Tmavý** (dark). The first is the default.
- With "Podľa systému", DocuSpend starts in Windows' current light or dark setting and follows it when it changes while DocuSpend runs.
- Changing the choice changes the whole window at once: every card, text, control, message and both charts, with nothing left in the old colours. Selections, typed text in the form, and the shown statement stay as they were.
- The window's title bar follows the look where Windows allows it.

### The choice is remembered
- The choice is saved on the user's computer, in DocuSpend's folder in their own application-data folder, as soon as it changes, and used the next time DocuSpend starts.
- The saved setting holds only the choice. Nothing about statements, rules or the user.
- If the setting can't be read (missing, damaged, from another version), DocuSpend follows Windows and starts normally; the next change of the choice replaces it. Unlike rules, a lost preference costs the user one click, so it is not protected the way the rules file is.

### Readability
- Normal text has a contrast of at least 4.5 to 1 against what is behind it, in both looks. Large text (the key numbers, headings), the accent on its background, chart lines and slice colours against the card have at least 3 to 1.
- Slovak diacritics and amounts display correctly in both looks, in the window and inside the charts.

### Screenshots for review
- Screenshots of every screen in both looks can be produced with one command, from the synthetic samples, without anyone clicking through the application. The maintainer reviews them; the same screenshots show later whether a change altered the look.

## Acceptance criteria

- [ ] Given each look, every colour, text size, corner rounding and spacing value used by the screens comes from the shared look; a check fails if a screen defines one of its own.
- [ ] Given each look, every pair of text and background the look defines meets 4.5 to 1, and every large text, accent, chart line and slice colour meets 3 to 1 against its card, checked automatically.
- [ ] Given each look, the header shows the spending total, the opening and the closing balance as large key figures, with muted labels.
- [ ] Given a statement with income, incoming amounts are shown in the positive colour and outgoing ones in the normal text colour.
- [ ] The spending chart is a ring with the spending total in its centre, in a fixed space that a long legend doesn't shrink; each category has the same colour in both looks; hovering shows the category, amount and share; clicking selects the category.
- [ ] The legend beside the ring lists every slice with its colour, name, share and amount, and clicking a row selects its category.
- [ ] The balance chart is a stepped line in the accent colour with a fading fill and no point markers, and hovering shows the date and the balance.
- [ ] The selected category row is marked with the accent colour; the primary button is the only filled button.
- [ ] Error and confirmation messages in the rule form are styled and readable in both looks.
- [ ] Given "Podľa systému", when Windows switches between light and dark while DocuSpend runs, the whole window including both charts follows.
- [ ] Given "Svetlý" or "Tmavý", Windows' setting is ignored; switching back to "Podľa systému" follows it again.
- [ ] Changing the look keeps the selected category, the typed rule form and the shown statement.
- [ ] Given a choice, when DocuSpend is closed and started again, the same choice is in effect.
- [ ] Given a missing or damaged setting, DocuSpend starts and follows Windows; the next change of the choice is saved normally.
- [ ] Every behavior that works today works in both looks: choosing a sample, selecting a category in the chart or the list, adding each kind of rule, invalid input, the empty month.
- [ ] One command produces screenshots of every screen in both looks from the samples; the maintainer has approved them.
- [ ] All privacy checks pass, with no new entries in the reviewed lists.

## Edge cases and errors

- **The empty month (`04`).** The ring is replaced by its muted empty message, the balance line is flat; both look intentional in both looks.
- **Many categories (`02`).** The palette has enough distinct colours for every category the built-in set has; colours don't repeat within one chart.
- **Long names.** Long category names and descriptions are shortened with an ellipsis, never overlapping.
- **Windows' contrast themes** (for people who need very high contrast) are not specially supported in this item. DocuSpend keeps its own look; the finding is noted for a later accessibility item.
- **Display scaling** (125 %, 150 %) and **a small window**: rounded corners, shadows and charts stay crisp and laid out.
- **Switching the look while a tooltip or a choice box is open.** It closes or repaints in the new look; nothing stays in the old colours.
- **The setting can't be written** (a full disk, no permission). The choice still applies for this session, and DocuSpend doesn't show an error for it.

## Out of scope

- How the screens are arranged and navigated, and any new screen or feature.
- An application icon, a logo, branding.
- A bundled font; the look uses Windows' own font (intent: decisions).
- Profile pictures, notifications, product images, card graphics.
- Animations beyond the defaults of the toolkit and the charts.
- Special support for Windows' contrast themes.
- macOS and Linux.

## Decisions

- **A ring instead of a full pie.** It is the same chart for the purpose (shares of spending), the hole gives the spending total a natural place, and it matches the references. The MVP's "pie chart" is met by it.
- **The balance line stays stepped.** Smooth curves are part of the references' look, but a smoothed balance would show amounts the account never had. The softness comes from the fill and the colours instead.
- **Spending is not red.** Red is kept for errors, so it keeps meaning "something is wrong".
- **Each category keeps its colour in both looks**, so switching looks doesn't reshuffle what the user has learnt.

## Open questions

None.
