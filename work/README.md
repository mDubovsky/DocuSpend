# Work items

One folder per backlog item: `work/NNN-short-slug/`, with a zero-padded sequence number (e.g. `001-import-statement`).

```
work/001-example/
├── intent.md   # required, starts the item
├── spec.md     # after intent is agreed
└── plan.md     # after spec is approved
```

To start a new item, copy `_templates/intent.md` into a new folder (or use the `new-intent` skill). Workflow details: [docs/sdlc.md](../docs/sdlc.md).
