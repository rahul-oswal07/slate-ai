#!/usr/bin/env bash
# Run Tappida Paatha locally with one command.
#
#   ./scripts/run-local.sh              # dev: Postgres + API (:5080) + Vite dev server with hot reload (:5173)
#   ./scripts/run-local.sh --container  # production-like: Postgres + the built container (:8080)
#   API_PORT=5081 WEB_PORT=5174 ./scripts/run-local.sh   # other ports, e.g. next to a running copy
#
# Ctrl+C stops the API and web server. The database keeps running; stop it with:
#   docker compose -f deploy/docker-compose.yml down
set -euo pipefail
cd "$(dirname "$0")/.."

COMPOSE=(docker compose -f deploy/docker-compose.yml)
API_PORT="${API_PORT:-5080}"
WEB_PORT="${WEB_PORT:-5173}"
API_URL="http://localhost:$API_PORT"
MODE=dev
case "${1:-}" in
  "") ;;
  --container) MODE=container ;;
  -h|--help) sed -n '2,10p' "$0"; exit 0 ;;
  *) echo "unknown option: $1 (try --help)" >&2; exit 2 ;;
esac

info() { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
fail() { printf '\033[1;31merror:\033[0m %s\n' "$*" >&2; exit 1; }
need() { command -v "$1" >/dev/null 2>&1 || fail "$1 not found. $2"; }
port_free() {
  if lsof -nP -iTCP:"$1" -sTCP:LISTEN >/dev/null 2>&1; then
    fail "port $1 is already in use (another API or dev server running?). Stop it first: lsof -nP -iTCP:$1 -sTCP:LISTEN"
  fi
}

# --- Docker (Colima or Docker Desktop) ---
need docker "Install Colima: brew install colima docker docker-compose (see README)."
if ! docker info >/dev/null 2>&1; then
  if command -v colima >/dev/null 2>&1; then
    info "Starting Colima"
    colima start
  else
    fail "Docker isn't running. Start Docker Desktop or Colima."
  fi
fi
# Lets docker compose find Colima's socket.
if [ -S "$HOME/.colima/default/docker.sock" ]; then
  export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
fi

if [ "$MODE" = container ]; then
  port_free 8080
  info "Building and starting the container (first build takes a few minutes)"
  info "App: http://localhost:8080   Health: http://localhost:8080/health   (Ctrl+C to stop)"
  exec "${COMPOSE[@]}" up --build app
fi

# --- Dev mode ---
need dotnet "Install the .NET 10 SDK (arm64 on Apple Silicon): brew install --cask dotnet-sdk"
need npm "Install Node 22: brew install node@22"
dotnet --list-sdks | grep -q '^10\.' || fail ".NET 10 SDK not found (have: $(dotnet --list-sdks | cut -d' ' -f1 | tr '\n' ' '))."
port_free "$API_PORT"
port_free "$WEB_PORT"

info "Starting PostgreSQL"
"${COMPOSE[@]}" up -d --wait db

if [ ! -d src/web/node_modules ] || [ src/web/package-lock.json -nt src/web/node_modules ]; then
  info "Installing web dependencies"
  (cd src/web && npm ci --no-audit --no-fund)
fi

pids=()
cleanup() {
  trap - INT TERM EXIT
  echo
  info "Stopping API and web server (database left running)"
  # ${pids[@]+...}: bash 3.2 (macOS default) treats an empty array as unset under set -u.
  for pid in ${pids[@]+"${pids[@]}"}; do kill "$pid" 2>/dev/null || true; done
  wait 2>/dev/null || true
}
trap cleanup INT TERM EXIT

info "Starting API on $API_URL (applies migrations + demo seed)"
dotnet run --project src/api/SlateAi.Api --urls "$API_URL" &
pids+=($!)

info "Waiting for the API to be healthy"
for _ in $(seq 1 90); do
  curl -sf "$API_URL/health" >/dev/null 2>&1 && break
  kill -0 "${pids[0]}" 2>/dev/null || fail "API exited during startup (see output above)."
  sleep 1
done
curl -sf "$API_URL/health" >/dev/null 2>&1 || fail "API didn't become healthy within 90 s."

info "Starting web dev server on http://localhost:$WEB_PORT"
(cd src/web && API_URL="$API_URL" exec npm run dev -- --port "$WEB_PORT" --strictPort) &
pids+=($!)

cat <<URLS

  App (hot reload)  http://localhost:$WEB_PORT
  API               $API_URL
  API docs          $API_URL/swagger
  Health            $API_URL/health

  Ctrl+C to stop.

URLS
# Stop everything if either process exits (no `wait -n` in bash 3.2).
while kill -0 "${pids[0]}" 2>/dev/null && kill -0 "${pids[1]}" 2>/dev/null; do sleep 1; done
