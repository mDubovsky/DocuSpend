---
name: finish-item
description: Close a work item after its build and hand checks - set the intent to done, bring README, AGENTS.md, the glossary and the plan up to date, commit, and once the human has pushed, open the PR. Use when the human says the hand check passed or "finish the item", and again when they say "pushed, create the PR".
---

# finish-item

The last step of an item, after every plan step is built and the human has done the hand checks. It has two parts: the closing commit, then the PR once the human has pushed. Never push.

## Before closing

1. Be on the item's branch. Every plan step is committed, and the human has approved the review stops and hand checks.
2. Run `dotnet test`; everything passes.
3. Read the item's `intent.md`, `spec.md` and `plan.md` once more. If the build or a review changed behavior, the spec, intent and plan must already say so (`docs/sdlc.md`: the docs describe what was built). Fix any that don't.

## The closing commit

- **`intent.md`:** `status: done`.
- **`plan.md`:** at the end of "Risks and unknowns", one bullet per surprise, each starting **Found in the build:**, **Found in the screenshot review:** or **Found in the hand check:**. Say what happened and what was done about it, in a sentence or two. If a hand check found nothing, say so. See `work/013-keep-rules/plan.md` for the shape.
- **`README.md`:** the "What works today" sentence gains what this item added, in a user's words. Its "… is next" names the next item (see "Choosing the next item" in `docs/sdlc.md`), linked to its issue.
- **`AGENTS.md`:** only what a future agent must know and can't see quickly in the code: a file that must never change, a bound, a convention. Keep it to a sentence.
- **`docs/glossary.md`:** a term whose meaning grew or changed.
- **`PRIVACY.md`:** only if what DocuSpend keeps, or touches, changed.

Commit on the item branch: `Mark <short title> as done (#N)`, with the session's attribution lines. Then tell the human it is ready to push. Don't push.

## The PR

When the human says they have pushed, open the PR with `gh pr create --base main --head NNN-slug`, titled with the issue's title. Write the body to a file and pass it with `--body-file`. Follow `.github/pull_request_template.md`, in the shape of the earlier PRs (`gh pr view <number>` on the last merged one):

- `Closes #N` first.
- **Summary:** one or two sentences on what users can now do. Then a bullet per part, each starting with a bold label. Mention what a review changed, so the reviewer isn't surprised that the code differs from the issue. End with dependencies and the privacy lists ("No new dependencies. Privacy lists unchanged.", or what changed and who reviewed it).
- `Intent, spec and plan: [work/NNN-slug/](work/NNN-slug/)`.
- **Checklist:** tick each box that holds. For the tests box, name the checks the spec leaves to hand checks and say the maintainer did them.
- The attribution line the session asks for.

Report the PR's link. The item branch is kept after merging; never delete it.
