# Repo setup files

These belong in `.github/` and `.claude/`, which Claude's remote tools are not allowed to write on your computer. Move them once from the repo root:

```bash
mkdir -p .github/workflows .claude
git mv deploy/repo-setup/github/workflows/ci.yml        .github/workflows/ci.yml
git mv deploy/repo-setup/github/pull_request_template.md .github/pull_request_template.md
git mv deploy/repo-setup/github/CODEOWNERS              .github/CODEOWNERS
git mv deploy/repo-setup/claude/settings.json           .claude/settings.json
git commit -m "chore: move CI, PR template, CODEOWNERS and Claude settings into place"
```

Then replace `@RAHUL_GITHUB` / `@MONIKA_GITHUB` in `CODEOWNERS` and delete this folder.
