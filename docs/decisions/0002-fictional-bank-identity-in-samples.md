# 0002: Samples copy a bank's layout, never its identity

Status: accepted

## Context
The synthetic statements in `samples/public/` reproduce the layout of real VÚB statements so that a reader tested on them works on real ones ([work/001-synthetic-statements](../../work/001-synthetic-statements/intent.md)). A first version also reproduced VÚB's identity verbatim: company name, registered address, registry court and number, IČO, website, BIC, bank code and product names.

Realistic statements carrying a real bank's full legal identity, in a public repository, with the generator next to them, look like official documents of that bank. That invites misuse (for example as fake proof of income) and goes beyond describing compatibility with the bank's format. A small margin marking doesn't change that, since it's easy to crop.

## Decision
- Samples copy the **layout** of a real bank's statement exactly: page structure, positions, fonts' character grid, labels, row structure and formats.
- Samples never carry the real bank's **identity**. They use an invented bank instead: name, legal line, IČO, website (on the reserved `.example` domain), BIC, bank code and product names are all fictional (`tools/DocuSpend.SampleGenerator/Rendering/FictionalBank.cs`). No logo is drawn.
- Every sample is marked "SYNTHETIC TEST DATA – NOT A REAL STATEMENT" in three places: the left margin, a footer on every page, and the document metadata.
- Naming the bank to describe what DocuSpend supports ("reads VÚB statements") is fine and continues.

## Consequences
- Readers must recognize a statement format by its **structure** (title, table headers, column positions), not by the bank's name, IČO or BIC. That is more robust anyway, and it's the only thing the samples can test.
- Any identity-based check a reader adds (e.g. on the bank's BIC) can't be tested with the samples and needs to be kept small and documented.
- A test fails if any valid sample contains the real bank's identity.
- The same rule applies to samples for other banks added later.
