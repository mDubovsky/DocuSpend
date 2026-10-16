# Work items

One folder per backlog item: `work/NNN-short-slug/`, where `NNN` is the item's GitHub issue number, zero-padded to three digits (issue #42 becomes `042`). The slug is a short lowercase hyphenated label, chosen once and never renamed.

```
work/042-synthetic-statement-generator/
├── intent.md   # required, starts the item
├── spec.md     # after intent is agreed
└── plan.md     # after spec is approved
```

To start a new item, create the issue first (the `new-issue` skill can help), then copy `_templates/intent.md` into a new folder (or use the `new-intent` skill) on a branch with the same name. Workflow details: [docs/sdlc.md](../docs/sdlc.md).
