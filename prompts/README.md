# Prompts

One file per Claude call (see `docs/spec.md` §5.1), loaded at runtime by `Ai/PromptLibrary` (a bad header stops startup).
Each file starts with a header:

```
---
id: a1-lesson-mapping
version: 1
model_key: Mapping
---
```

Bump `version` on any change and attach an eval run to the PR. Use `{child}` / `{teacher}` placeholders; never real names.
