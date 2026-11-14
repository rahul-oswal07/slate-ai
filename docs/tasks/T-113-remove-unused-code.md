# T-113 · Remove unused code

| | |
|---|---|
| Status | Done |
| Issue | #221 |
| Owner | R |
| Screens | none (no behaviour change) |
| Source | Rahul, 15 Nov; small |

## What

Remove code that nothing uses, in the API, the web app and the locale files, without changing what any teacher or family sees.

Removed:

- `ITodaysMessageStore.GetApprovedAsync` and its EF implementation. Nothing called it. The evening send attaches today's message through `IEveningStore.GetApprovedTodaysMessageAsync` (`EveningSender`), which is where guardrail 3 is enforced, and that is unchanged. The test fake's `ApprovedFor` keeps the same "approved at its current version" rule inline.
- `CurriculumGraph.InChapter` and `CurriculumGraph.Ancestors`, which only tests called. The private prerequisite walk stays because `CandidateSet` uses it, and the `Candidate_set_*` tests cover the transitive, diamond and shortest-path cases.
- The unused `api` parameter of the private `WaitAsync` helper in `Db/CatchUpApprovalDbTests.cs` and `Db/ParentDbTests.cs`.
- Locale keys `parent.stop.bodyVideo` (the demo-reset text left in the wrong section, see T-83) and `demo.message.sentAt`, in all five locales.

## Acceptance criteria

- No behaviour change: the API tests, Vitest, the Db suite and the e2e specs pass as before, minus the tests of the removed methods.
- Guardrail 3 is unchanged: the evening send still attaches only a today's message approved at its current version.
- The scans below come back clean, except for what is kept on purpose.

## How it's tested

- `./scripts/verify.sh`: build with `-warnaserror`, API tests (3 `Ancestors_*` tests removed with the method), lint, typecheck, Vitest (including the locale key-parity test), web build.
- Db suite (`Category=Db`): a store and two Db test files changed.
- `npx playwright test`: the 12 existing e2e specs.

## Notes

- 15 Nov: how the unused code was found, on `main` at 5854c40:
  - **C# analyzers.** `dotnet format style --diagnostics IDE0051 IDE0052 IDE0060 IDE0005 --verify-no-changes` found no unused private members or usings, only the 2 parameters.
  - **C# reference scan.** Every type, method, constant and interface member in `src/api` was checked against `src/api` + `tests/api`.
  - **Web.** A TypeScript language-service scan (the project's own `typescript`, `tsconfig.app.json`) for unused exports and unimported files found nothing unused.
  - **Everything else is used:** locale keys (with dynamic prefixes such as `` `approve.status.${…}` `` checked one by one), npm and NuGet packages, prompt files, `data/`, `public/` and options properties.

  After the change all of the scans come back clean.
- 15 Nov, kept on purpose:
  - `SourceResult.NotYet`: the "register not in yet" state of the attendance contract (D-27, D-69). The simulated source can't return it, but the API handles it.
  - 39 web exports that are used only inside their own file. They are used, just exported needlessly; dropping `export` would be churn.
  - Design tokens: used through Tailwind classes, and owned by Monika.
  - The endpoint and setup classes the scan flagged: they hold extension methods, so their names never appear at the call site.
- 15 Nov: `knip` (unused files, exports and dependencies for JS) wasn't used, because downloading it was denied. The TypeScript scan covers the same ground for this repo.
