#!/usr/bin/env bash
# One command to check the repo. Exits non-zero on any failure.
# Skips parts that don't exist yet so it works from Day 0.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ -f SlateAi.sln ]; then
  echo "==> dotnet build"
  dotnet build SlateAi.sln --nologo -warnaserror
  echo "==> dotnet test (excluding live evals and Db tests)"
  dotnet test SlateAi.sln --nologo --no-build --filter "Category!=Eval&Category!=Db"
else
  echo "==> skip .NET (no SlateAi.sln yet)"
fi

if [ -f src/web/package.json ]; then
  echo "==> web: install, lint, typecheck, test, build"
  (cd src/web && npm ci --no-audit --no-fund && npm run lint && npm run typecheck && npm test -- --run && npm run build)
else
  echo "==> skip web (no src/web/package.json yet)"
fi

echo "verify: OK"
