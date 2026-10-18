#!/usr/bin/env bash
# Provision Azure resources for Tappida Paatha (T-04). Idempotent: safe to re-run.
#
#   deploy/azure/provision.sh                       # show what will be created, ask, then apply
#   deploy/azure/provision.sh --yes                 # no prompt
#   deploy/azure/provision.sh --rotate-db-password  # new Postgres password, pushed to both apps
#
# Needs: az (logged in, right subscription selected), gh (logged in, repo admin).
# The Postgres password is generated here and only ever written to App Service settings.
set -euo pipefail

LOCATION="${LOCATION:-centralindia}"
RG="${RG:-rg-slateai}"
PLAN="${PLAN:-asp-slateai}"
PG="${PG:-psql-slateai}"
PG_ADMIN="${PG_ADMIN:-slateadmin}"
APP_PREFIX="${APP_PREFIX:-slateai}"
IMAGE="${IMAGE:-ghcr.io/rahul-oswal07/slate-ai}"
REPO="${REPO:-rahul-oswal07/slate-ai}"
SP_NAME="${SP_NAME:-gh-slateai-deploy}"
ENVS=(staging prod)

YES=false
ROTATE=false
for arg in "$@"; do
  case "$arg" in
    --yes) YES=true ;;
    --rotate-db-password) ROTATE=true ;;
    *) echo "unknown argument: $arg" >&2; exit 2 ;;
  esac
done

app_name() { echo "${APP_PREFIX}-$1"; }
gh_env() { [ "$1" = prod ] && echo production || echo staging; }
exists() { "$@" --output none >/dev/null 2>&1; }
# No 'head' in the pipe: with pipefail, SIGPIPE would abort the script silently. Prefix guarantees Azure's complexity rule.
new_password() { echo "Aa1$(openssl rand -base64 30 | tr -d '/+=\n')"; }

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

cat <<SUMMARY
Subscription : $(az account show --query name -o tsv) ($SUBSCRIPTION_ID)
Location     : $LOCATION
Creates (if missing):
  resource group   $RG
  postgres         $PG  (Flexible, Burstable B1ms, v16, 32 GB; dbs slateai_staging, slateai_prod)
  app service plan $PLAN (Linux B1)
  web apps         $(app_name staging), $(app_name prod)  (container $IMAGE)
  entra app + SP   $SP_NAME  (OIDC for GitHub environments staging/production; Website Contributor on $RG)
  github           environments staging/production + AZURE_* variables on $REPO
Approx cost: ~US\$30/month (App Service B1 ~13, Postgres B1ms + storage ~17)
SUMMARY
if ! $YES; then
  read -r -p "Proceed? [y/N] " answer
  [ "$answer" = y ] || { echo "aborted"; exit 1; }
fi

echo "==> resource providers (new subscriptions start unregistered)"
for provider in Microsoft.Web Microsoft.DBforPostgreSQL; do
  az provider register --namespace "$provider" --wait --output none
done

echo "==> resource group"
az group create -n "$RG" -l "$LOCATION" --output none

echo "==> postgres"
PG_PASSWORD=""
if ! exists az postgres flexible-server show -g "$RG" -n "$PG"; then
  PG_PASSWORD=$(new_password)
  az postgres flexible-server create -g "$RG" -n "$PG" -l "$LOCATION" \
    --tier Burstable --sku-name Standard_B1ms --version 16 --storage-size 32 \
    --admin-user "$PG_ADMIN" --admin-password "$PG_PASSWORD" \
    --public-access 0.0.0.0 --yes --output none   # 0.0.0.0 = allow Azure services only
elif $ROTATE; then
  PG_PASSWORD=$(new_password)
  az postgres flexible-server update -g "$RG" -n "$PG" --admin-password "$PG_PASSWORD" --output none
fi
for env in "${ENVS[@]}"; do
  exists az postgres flexible-server db show -g "$RG" --server-name "$PG" --name "slateai_$env" \
    || az postgres flexible-server db create -g "$RG" --server-name "$PG" --name "slateai_$env" --output none
done

echo "==> app service plan + web apps"
exists az appservice plan show -g "$RG" -n "$PLAN" \
  || az appservice plan create -g "$RG" -n "$PLAN" -l "$LOCATION" --is-linux --sku B1 --output none
for env in "${ENVS[@]}"; do
  app=$(app_name "$env")
  exists az webapp show -g "$RG" -n "$app" \
    || az webapp create -g "$RG" -p "$PLAN" -n "$app" --container-image-name "$IMAGE:main" --output none
  az webapp update -g "$RG" -n "$app" --https-only true --output none
  az webapp config set -g "$RG" -n "$app" --always-on true --min-tls-version 1.2 --ftps-state Disabled \
    --generic-configurations '{"healthCheckPath": "/health"}' --output none
  az webapp config appsettings set -g "$RG" -n "$app" --output none --settings \
    WEBSITES_PORT=8080 ASPNETCORE_ENVIRONMENT=Production
  if [ -n "$PG_PASSWORD" ]; then
    az webapp config appsettings set -g "$RG" -n "$app" --output none --settings \
      "ConnectionStrings__Db=Host=$PG.postgres.database.azure.com;Port=5432;Database=slateai_$env;Username=$PG_ADMIN;Password=$PG_PASSWORD;SSL Mode=Require"
  fi
done
unset PG_PASSWORD

echo "==> entra app + federated credentials (OIDC)"
CLIENT_ID=$(az ad app list --display-name "$SP_NAME" --query "[0].appId" -o tsv)
if [ -z "$CLIENT_ID" ]; then
  CLIENT_ID=$(az ad app create --display-name "$SP_NAME" --query appId -o tsv)
fi
exists az ad sp show --id "$CLIENT_ID" || az ad sp create --id "$CLIENT_ID" --output none
# GitHub's subject prefix: newer repos use immutable IDs (repo:owner@id/name@id), older ones repo:owner/name.
SUB_PREFIX=$(gh api "repos/$REPO/actions/oidc/customization/sub" --jq '.sub_claim_prefix // empty' 2>/dev/null || true)
SUB_PREFIX="${SUB_PREFIX:-repo:$REPO}"
for env in "${ENVS[@]}"; do
  name="github-$(gh_env "$env")"
  subject="$SUB_PREFIX:environment:$(gh_env "$env")"
  params="{
    \"name\": \"$name\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"$subject\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
  current=$(az ad app federated-credential list --id "$CLIENT_ID" --query "[?name=='$name'].subject" -o tsv)
  if [ -z "$current" ]; then
    az ad app federated-credential create --id "$CLIENT_ID" --parameters "$params" --output none
  elif [ "$current" != "$subject" ]; then
    az ad app federated-credential update --id "$CLIENT_ID" --federated-credential-id "$name" --parameters "$params" --output none
  fi
done
RG_ID=$(az group show -n "$RG" --query id -o tsv)
if [ -z "$(az role assignment list --assignee "$CLIENT_ID" --scope "$RG_ID" --role "Website Contributor" --query "[].id" -o tsv)" ]; then
  az role assignment create --assignee "$CLIENT_ID" --role "Website Contributor" --scope "$RG_ID" --output none
fi

echo "==> github environments + variables"
gh api -X PUT "repos/$REPO/environments/staging" --silent \
  -F "deployment_branch_policy[protected_branches]=false" -F "deployment_branch_policy[custom_branch_policies]=true"
gh api -X PUT "repos/$REPO/environments/production" --silent \
  -F "deployment_branch_policy[protected_branches]=false" -F "deployment_branch_policy[custom_branch_policies]=true"
add_policy() { # env, name, type: only add if missing
  gh api "repos/$REPO/environments/$1/deployment-branch-policies" --jq '.branch_policies[].name' | grep -qx "$2" \
    || gh api -X POST "repos/$REPO/environments/$1/deployment-branch-policies" -f name="$2" -f type="$3" --silent
}
add_policy staging main branch
add_policy production 'v*' tag
gh variable set AZURE_CLIENT_ID --repo "$REPO" --body "$CLIENT_ID"
gh variable set AZURE_TENANT_ID --repo "$REPO" --body "$TENANT_ID"
gh variable set AZURE_SUBSCRIPTION_ID --repo "$REPO" --body "$SUBSCRIPTION_ID"
gh variable set AZURE_WEBAPP_STAGING --repo "$REPO" --body "$(app_name staging)"
gh variable set AZURE_WEBAPP_PROD --repo "$REPO" --body "$(app_name prod)"

echo "done:"
for env in "${ENVS[@]}"; do echo "  https://$(app_name "$env").azurewebsites.net"; done
