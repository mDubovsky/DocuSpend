# Development workflow (AI SDLC)

Every change moves through the same stages. Each stage produces a markdown file in the item's folder, `work/<NNN-slug>/`, where NNN is the item's GitHub issue number.

| Stage | File | Answers | Owner |
|---|---|---|---|
| Intent | `intent.md` | Why do this, and what outcome do we want? No solution. | Human |
| Spec | `spec.md` | What will it do, observably? Acceptance criteria. | AI drafts, human approves |
| Plan | `plan.md` | How will we build it? Approach, steps, risks, verification. | AI drafts, human approves |
| Build | code | | AI implements, human reviews |

## Rules

- No implementation before the spec and plan are approved.
- Keep items small enough to finish in one go. Split when in doubt.
- Technology choices live in `plan.md`, never in intent or spec.
- Unit tests are a core part of the product, not an afterthought. Each acceptance criterion in a spec must map to automated tests, and `plan.md` must say how. Code is not done without them.
- Test data is synthetic: realistic fake statements in `samples/public/` (committed), never real financial data.
- If reality diverges from a doc, update the doc in the same change.
- `intent.md` carries a front-matter `status:` field: `draft | specced | planned | in-progress | done`.

## Issues and the repo

- **GitHub issue = idea and tracking.** Every item starts as an issue: a short description, discussion, triage, labels. The `new-issue` skill helps draft one.
- **`work/` folder = the item itself.** Once an issue is picked up, its `intent.md`, `spec.md` and `plan.md` live in the repo, where they are versioned and reviewed with the code.
- **The issue number is the item number.** Issue #42 titled "Synthetic statement generator" becomes `work/042-synthetic-statement-generator/` and branch `042-synthetic-statement-generator`. The part after the number is the *slug*: a short lowercase hyphenated label, chosen once and never renamed.
- **Single source of truth:** the repo owns the intent, spec, plan and `status:`. GitHub owns discussion, triage, coarse labels (`idea`, `ready`, `blocked`) and open/closed. Don't copy the intent into the issue; link to the folder.
- Docs-only or trivial changes (typos, small doc fixes) don't need an issue or work folder. Anything that changes behavior does.

## Branching and PRs

1. Start from an issue. Create branch `NNN-slug` from an up-to-date `main`. Never work or commit on `main`.
2. Commit the stages in order, each after human approval: intent, spec, plan, then code and tests in small commits. Update `status:` as you go.
3. Open a PR into `main` that says `Closes #NNN`. One PR per item by default. For large items, merge a docs-only PR (intent, spec, plan) first, then a separate code PR.
4. A human reviews and merges with a squash merge, so `main` has one commit per item. The stage-by-stage history stays visible in the PR.
5. Protect `main` on GitHub (PR required, no direct pushes) once more than one person contributes. While the repo is private and single-contributor, the maintainer may merge item branches into `main` locally instead of through a PR; the rule that nobody works directly on `main` still applies.

## Layout

- `work/_templates/`: skeletons for `intent.md`, `spec.md`, `plan.md`
- `work/NNN-slug/`: one folder per backlog item (see [work/README.md](../work/README.md))
- `.claude/skills/`: skills that help with each stage: `new-issue`, `new-intent`, `write-spec`, `write-plan`
- `.github/`: issue and PR templates

## Git

Agents never commit without human approval, never push, and never work on `main`.
