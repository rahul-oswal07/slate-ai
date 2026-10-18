# Azure deployment runbook (T-04)

| | Staging | Production |
|---|---|---|
| URL | https://slateai-staging.azurewebsites.net | https://slateai-prod.azurewebsites.net |
| Deploys on | merge to `main`, after `ci` passes | push of a `v*` tag |
| Database | `slateai_staging` | `slateai_prod` |

Both web apps share one Linux **B1** App Service plan (`asp-slateai`) and one PostgreSQL Flexible server (`psql-slateai`, B1ms, v16) in `rg-slateai`, Central India. B1 has no deployment slots, so "staging" is a second app rather than a slot. Approx cost ~US$30/month.

## How a deploy works

`.github/workflows/deploy.yml`:
1. Builds `deploy/Dockerfile` and pushes `ghcr.io/rahul-oswal07/slate-ai:sha-<short>` (plus `:main` or `:<tag>`).
2. Logs in to Azure with **OIDC**: no stored secrets. App registration `gh-slateai-deploy` trusts only the GitHub environments `staging` and `production`, and has *Website Contributor* on `rg-slateai` only. The federated subject uses GitHub's immutable-ID format (`repo:rahul-oswal07@16301809/slate-ai@1424145741:environment:<env>`); `provision.sh` reads the prefix from the repo's OIDC settings.
3. Points the web app at the new image, then smoke-tests `/health`, `/` and `/api/does-not-exist`.

On startup the API applies EF migrations and the idempotent demo seed.

**API docs:** `OpenApi__Enabled` is `true` on staging (https://slateai-staging.azurewebsites.net/swagger) and `false` on prod until the API has auth. Set by `provision.sh`.

GitHub environment rules: `staging` accepts only `main`, `production` accepts only `v*` tags. IDs and app names are repo **variables** (`AZURE_*`), set by the provisioning script.

## Provisioning

```bash
az login                                   # account that owns the subscription
deploy/azure/provision.sh                  # prints a summary, asks, then creates what's missing
deploy/azure/provision.sh --rotate-db-password   # new Postgres password, written to both apps
```

The script is idempotent. The Postgres admin password is generated in the script and only written to the apps' `ConnectionStrings__Db` setting; it is never printed or committed. Overrides via env vars: `LOCATION`, `RG`, `PG`, `APP_PREFIX`, `IMAGE`, `REPO`.

**Subscription:** Azure Free Trial with spending limit on (expires ~30 days after creation; upgrade to Pay-As-You-Go before the finals on 18 Nov if shortlisted).

## One-time manual step

The container package must be **public** so App Service can pull it without credentials: GitHub → Packages → `slate-ai` → Package settings → Change visibility → Public. (Not possible via the API for user-owned packages.)

## Operations

- **Logs:** `az webapp log tail -g rg-slateai -n slateai-staging`
- **Roll back:** re-run an older successful `deploy` workflow run, or
  `az webapp config container set -g rg-slateai -n slateai-prod --container-image-name ghcr.io/rahul-oswal07/slate-ai:sha-<old>`
- **Restart:** `az webapp restart -g rg-slateai -n slateai-staging`
- **Reset staging data:** drop and recreate `slateai_staging`, restart the app (migrations + seed re-run).
- **Tear down everything:** `az group delete -n rg-slateai`, and delete app registration `gh-slateai-deploy`.

## Fallback (plan risk R8)

If Azure is blocked, the same image runs on Render or Fly.io with Neon Postgres: set `ConnectionStrings__Db` and port 8080, and replace the `deploy` job's Azure steps.
