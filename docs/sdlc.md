# Development workflow (AI SDLC)

Every change moves through the same stages. Each stage produces a markdown file in the item's folder, `work/<NNN-slug>/`.

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

## Layout

- `work/_templates/`: skeletons for `intent.md`, `spec.md`, `plan.md`
- `work/NNN-slug/`: one folder per backlog item (see [work/README.md](../work/README.md))
- `.claude/skills/`: skills that help with each stage: `new-intent`, `write-spec`, `write-plan`

## Git

Agents never commit without human approval and never push.
