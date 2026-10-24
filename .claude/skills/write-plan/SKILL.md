---
name: write-plan
description: Draft plan.md for a work item from its approved spec.md - the technical approach, steps, risks, and how every acceptance criterion is verified. Use after the spec is committed, e.g. "write the plan for 012-open-statement".
---

# write-plan

Turn an approved spec into `work/NNN-slug/plan.md`: how the item will be built and checked. This is the only stage where technology is chosen (rule 8 in `AGENTS.md`). The AI drafts; the human approves. No code is written until the plan is approved.

## Before writing

1. Be on the item's branch, with `intent.md` and `spec.md` committed and approved.
2. Read `AGENTS.md`, the decisions in `docs/decisions/`, `privacy/README.md`, and the plans and findings of related items. Read the code the plan builds on: the plan must fit what exists, not what you remember.
3. **Verify before planning.** Check every claim the plan will rest on:
   - Package versions and their dependencies on NuGet (`https://api.nuget.org/v3-flatcontainer/<id>/index.json`, and the `.nuspec`), with dates, so the plan names exact, current, compatible versions.
   - That an API or capability exists in that version (read the assembly's metadata, or try it in a throwaway project in the scratchpad), rather than assuming it from documentation or memory.
   - Anything that could decide the approach (a format, a limit, a contrast ratio, a size): measure it.
   Record what was verified, and when, in the plan.
4. **Privacy first.** Every new dependency, tool or shipped file falls under decision 0006 and the checks in `privacy/`. Find out what a new package brings (build steps, telemetry, networking references, native files) before choosing it. Never plan to suppress or work around a check. Entries in `privacy/allowed-dependencies.json`, `privacy/native-libraries.json` or `privacy/exceptions.json` are proposed with a reason and added only after the human reviews them; plan that review as a step where it will be needed.

## Writing the plan

Copy `work/_templates/plan.md`. Write `Spec: [spec.md](spec.md)` under the title.

- **Approach:** the chosen design and why, organised by part, with exact versions. Put choices the human should know about first (a version that isn't the newest, a dependency with telemetry, a check that changes).
- **Alternatives considered:** each with the reason it lost.
- **Layout:** the new folders and files.
- **Prerequisites:** items that must be merged, and anything the human must provide or do (push, review, a hand check).
- **Steps:** small, ordered, test-first for logic. One commit per step, each after the human's approval. Mark the steps that stop for a review (privacy findings, screenshots, colours, hand checks). The last step updates the docs and sets the intent to `done`.
- **Risks and unknowns:** with the fallback for each.
- **Verification:** a table mapping every acceptance criterion of the spec to its automated test or named hand check. No criterion may be left without one.

## With the human

1. Summarize the approach, the verified facts that shaped it, and the stop points; call out any decision the human must make (for example between two versions) and ask with a recommendation.
2. Revise until approved. **After approval:** set the intent's `status:` to `planned`, commit on the item branch: `Add plan for <short title> (#N)`. Never push.
3. Start building only after that. Set `status: in-progress` with the first build commit, and commit each step only after the human approves it. If the build changes the plan or the spec, update them in the same change.
