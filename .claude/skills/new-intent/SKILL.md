---
name: new-intent
description: Start a work item from its GitHub issue - create the NNN-slug branch and work/NNN-slug/ folder, and draft intent.md (the WHY and the desired outcome, no solution). Use when the user picks up an issue, e.g. "let's start #12".
---

# new-intent

Turn a GitHub issue into the item's `intent.md`: why the item exists and what is true for the user once it is done. The intent is owned by the human (see `docs/sdlc.md`); you draft, they decide. It says nothing about how: no technology, libraries, file formats or code.

## Before writing

1. **Read the issue:** `gh issue view <N>`. Take the slug the issue suggests, or derive one: 2 to 5 lowercase hyphenated words. The folder and branch are `NNN-slug`, with the number zero-padded to three digits (issue 12 is `012-...`).
2. **Branch** from an up-to-date `main`: `git checkout main`, `git pull --ff-only`, `git checkout -b NNN-slug`. Never work on `main`. If a branch for the item exists, use it.
3. **Read the context** the intent must fit:
   - `docs/product.md` and `docs/mvp.md`: is the item in the MVP, out of it, or a future direction?
   - `docs/glossary.md`: use its terms, and use them consistently.
   - `docs/decisions/`: an intent must not contradict a decision; if it has to, say so as an open question.
   - Related items in `work/`, especially their non-goals, and the findings of earlier items (`work/*/findings.md`).
   - Never read `samples/private/`.

## Writing the intent

Copy `work/_templates/intent.md` to `work/NNN-slug/intent.md`, with front matter `status: draft` and `issue: N`, and a link to the issue under the title.

- **Problem:** who struggles with what today, in the user's terms. No solution.
- **Desired outcome:** what is true afterwards. Concrete enough to test, still free of technology. Sub-headings or bullets are fine when the outcome has parts.
- **Success criteria:** observable and checkable: "a user can …", "a file that … is rejected with …". Each one should later map to an acceptance criterion and a test, or to a named hand check.
- **Non-goals:** what this item deliberately leaves out, pointing to the issue or item that covers it when there is one.
- **Decisions:** questions already answered, by the issue or by the human during drafting, each with its reason.
- **Open questions:** what the human must decide. Give each a recommendation with a one-line reason, so they can answer "agree".

Write the way the existing intents are written: plain English, short sentences, no jargon; Slovak only where quoting what the user will see (decision 0003).

## With the human

1. Present a short summary: what the intent says, then the open questions with your recommendations. Don't paste the whole file.
2. Fold their answers in: an answered question moves to **Decisions**. Repeat until there are no open questions, or the human approves them as they are.
3. **Commit only after the human approves**, on the item branch: `Add intent for <short title> (#N)`, ending with the attribution lines the session asks for. Never push.
4. The next step is `write-spec`.

## Notes

- Keep items small enough to finish in one go. If the issue holds several items, suggest splitting before drafting.
- If the issue mixes in a solution, keep the need in the intent and leave the solution for the plan.
- Write files as UTF-8 without a byte-order mark. Windows PowerShell 5.1 adds one with `Set-Content -Encoding utf8`; use the editor tools or `[IO.File]::WriteAllText` with `UTF8Encoding($false)`.
