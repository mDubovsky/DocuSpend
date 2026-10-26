---
name: write-spec
description: Draft spec.md for a work item from its approved intent.md - what the user can do and observe, acceptance criteria, edge cases. Use after the intent is committed, e.g. "write the spec for 012-open-statement".
---

# write-spec

Turn an approved intent into `work/NNN-slug/spec.md`: what DocuSpend will do, observably, precisely enough that every acceptance criterion can become a test. The AI drafts; the human approves (`docs/sdlc.md`). A spec still says nothing about technology: no libraries, classes or file formats. Those belong in the plan.

## Before writing

1. Be on the item's branch. Read `work/NNN-slug/intent.md`; it must be committed and approved. If it still has open questions, resolve them with the human first.
2. Read the context: `docs/mvp.md`, `docs/glossary.md`, the decisions in `docs/decisions/`, and the specs of related items in `work/`. Keep their terms and don't contradict them silently.
3. **Check facts against the code.** When the spec names something that exists (a category, a rejection kind, a sample, a rule condition, a check name), look it up in `src/`, `samples/public/` or `privacy/` rather than writing it from memory. Read `samples/public/*.expected.json` for what the samples contain. Never read `samples/private/`.

## Writing the spec

Copy `work/_templates/spec.md`. Write `Intent: [intent.md](intent.md)` under the title.

- **Behavior:** what the user (or contributor, or visitor, if the item is for them) can do and observe, grouped by sub-headings. Describe what they see, in their terms; quote visible Slovak texts exactly when they matter (decision 0003).
- **Screens:** for an item that adds or changes anything on screen, a rough text sketch of each new or changed screen or card, in a fenced block: what is shown where, what is hidden until asked for (a number, a second list, a detail), and how each question or confirmation appears (inside a row, under a field, or as a pop-up box over the window). Sketch the choices that are cheap to change now and costly after the build. Say in the summary to the human that the sketches are there to look at, so layout is settled before any code is written.
- **Acceptance criteria:** a checklist, one observable criterion per line, in the "given …, when …, then …" form where it helps. Each must be checkable: mark the ones that can only be checked by hand, and say who checks them. Cover every success criterion of the intent.
- **Edge cases and errors:** odd, missing or invalid input; empty months; many items; long names; failures to read or write; what the user sees in each case. Nothing may fail silently or produce a wrong result without saying so.
- **Out of scope:** pointing to the issue or item that covers each part when there is one.
- **Decisions:** choices made while specifying, with the reason (for example "the balance line stays stepped, because a smoothed line would show balances that never existed").
- **Open questions:** ideally none by approval. Each one with a recommendation.

Write plainly and briefly, like the existing specs. Prefer one exact sentence to three approximate ones.

## With the human

1. Summarize the spec in a few bullets, then list the decisions you made that they should check, so they don't have to read the whole file to find them. If the spec has screen sketches, show them in the summary too: layout changes after the build cost the most rework.
2. Revise until approved. If the human later changes behavior during the build (a review of screenshots, a hand check), update the spec in the same change: the spec describes what was built.
3. **After approval:** set the intent's `status:` to `specced`, and commit both on the item branch: `Add spec for <short title> (#N)`. Never push.
4. The next step is `write-plan`.
