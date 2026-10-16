---
name: new-issue
description: Turn a rough idea into a well-formed GitHub issue (title and description) through a short back-and-forth conversation. Use when the user has an idea, bug, or request they want to capture as an issue.
---

# new-issue

Help the user turn a rough idea into a GitHub issue. The issue captures the WHAT and WHY only. No technology or implementation.

## Process

1. **Listen first.** Restate the idea in one or two sentences and check you understood it.
2. **Ask a few focused questions, one or two at a time.** Cover only what is missing:
   - Who has this problem, and what happens today?
   - Why does it matter? What is the value of solving it?
   - What would be true afterwards? (the outcome, not the solution)
   - What is clearly out of scope?
   - How does it relate to `docs/mvp.md` and existing issues or `work/` items?
3. **Push back gently** if the idea is really several items (suggest splitting), mixes in a solution (move it to "Rough idea"), or is too vague to act on.
4. **Draft the issue** when you have enough, and show it for approval:
   - **Title:** short, 3 to 8 words, describing the outcome or problem, not the solution. No prefixes.
   - **Description:** follow `.github/ISSUE_TEMPLATE/idea.md` (Problem, Why it matters, Rough idea, Notes). For bugs, follow `bug.md`.
   - **Suggested label:** `idea` or `bug`.
   - **Suggested slug:** 2 to 5 lowercase hyphenated words, for the future `work/NNN-slug/` folder and branch.
5. **Iterate** until the user is happy.

## Output

Present the final title, description (as markdown the user can paste), label and slug.

Do not create the issue yourself unless the user asks and `gh` is available. Creating an issue publishes it, so confirm first. Never include real financial data, and never read `samples/private/`.

After the issue exists, the next step is a branch `NNN-slug` and the `new-intent` skill.
