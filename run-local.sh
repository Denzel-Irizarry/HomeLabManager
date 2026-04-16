#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_PROJECT="$ROOT_DIR/HomeLabManager.API/HomeLabManager.API.csproj"
WEB_PROJECT="$ROOT_DIR/HomeLabManager.WEBUI/HomeLabManager.WEBUI.csproj"

cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" || true
  fi
  if [[ -n "${WEB_PID:-}" ]] && kill -0 "$WEB_PID" 2>/dev/null; then
    kill "$WEB_PID" || true
  fi
}

trap cleanup EXIT INT TERM

echo "Starting HomeLabManager local stack..."
dotnet restore "$ROOT_DIR"

dotnet run --project "$API_PROJECT" &
API_PID=$!

dotnet run --project "$WEB_PROJECT" &
WEB_PID=$!

echo "API PID: $API_PID"
echo "WEBUI PID: $WEB_PID"
echo "Press Ctrl+C to stop both services."

wait -n "$API_PID" "$WEB_PID"
